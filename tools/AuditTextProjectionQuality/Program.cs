using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Core.Text;
using MessageFlow.Data;
using Microsoft.Data.Sqlite;

const string OutputDirectory = @"D:\MessageFlow Archive\FinalQualityAudit";
const string ReportPath = OutputDirectory + @"\projection_text_quality_report.txt";
const string CsvPath = OutputDirectory + @"\projection_text_quality_issues.csv";
const string BranhamPdfRoot = @"D:\Br William Marrion Branham\PDF";

Directory.CreateDirectory(OutputDirectory);

var reportOnly = args.Any(arg => string.Equals(arg, "--report-only", StringComparison.OrdinalIgnoreCase));
var databasePath = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ??
                   MessageFlowDatabase.DefaultDatabasePath;

if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return 1;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var beforeIssues = await ScanAsync(connection);
var autoFixes = beforeIssues
    .Where(issue => issue.CanAutoFix &&
                    issue.ContentType == "Sermon" &&
                    !string.Equals(issue.BeforeText, issue.SuggestedAfterText, StringComparison.Ordinal))
    .GroupBy(issue => issue.RecordId)
    .Select(group => group.First())
    .ToList();

if (!reportOnly && autoFixes.Count > 0)
{
    await ApplySafeSermonFixesAsync(connection, autoFixes);
}

var remainingIssues = await ScanAsync(connection);
var highSeverityRemaining = remainingIssues.Count(issue => issue.Severity == "High");

await WriteCsvAsync(CsvPath, beforeIssues, autoFixes, reportOnly);
await WriteReportAsync(
    ReportPath,
    databasePath,
    beforeIssues,
    remainingIssues,
    autoFixes,
    reportOnly);

Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"Issues before safe fixes: {beforeIssues.Count:N0}");
Console.WriteLine($"Safe sermon fixes applied: {(reportOnly ? 0 : autoFixes.Count):N0}");
Console.WriteLine($"High-severity issues remaining: {highSeverityRemaining:N0}");
Console.WriteLine($"Report: {ReportPath}");
Console.WriteLine($"CSV: {CsvPath}");

return highSeverityRemaining == 0 ? 0 : 2;

static async Task<List<TextQualityIssue>> ScanAsync(SqliteConnection connection)
{
    var issues = new List<TextQualityIssue>();

    await ScanSermonsAsync(connection, issues);
    await ScanBibleAsync(connection, issues);
    await ScanSongsAsync(connection, issues);

    return issues;
}

static async Task ScanSermonsAsync(SqliteConnection connection, List<TextQualityIssue> issues)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT p."Id",
               s."Title",
               s."SermonCode",
               s."SourceFilePath",
               p."ParagraphNumber",
               p."Text"
        FROM "SermonParagraphs" p
        INNER JOIN "Sermons" s ON s."Id" = p."SermonId"
        WHERE s."SourceFilePath" LIKE $sourceRoot;
        """;
    command.Parameters.AddWithValue("$sourceRoot", BranhamPdfRoot + "%");

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var title = reader.GetString(1);
        var code = reader.GetString(2);
        var sourcePath = reader.GetString(3);
        var paragraphNumber = reader.GetInt32(4);
        var text = reader.GetString(5);
        var titleOrReference = string.IsNullOrWhiteSpace(code) ? title : $"{code} {title}";
        var sectionOrParagraph = $"Paragraph {paragraphNumber}";
        var cleaned = ProjectionTextCleaner.CleanSermonText(text);

        if (ProjectionTextCleaner.HasSuspiciousLetterSpacing(text) &&
            !string.Equals(text, cleaned, StringComparison.Ordinal))
        {
            issues.Add(new TextQualityIssue(
                "Sermon",
                id,
                titleOrReference,
                sourcePath,
                sectionOrParagraph,
                "LetterSpacingArtifact",
                text,
                cleaned,
                "High",
                "Safe PDF extraction artifact: isolated uppercase letters are split inside normal words.",
                CanAutoFix: true));
        }

        AddCommonIssues(issues, "Sermon", id, titleOrReference, sourcePath, sectionOrParagraph, text, cleaned);

        if (CleanForComparison(cleaned).Contains("THE SPOKEN WORD", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new TextQualityIssue(
                "Sermon",
                id,
                titleOrReference,
                sourcePath,
                sectionOrParagraph,
                "PossibleHeaderOrFooterText",
                Shorten(text),
                Shorten(cleaned),
                "Low",
                "The paragraph contains a common PDF header phrase. Review only if it appears in operator projection."));
        }
    }
}

static async Task ScanBibleAsync(SqliteConnection connection, List<TextQualityIssue> issues)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT v."Id",
               b."Name",
               t."Abbreviation",
               v."Chapter",
               v."Verse",
               v."Text"
        FROM "BibleVerses" v
        INNER JOIN "BibleBooks" b ON b."Id" = v."BookId"
        INNER JOIN "BibleTranslations" t ON t."Id" = v."TranslationId";
        """;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var book = reader.GetString(1);
        var translation = reader.GetString(2);
        var chapter = reader.GetInt32(3);
        var verse = reader.GetInt32(4);
        var text = reader.GetString(5);
        var reference = $"{book} {chapter}:{verse} ({translation})";

        if (HasObviousNonSermonLetterSpacing(text))
        {
            issues.Add(new TextQualityIssue(
                "Bible",
                id,
                reference,
                "KJV database",
                $"{chapter}:{verse}",
                "LetterSpacingArtifact",
                text,
                text,
                "High",
                "Bible text should be exact and was not automatically changed."));
        }

        AddCommonIssues(issues, "Bible", id, reference, "KJV database", $"{chapter}:{verse}", text, text);
    }
}

