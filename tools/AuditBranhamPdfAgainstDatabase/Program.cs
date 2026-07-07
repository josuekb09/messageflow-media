using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Core.Text;
using MessageFlow.Data;
using MessageFlow.Importer;
using Microsoft.Data.Sqlite;

const string BranhamPdfRoot = @"D:\Br William Marrion Branham\PDF";
const string OutputDirectory = @"D:\MessageFlow Archive\BranhamAudit";
const string TextReportPath = OutputDirectory + @"\branham_pdf_database_accuracy_report.txt";
const string CsvReportPath = OutputDirectory + @"\branham_pdf_database_accuracy_issues.csv";

Directory.CreateDirectory(OutputDirectory);

var databasePath = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ??
                   MessageFlowDatabase.DefaultDatabasePath;

if (!Directory.Exists(BranhamPdfRoot))
{
    Console.Error.WriteLine($"Brother Branham PDF folder not found: {BranhamPdfRoot}");
    return 1;
}

if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Database not found: {databasePath}");
    return 1;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var sourceContext = new SourceMetadataContext(
    1,
    "brother_branham",
    "Brother Branham",
    "SermonPdfCollection");
var extractor = new PdfTextExtractor();
var pdfFiles = Directory.EnumerateFiles(BranhamPdfRoot, "*.pdf", SearchOption.AllDirectories)
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToList();
var databaseSermons = await LoadDatabaseSermonsAsync(connection);
var sermonIndex = BuildSermonIndex(databaseSermons);
var issues = new List<AuditIssue>();
var auditedSermonIds = new HashSet<int>();
var processed = 0;

foreach (var pdfFile in pdfFiles)
{
    processed++;
    if (processed % 100 == 0)
    {
        Console.WriteLine($"Audited {processed:N0}/{pdfFiles.Count:N0} PDFs...");
    }

    var metadata = SermonMetadataParser.Parse(pdfFile, BranhamPdfRoot, sourceContext);
    if (!TryFindDatabaseSermon(sermonIndex, pdfFile, metadata, out var databaseSermon))
    {
        issues.Add(new AuditIssue(
            metadata.SermonCode,
            metadata.Title,
            metadata.Year,
            pdfFile,
            "UnmatchedPdf",
            null,
            string.Empty,
            Shorten(Path.GetFileName(pdfFile), 240),
            "High",
            "The local PDF could not be matched to a Brother Branham sermon record."));
        continue;
    }

    auditedSermonIds.Add(databaseSermon.Id);
    var pdfParagraphs = PdfFirstBranhamBlockExtractor.Split(extractor.ExtractPages(pdfFile), metadata);
    var databaseParagraphs = await LoadParagraphsAsync(connection, databaseSermon.Id);
    AuditSermon(databaseSermon, pdfFile, databaseParagraphs, pdfParagraphs, issues);
}

foreach (var databaseSermon in databaseSermons.Where(sermon => !auditedSermonIds.Contains(sermon.Id)))
{
    issues.Add(new AuditIssue(
        databaseSermon.SermonCode,
        databaseSermon.Title,
        databaseSermon.Year,
        databaseSermon.SourceFilePath,
        "DatabaseSermonWithoutMatchedPdf",
        null,
        string.Empty,
        string.Empty,
        "High",
        "A Brother Branham sermon record was not matched by any local PDF during the audit."));
}

await WriteCsvAsync(CsvReportPath, issues);
await WriteReportAsync(TextReportPath, databasePath, pdfFiles.Count, databaseSermons.Count, auditedSermonIds.Count, issues);

var highSeverityCount = issues.Count(issue => issue.Severity == "High");
Console.WriteLine($"PDF files audited: {pdfFiles.Count:N0}");
Console.WriteLine($"Database sermons found: {databaseSermons.Count:N0}");
Console.WriteLine($"Matched database sermons: {auditedSermonIds.Count:N0}");
Console.WriteLine($"Issues: {issues.Count:N0}");
Console.WriteLine($"High-severity issues: {highSeverityCount:N0}");
Console.WriteLine($"Report: {TextReportPath}");
Console.WriteLine($"CSV: {CsvReportPath}");

return highSeverityCount == 0 ? 0 : 2;

static void AuditSermon(
    DatabaseSermon sermon,
    string pdfFile,
    IReadOnlyList<DatabaseParagraph> databaseParagraphs,
    IReadOnlyList<ParagraphDraft> pdfParagraphs,
    ICollection<AuditIssue> issues)
{
    var databaseByNumber = databaseParagraphs
        .GroupBy(paragraph => paragraph.ParagraphNumber)
        .ToDictionary(group => group.Key, group => group.First());
    var pdfByNumber = pdfParagraphs
        .GroupBy(paragraph => paragraph.ParagraphNumber)
        .ToDictionary(group => group.Key, group => group.First());

    if (pdfByNumber.TryGetValue(1, out var expectedOpening) &&
        (!databaseByNumber.TryGetValue(1, out var databaseOpening) ||
         !LooksLikeSameOpening(databaseOpening.Text, expectedOpening.Text)))
    {
        issues.Add(new AuditIssue(
            sermon.SermonCode,
            sermon.Title,
            sermon.Year,
            pdfFile,
            "MissingOpeningBodyParagraph",
            1,
            databaseOpening?.Text ?? string.Empty,
            expectedOpening.Text,
            "High",
            "The first real PDF body paragraph is missing or does not match database paragraph 1."));
    }

    foreach (var expected in pdfParagraphs)
    {
        if (!databaseByNumber.TryGetValue(expected.ParagraphNumber, out var databaseParagraph))
        {
            if (expected.ParagraphNumber != 1)
            {
                issues.Add(new AuditIssue(
                    sermon.SermonCode,
                    sermon.Title,
                    sermon.Year,
                    pdfFile,
                    "MissingParagraph",
                    expected.ParagraphNumber,
                    string.Empty,
                    expected.Text,
                    "High",
                    "A paragraph number present in the local PDF body text is missing from the database."));
            }

            continue;
        }

        var databaseText = NormalizeForComparison(databaseParagraph.Text);
        var pdfText = NormalizeForComparison(expected.Text);
        if (!string.Equals(databaseText, pdfText, StringComparison.Ordinal))
        {
            AddMismatchIssues(issues, sermon, pdfFile, databaseParagraph, expected, databaseText, pdfText);
        }

        if (ProjectionTextCleaner.HasSuspiciousLetterSpacing(databaseParagraph.Text))
        {
            issues.Add(new AuditIssue(
                sermon.SermonCode,
                sermon.Title,
                sermon.Year,
                pdfFile,
                "BrokenLetterSpacedWords",
                databaseParagraph.ParagraphNumber,
                databaseParagraph.Text,
                expected.Text,
                "High",
                "The database paragraph contains a known PDF letter-spacing artifact."));
        }

        if (LooksLikeImportedHeader(databaseParagraph.Text, sermon.Title))
        {
            issues.Add(new AuditIssue(
                sermon.SermonCode,
                sermon.Title,
                sermon.Year,
                pdfFile,
                "TitleOrHeaderImportedAsParagraphText",
                databaseParagraph.ParagraphNumber,
                databaseParagraph.Text,
                expected.Text,
                "High",
                "The database paragraph contains repeated PDF title/header/footer text."));
        }
    }

    foreach (var databaseParagraph in databaseParagraphs.Where(paragraph => !pdfByNumber.ContainsKey(paragraph.ParagraphNumber)))
    {
        var issueType = LooksLikeImportedHeader(databaseParagraph.Text, sermon.Title)
            ? "TitleOrHeaderImportedAsParagraphText"
            : "UnexpectedDatabaseParagraph";
        var severity = issueType == "TitleOrHeaderImportedAsParagraphText" ? "High" : "Medium";
        issues.Add(new AuditIssue(
            sermon.SermonCode,
            sermon.Title,
            sermon.Year,
            pdfFile,
            issueType,
            databaseParagraph.ParagraphNumber,
            databaseParagraph.Text,
            string.Empty,
            severity,
            "The database has a paragraph number that is not present in the rebuilt local PDF body paragraph set."));
    }

    AddMissingNumberIssues(issues, sermon, pdfFile, databaseParagraphs, pdfParagraphs);
    AddWholeSermonLengthIssue(issues, sermon, pdfFile, databaseParagraphs, pdfParagraphs);
}