static async Task ScanSongsAsync(SqliteConnection connection, List<TextQualityIssue> issues)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT ss."Id",
               s."Title",
               s."SourceFilePath",
               ss."SectionLabel",
               ss."SectionType",
               ss."Text"
        FROM "SongSections" ss
        INNER JOIN "Songs" s ON s."Id" = ss."SongId"
        WHERE s."IsActive" = 1;
        """;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var title = reader.GetString(1);
        var sourcePath = reader.GetString(2);
        var sectionLabel = reader.GetString(3);
        var sectionType = reader.GetString(4);
        var text = reader.GetString(5);
        var section = string.IsNullOrWhiteSpace(sectionType)
            ? sectionLabel
            : $"{sectionLabel} ({sectionType})";

        if (HasObviousNonSermonLetterSpacing(text))
        {
            issues.Add(new TextQualityIssue(
                "Song",
                id,
                title,
                sourcePath,
                section,
                "LetterSpacingArtifact",
                text,
                text,
                "High",
                "Song lyrics should match the PowerPoint source and were not automatically changed."));
        }

        AddCommonIssues(issues, "Song", id, title, sourcePath, section, text, text);
        AddSongRegressionChecks(issues, id, title, sourcePath, section, text);
    }
}

static void AddCommonIssues(
    List<TextQualityIssue> issues,
    string contentType,
    int recordId,
    string titleOrReference,
    string sourcePath,
    string sectionOrParagraph,
    string text,
    string suggestedText)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        issues.Add(new TextQualityIssue(
            contentType,
            recordId,
            titleOrReference,
            sourcePath,
            sectionOrParagraph,
            "EmptyOrJunkText",
            text,
            suggestedText,
            "High",
            "Projection content is empty."));
        return;
    }

    if (TextQualityRegex.WeirdSymbolRegex().IsMatch(text))
    {
        issues.Add(new TextQualityIssue(
            contentType,
            recordId,
            titleOrReference,
            sourcePath,
            sectionOrParagraph,
            "SuspiciousSymbol",
            Shorten(text),
            Shorten(suggestedText),
            "Medium",
            "Contains @, #, or %; review before projecting if it appears in visible text."));
    }

    if (TextQualityRegex.ExcessiveSpaceRegex().IsMatch(text))
    {
        issues.Add(new TextQualityIssue(
            contentType,
            recordId,
            titleOrReference,
            sourcePath,
            sectionOrParagraph,
            "ExcessiveSpaces",
            Shorten(text),
            Shorten(suggestedText),
            "Low",
            "Contains repeated horizontal whitespace."));
    }

    var lineCount = CountNonEmptyLines(text);
    var wordCount = CountWords(text);
    if (contentType != "Bible" && (lineCount > 16 || wordCount > 180))
    {
        issues.Add(new TextQualityIssue(
            contentType,
            recordId,
            titleOrReference,
            sourcePath,
            sectionOrParagraph,
            "ProjectionPaginationReview",
            Shorten(text),
            Shorten(suggestedText),
            "Low",
            "Long content should paginate rather than shrink below readable size."));
    }
}

static void AddSongRegressionChecks(
    List<TextQualityIssue> issues,
    int recordId,
    string title,
    string sourcePath,
    string section,
    string text)
{
    if (!title.StartsWith("116.", StringComparison.OrdinalIgnoreCase) ||
        !title.Contains("WON", StringComparison.OrdinalIgnoreCase) ||
        !section.Contains("Slide 2", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var repeatedLineCount = Regex.Matches(
            text,
            @"Won['’]t it be wonderful there\?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        .Count;

    if (repeatedLineCount < 2)
    {
        issues.Add(new TextQualityIssue(
            "Song",
            recordId,
            title,
            sourcePath,
            section,
            "MissingRepeatedSongLine",
            text,
            text,
            "High",
            "Regression check: Songs 116 Slide 2 should preserve the repeated final line from the PowerPoint."));
    }
}

static async Task ApplySafeSermonFixesAsync(SqliteConnection connection, IReadOnlyCollection<TextQualityIssue> fixes)
{
    await using var transaction = await connection.BeginTransactionAsync();

    foreach (var fix in fixes)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            UPDATE "SermonParagraphs"
            SET "Text" = $text,
                "SearchText" = $searchText
            WHERE "Id" = $id;
            """;
        command.Parameters.AddWithValue("$text", fix.SuggestedAfterText);
        command.Parameters.AddWithValue("$searchText", NormalizeSearchText(fix.SuggestedAfterText));
        command.Parameters.AddWithValue("$id", fix.RecordId);
        await command.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
}