static void AddMismatchIssues(
    ICollection<AuditIssue> issues,
    DatabaseSermon sermon,
    string pdfFile,
    DatabaseParagraph databaseParagraph,
    ParagraphDraft expected,
    string databaseText,
    string pdfText)
{
    if (LooksLikeCutAtPageBreak(databaseText, pdfText))
    {
        issues.Add(new AuditIssue(
            sermon.SermonCode,
            sermon.Title,
            sermon.Year,
            pdfFile,
            "ParagraphCutAtPageBreak",
            expected.ParagraphNumber,
            databaseParagraph.Text,
            expected.Text,
            "High",
            "The database paragraph is an opening prefix of the local PDF paragraph and is missing trailing continuation text."));
        return;
    }

    if (databaseText.Length + 80 < pdfText.Length)
    {
        issues.Add(new AuditIssue(
            sermon.SermonCode,
            sermon.Title,
            sermon.Year,
            pdfFile,
            "DatabaseParagraphShorterThanPdf",
            expected.ParagraphNumber,
            databaseParagraph.Text,
            expected.Text,
            "High",
            "The database paragraph is materially shorter than the corresponding local PDF body paragraph."));
        return;
    }

    issues.Add(new AuditIssue(
        sermon.SermonCode,
        sermon.Title,
        sermon.Year,
        pdfFile,
        "SuddenParagraphBoundaryMismatch",
        expected.ParagraphNumber,
        databaseParagraph.Text,
        expected.Text,
        "Medium",
        "The database paragraph text differs from the local PDF paragraph with the same paragraph number."));
}

static void AddMissingNumberIssues(
    ICollection<AuditIssue> issues,
    DatabaseSermon sermon,
    string pdfFile,
    IReadOnlyList<DatabaseParagraph> databaseParagraphs,
    IReadOnlyList<ParagraphDraft> pdfParagraphs)
{
    var expectedNumbers = pdfParagraphs.Select(paragraph => paragraph.ParagraphNumber).ToHashSet();
    var databaseNumbers = databaseParagraphs.Select(paragraph => paragraph.ParagraphNumber).ToHashSet();
    foreach (var missingNumber in expectedNumbers.Except(databaseNumbers).Order())
    {
        issues.Add(new AuditIssue(
            sermon.SermonCode,
            sermon.Title,
            sermon.Year,
            pdfFile,
            "SuspiciousMissingParagraphNumber",
            missingNumber,
            string.Empty,
            pdfParagraphs.First(paragraph => paragraph.ParagraphNumber == missingNumber).Text,
            "High",
            "A paragraph number expected from the local PDF body sequence is absent from the database."));
    }
}

static void AddWholeSermonLengthIssue(
    ICollection<AuditIssue> issues,
    DatabaseSermon sermon,
    string pdfFile,
    IReadOnlyList<DatabaseParagraph> databaseParagraphs,
    IReadOnlyList<ParagraphDraft> pdfParagraphs)
{
    var databaseLength = NormalizeForComparison(string.Join(' ', databaseParagraphs.Select(paragraph => paragraph.Text))).Length;
    var pdfLength = NormalizeForComparison(string.Join(' ', pdfParagraphs.Select(paragraph => paragraph.Text))).Length;
    if (pdfLength == 0 || databaseLength >= pdfLength * 0.98)
    {
        return;
    }

    issues.Add(new AuditIssue(
        sermon.SermonCode,
        sermon.Title,
        sermon.Year,
        pdfFile,
        "SermonTextShorterThanPdfBody",
        null,
        $"Database normalized body length: {databaseLength:N0}",
        $"PDF normalized body length: {pdfLength:N0}",
        "High",
        "The total database body text is materially shorter than the local PDF body text."));
}

static bool LooksLikeSameOpening(string databaseText, string pdfText)
{
    var databaseOpening = NormalizeForComparison(databaseText);
    var pdfOpening = NormalizeForComparison(pdfText);
    if (databaseOpening.Length == 0 || pdfOpening.Length == 0)
    {
        return false;
    }

    var comparableLength = Math.Min(Math.Min(databaseOpening.Length, pdfOpening.Length), 120);
    return string.Equals(databaseOpening[..comparableLength], pdfOpening[..comparableLength], StringComparison.Ordinal);
}

static bool LooksLikeCutAtPageBreak(string databaseText, string pdfText)
{
    if (databaseText.Length < 80 || pdfText.Length < databaseText.Length + 80)
    {
        return false;
    }

    return pdfText.StartsWith(databaseText, StringComparison.Ordinal);
}

static bool LooksLikeImportedHeader(string text, string sermonTitle)
{
    var normalized = NormalizeKey(text);
    _ = sermonTitle;
    return AuditRegexes.NumberedSpokenWordHeaderRegex().IsMatch(text) ||
           normalized == "THESPOKENWORD" ||
           normalized.Contains("VOICEOFGODRECORDINGS", StringComparison.Ordinal) ||
           normalized.Contains("ALLRIGHTSRESERVED", StringComparison.Ordinal) ||
           normalized.Contains("WWWBRANHAMORG", StringComparison.Ordinal);
}

static async Task<IReadOnlyList<DatabaseSermon>> LoadDatabaseSermonsAsync(SqliteConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT s."Id",
               s."SermonCode",
               s."Title",
               s."Year",
               s."SourceFilePath"
        FROM "Sermons" s
        INNER JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham'
        ORDER BY s."Year", s."SermonCode", s."Title";
        """;

    var sermons = new List<DatabaseSermon>();
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        sermons.Add(new DatabaseSermon(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetString(4)));
    }

    return sermons;
}

static SermonIndex BuildSermonIndex(IEnumerable<DatabaseSermon> sermons)
{
    var byPath = new Dictionary<string, DatabaseSermon>(StringComparer.OrdinalIgnoreCase);
    var byCodeTitle = new Dictionary<string, DatabaseSermon>(StringComparer.OrdinalIgnoreCase);
    var byCodeYear = new Dictionary<string, DatabaseSermon>(StringComparer.OrdinalIgnoreCase);
    var byFileName = new Dictionary<string, DatabaseSermon>(StringComparer.OrdinalIgnoreCase);

    foreach (var sermon in sermons)
    {
        if (!string.IsNullOrWhiteSpace(sermon.SourceFilePath))
        {
            byPath.TryAdd(Path.GetFullPath(sermon.SourceFilePath), sermon);
            byFileName.TryAdd(NormalizeKey(Path.GetFileNameWithoutExtension(sermon.SourceFilePath)), sermon);
        }

        byCodeTitle.TryAdd(BuildCodeTitleKey(sermon.SermonCode, sermon.Title), sermon);
        byCodeYear.TryAdd(BuildCodeYearKey(sermon.SermonCode, sermon.Year), sermon);
    }

    return new SermonIndex(byPath, byCodeTitle, byCodeYear, byFileName);
}

static bool TryFindDatabaseSermon(
    SermonIndex index,
    string pdfFile,
    SermonMetadata metadata,
    out DatabaseSermon sermon)
{
    if (index.ByPath.TryGetValue(Path.GetFullPath(pdfFile), out sermon!))
    {
        return true;
    }

    if (index.ByCodeTitle.TryGetValue(BuildCodeTitleKey(metadata.SermonCode, metadata.Title), out sermon!))
    {
        return true;
    }

    if (index.ByCodeYear.TryGetValue(BuildCodeYearKey(metadata.SermonCode, metadata.Year), out sermon!))
    {
        return true;
    }

    return index.ByFileName.TryGetValue(NormalizeKey(Path.GetFileNameWithoutExtension(pdfFile)), out sermon!);
}

static async Task<List<DatabaseParagraph>> LoadParagraphsAsync(SqliteConnection connection, int sermonId)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT "Id",
               "ParagraphNumber",
               "Text"
        FROM "SermonParagraphs"
        WHERE "SermonId" = $sermonId
        ORDER BY "ParagraphNumber";
        """;
    command.Parameters.AddWithValue("$sermonId", sermonId);

    var paragraphs = new List<DatabaseParagraph>();
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        paragraphs.Add(new DatabaseParagraph(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2)));
    }

    return paragraphs;
}