static async Task WriteCsvAsync(
    string path,
    IReadOnlyCollection<TextQualityIssue> issues,
    IReadOnlyCollection<TextQualityIssue> autoFixes,
    bool reportOnly)
{
    var fixedIds = autoFixes.Select(issue => issue.RecordId).ToHashSet();
    await using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    await writer.WriteLineAsync(
        "contentType,titleOrReference,sourcePath,sectionOrParagraph,issueType,beforeText,suggestedAfterText,severity,notes");

    foreach (var issue in issues.OrderByDescending(SortSeverity).ThenBy(issue => issue.ContentType).ThenBy(issue => issue.TitleOrReference))
    {
        var notes = issue.Notes;
        if (issue.CanAutoFix && fixedIds.Contains(issue.RecordId))
        {
            notes = reportOnly
                ? $"{notes} Safe fix was available but not applied because --report-only was used."
                : $"{notes} Automatically fixed in SermonParagraphs.Text and SearchText.";
        }

        await writer.WriteLineAsync(string.Join(
            ',',
            Csv(issue.ContentType),
            Csv(issue.TitleOrReference),
            Csv(issue.SourcePath),
            Csv(issue.SectionOrParagraph),
            Csv(issue.IssueType),
            Csv(issue.BeforeText),
            Csv(issue.SuggestedAfterText),
            Csv(issue.Severity),
            Csv(notes)));
    }
}