static async Task WriteCsvAsync(string path, IReadOnlyList<AuditIssue> issues)
{
    await using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    await writer.WriteLineAsync("sermonCode,title,year,sourcePdfPath,issueType,paragraphNumber,databaseSnippet,pdfSnippet,severity,notes");
    foreach (var issue in issues.OrderByDescending(SortSeverity).ThenBy(issue => issue.SermonCode).ThenBy(issue => issue.ParagraphNumber))
    {
        await writer.WriteLineAsync(string.Join(
            ',',
            Csv(issue.SermonCode),
            Csv(issue.Title),
            issue.Year.ToString(),
            Csv(issue.SourcePdfPath),
            Csv(issue.IssueType),
            issue.ParagraphNumber?.ToString() ?? string.Empty,
            Csv(Shorten(issue.DatabaseSnippet, 400)),
            Csv(Shorten(issue.PdfSnippet, 400)),
            Csv(issue.Severity),
            Csv(issue.Notes)));
    }
}

static async Task WriteReportAsync(
    string path,
    string databasePath,
    int pdfFileCount,
    int databaseSermonCount,
    int matchedSermonCount,
    IReadOnlyList<AuditIssue> issues)
{
    var builder = new StringBuilder();
    builder.AppendLine("Brother Branham PDF Against Database Accuracy Audit");
    builder.AppendLine("===================================================");
    builder.AppendLine($"Run time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Mode: read-only; no database changes were made.");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"PDF root: {BranhamPdfRoot}");
    builder.AppendLine();
    builder.AppendLine($"PDF files audited: {pdfFileCount:N0}");
    builder.AppendLine($"Brother Branham database sermons: {databaseSermonCount:N0}");
    builder.AppendLine($"Matched sermons: {matchedSermonCount:N0}");
    builder.AppendLine($"Issues: {issues.Count:N0}");
    builder.AppendLine($"High-severity issues: {issues.Count(issue => issue.Severity == "High"):N0}");
    builder.AppendLine($"Medium-severity issues: {issues.Count(issue => issue.Severity == "Medium"):N0}");
    builder.AppendLine($"Low-severity issues: {issues.Count(issue => issue.Severity == "Low"):N0}");
    builder.AppendLine();

    builder.AppendLine("Issue counts");
    builder.AppendLine("------------");
    foreach (var pair in issues.GroupBy(issue => issue.IssueType).OrderByDescending(group => group.Count()).ThenBy(group => group.Key))
    {
        builder.AppendLine($"{pair.Key}: {pair.Count():N0}");
    }

    builder.AppendLine();
    builder.AppendLine("Focused regression status");
    builder.AppendLine("-------------------------");
    AppendFocusStatus(builder, issues, "58-1228", "Why Little Bethlehem", "58-1228 Why Little Bethlehem opening order");
    AppendFocusStatus(builder, issues, "63-1201X", "Wedding Ceremony", "63-1201X Wedding Ceremony opening order");
    AppendFocusStatus(builder, issues, "63-1214", "Why Little Bethlehem", "63-1214 page-break continuation");
    AppendFocusStatus(builder, issues, "47-0412", "Faith Is The Substance", "Faith Is The Substance paragraph lookup");
    builder.AppendLine();

    builder.AppendLine("First high-severity issues");
    builder.AppendLine("--------------------------");
    foreach (var issue in issues.Where(issue => issue.Severity == "High").Take(75))
    {
        builder.AppendLine($"{issue.SermonCode} | {issue.Title} | Paragraph {issue.ParagraphNumber?.ToString() ?? "n/a"} | {issue.IssueType}");
        builder.AppendLine($"PDF: {issue.SourcePdfPath}");
        builder.AppendLine($"Database: {Shorten(issue.DatabaseSnippet, 240)}");
        builder.AppendLine($"PDF     : {Shorten(issue.PdfSnippet, 240)}");
        builder.AppendLine($"Notes   : {issue.Notes}");
        builder.AppendLine();
    }

    builder.AppendLine($"Full CSV: {CsvReportPath}");
    await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static void AppendFocusStatus(StringBuilder builder, IReadOnlyList<AuditIssue> issues, string codeOrTitle, string title, string label)
{
    var matchingIssues = issues
        .Where(issue =>
            issue.SermonCode.Contains(codeOrTitle, StringComparison.OrdinalIgnoreCase) ||
            issue.Title.Contains(codeOrTitle, StringComparison.OrdinalIgnoreCase) ||
            issue.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
        .ToList();
    var high = matchingIssues.Count(issue => issue.Severity == "High");
    var medium = matchingIssues.Count(issue => issue.Severity == "Medium");
    builder.AppendLine($"{label}: high={high:N0}, medium={medium:N0}, total={matchingIssues.Count:N0}");
}

static string BuildCodeTitleKey(string code, string title)
{
    return $"{code.Trim().ToUpperInvariant()}|{NormalizeTitle(title)}";
}

static string BuildCodeYearKey(string code, int year)
{
    return $"{code.Trim().ToUpperInvariant()}|{year}";
}

static string NormalizeTitle(string title)
{
    return NormalizeKey(title.Replace(" VGR", string.Empty, StringComparison.OrdinalIgnoreCase));
}

static string NormalizeKey(string value)
{
    var builder = new StringBuilder(value.Length);
    foreach (var character in value.ToUpperInvariant())
    {
        if (char.IsLetterOrDigit(character))
        {
            builder.Append(character);
        }
    }

    return builder.ToString();
}

static string NormalizeForComparison(string value)
{
    return string.Join(' ', TextCleaner.CleanExtractedText(value).Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

static string Shorten(string value, int maxLength)
{
    var normalized = string.Join(' ', value.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    return normalized.Length <= maxLength
        ? normalized
        : $"{normalized[..maxLength].TrimEnd()}...";
}

static int SortSeverity(AuditIssue issue)
{
    return issue.Severity switch
    {
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };
}

static string Csv(string value)
{
    return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}

sealed record SermonIndex(
    IReadOnlyDictionary<string, DatabaseSermon> ByPath,
    IReadOnlyDictionary<string, DatabaseSermon> ByCodeTitle,
    IReadOnlyDictionary<string, DatabaseSermon> ByCodeYear,
    IReadOnlyDictionary<string, DatabaseSermon> ByFileName);

sealed record DatabaseSermon(
    int Id,
    string SermonCode,
    string Title,
    int Year,
    string SourceFilePath);

sealed record DatabaseParagraph(
    int Id,
    int ParagraphNumber,
    string Text);

sealed record AuditIssue(
    string SermonCode,
    string Title,
    int Year,
    string SourcePdfPath,
    string IssueType,
    int? ParagraphNumber,
    string DatabaseSnippet,
    string PdfSnippet,
    string Severity,
    string Notes);

static partial class AuditRegexes
{
    [GeneratedRegex(@"\b\d{1,4}\s+THE\s+SPOKEN\s+WORD(?:\.{3})?\b", RegexOptions.CultureInvariant)]
    public static partial Regex NumberedSpokenWordHeaderRegex();

}