static async Task WriteReportAsync(
    string path,
    string databasePath,
    IReadOnlyCollection<TextQualityIssue> beforeIssues,
    IReadOnlyCollection<TextQualityIssue> remainingIssues,
    IReadOnlyCollection<TextQualityIssue> autoFixes,
    bool reportOnly)
{
    var highBefore = beforeIssues.Count(issue => issue.Severity == "High");
    var highRemaining = remainingIssues.Count(issue => issue.Severity == "High");
    var mediumRemaining = remainingIssues.Count(issue => issue.Severity == "Medium");
    var lowRemaining = remainingIssues.Count(issue => issue.Severity == "Low");

    var builder = new StringBuilder();
    builder.AppendLine("MessageFlow Final Projection Text Quality Audit");
    builder.AppendLine("================================================");
    builder.AppendLine($"Run time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"Sermon scan scope: {BranhamPdfRoot}");
    builder.AppendLine();
    builder.AppendLine($"Issues before safe fixes: {beforeIssues.Count:N0}");
    builder.AppendLine($"High-severity issues before safe fixes: {highBefore:N0}");
    builder.AppendLine($"Safe sermon spacing fixes available: {autoFixes.Count:N0}");
    builder.AppendLine($"Safe sermon spacing fixes applied: {(reportOnly ? 0 : autoFixes.Count):N0}");
    builder.AppendLine($"High-severity issues remaining: {highRemaining:N0}");
    builder.AppendLine($"Medium-severity issues remaining: {mediumRemaining:N0}");
    builder.AppendLine($"Low-severity review items remaining: {lowRemaining:N0}");
    builder.AppendLine();
    builder.AppendLine("Recommended import/projection strategy");
    builder.AppendLine("- Keep Bible wording exact from the local KJV database.");
    builder.AppendLine("- Keep song lyric lines exact from the local PowerPoint files; use the song accuracy audit for source-to-database comparison.");
    builder.AppendLine("- Normalize only obvious sermon PDF letter-spacing artifacts such as T HE, W ORD, or S POKEN.");
    builder.AppendLine("- Leave projection pagination enabled for long sermon paragraphs rather than shrinking below readable size.");
    builder.AppendLine();

    if (autoFixes.Count > 0)
    {
        builder.AppendLine(reportOnly ? "Safe fixes available" : "Safe fixes applied");
        builder.AppendLine("------------------");
        foreach (var fix in autoFixes.OrderBy(fix => fix.TitleOrReference).ThenBy(fix => fix.SectionOrParagraph))
        {
            builder.AppendLine($"{fix.TitleOrReference} | {fix.SectionOrParagraph}");
            builder.AppendLine($"Before: {Shorten(fix.BeforeText, 220)}");
            builder.AppendLine($"After : {Shorten(fix.SuggestedAfterText, 220)}");
            builder.AppendLine();
        }
    }

    if (highRemaining > 0)
    {
        builder.AppendLine("Remaining high-severity issues");
        builder.AppendLine("------------------------------");
        foreach (var issue in remainingIssues.Where(issue => issue.Severity == "High").Take(50))
        {
            builder.AppendLine($"{issue.ContentType} | {issue.TitleOrReference} | {issue.SectionOrParagraph} | {issue.IssueType}");
            builder.AppendLine(Shorten(issue.BeforeText, 220));
            builder.AppendLine();
        }
    }

    await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static string NormalizeSearchText(string text)
{
    return ProjectionTextCleaner.CleanSermonText(text).ToUpperInvariant();
}

static string CleanForComparison(string text)
{
    return string.Join(' ', text.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

static int CountWords(string text)
{
    return string.IsNullOrWhiteSpace(text)
        ? 0
        : text.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
}

static int CountNonEmptyLines(string text)
{
    return text
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Split('\n')
        .Count(line => !string.IsNullOrWhiteSpace(line));
}

static int SortSeverity(TextQualityIssue issue)
{
    return issue.Severity switch
    {
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };
}

static string Shorten(string text, int maxLength = 500)
{
    var flattened = CleanForComparison(text);
    return flattened.Length <= maxLength
        ? flattened
        : $"{flattened[..maxLength].TrimEnd()}...";
}

static string Csv(string value)
{
    return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

static bool HasObviousNonSermonLetterSpacing(string text)
{
    return TextQualityRegex.NonSermonLetterSpacingRegex().IsMatch(text);
}

static partial class TextQualityRegex
{
    [GeneratedRegex("[@#%]", RegexOptions.CultureInvariant)]
    public static partial Regex WeirdSymbolRegex();

    [GeneratedRegex("[ \\t]{3,}", RegexOptions.CultureInvariant)]
    public static partial Regex ExcessiveSpaceRegex();

    [GeneratedRegex(@"(?<![A-Za-z])(?:[A-Z]\s+){2,}[A-Z](?![A-Za-z])", RegexOptions.CultureInvariant)]
    public static partial Regex NonSermonLetterSpacingRegex();
}

sealed record TextQualityIssue(
    string ContentType,
    int RecordId,
    string TitleOrReference,
    string SourcePath,
    string SectionOrParagraph,
    string IssueType,
    string BeforeText,
    string SuggestedAfterText,
    string Severity,
    string Notes,
    bool CanAutoFix = false);
