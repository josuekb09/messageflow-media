using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Data;
using Microsoft.Data.Sqlite;

const string DefaultBranhamPdfRoot = @"D:\Br William Marrion Branham\PDF";
const string ReportDirectory = @"D:\MessageFlow Archive\BranhamAudit";
const string TextReportPath = ReportDirectory + @"\branham_import_quality_report.txt";
const string CsvReportPath = ReportDirectory + @"\branham_suspicious_paragraphs.csv";

var databasePath = MessageFlowDatabase.DefaultDatabasePath;
if (!File.Exists(databasePath))
{
    Console.WriteLine($"ERROR Database not found: {databasePath}");
    return 1;
}

Directory.CreateDirectory(ReportDirectory);

var pdfIndex = BuildPdfIndex(DefaultBranhamPdfRoot);
var findings = new List<Finding>();
var reasonCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var mappedSermons = new HashSet<int>();
var unmappedSermons = new HashSet<int>();
var scannedSermons = new HashSet<int>();
var paragraphsScanned = 0;

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

await using (var command = connection.CreateCommand())
{
    command.CommandText =
        """
        SELECT
            s."Id" AS SermonId,
            s."SermonCode",
            s."Title",
            s."Year",
            s."SourceFilePath",
            COALESCE(cs."LocalFolderPath", '') AS LocalFolderPath,
            p."ParagraphNumber",
            p."Text"
        FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        LEFT JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        LEFT JOIN "Authors" a ON a."Id" = s."AuthorId"
        WHERE cs."Name" = 'brother_branham'
           OR cs."DisplayName" LIKE '%Branham%'
           OR a."DisplayName" LIKE '%Branham%'
           OR a."FullName" LIKE '%Branham%'
           OR s."SourceFilePath" LIKE '%Branham%'
        ORDER BY s."Year", s."SermonCode", p."ParagraphNumber";
        """;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var sermonId = reader.GetInt32(0);
        var sermonCode = reader.GetString(1);
        var title = reader.GetString(2);
        var year = reader.GetInt32(3);
        var sourceFilePath = reader.GetString(4);
        var localFolderPath = reader.GetString(5);
        var paragraphNumber = reader.GetInt32(6);
        var text = reader.GetString(7);

        paragraphsScanned++;
        scannedSermons.Add(sermonId);

        var possibleSourcePdfPath = ResolveSourcePdfPath(
            sourceFilePath,
            localFolderPath,
            sermonCode,
            title,
            year,
            pdfIndex);

        if (string.IsNullOrWhiteSpace(possibleSourcePdfPath))
        {
            unmappedSermons.Add(sermonId);
        }
        else
        {
            mappedSermons.Add(sermonId);
        }

        var reasons = FindSuspiciousReasons(text);
        if (reasons.Count == 0)
        {
            continue;
        }

        foreach (var reason in reasons)
        {
            reasonCounts[reason] = reasonCounts.TryGetValue(reason, out var count) ? count + 1 : 1;
        }

        findings.Add(new Finding(
            sermonCode,
            title,
            year,
            paragraphNumber,
            string.Join("; ", reasons),
            CreateSnippet(text),
            possibleSourcePdfPath));
    }
}

await WriteCsvAsync(CsvReportPath, findings);
await WriteTextReportAsync(
    TextReportPath,
    databasePath,
    DefaultBranhamPdfRoot,
    scannedSermons.Count,
    paragraphsScanned,
    mappedSermons.Count,
    unmappedSermons.Count,
    findings,
    reasonCounts);

Console.WriteLine($"Scanned sermons: {scannedSermons.Count:N0}");
Console.WriteLine($"Scanned paragraphs: {paragraphsScanned:N0}");
Console.WriteLine($"Suspicious paragraphs: {findings.Count:N0}");
Console.WriteLine($"Mapped source PDFs: {mappedSermons.Count:N0}");
Console.WriteLine($"Unmapped source PDFs: {unmappedSermons.Count:N0}");
Console.WriteLine($"Report: {TextReportPath}");
Console.WriteLine($"CSV: {CsvReportPath}");

return 0;

static IReadOnlyDictionary<string, string> BuildPdfIndex(string root)
{
    if (!Directory.Exists(root))
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var path in Directory.EnumerateFiles(root, "*.pdf", SearchOption.AllDirectories))
    {
        var fileName = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            index.TryAdd(fileName, path);
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            index.TryAdd(NormalizeKey(fileNameWithoutExtension), path);
        }
    }

    return index;
}

static string ResolveSourcePdfPath(
    string sourceFilePath,
    string localFolderPath,
    string sermonCode,
    string title,
    int year,
    IReadOnlyDictionary<string, string> pdfIndex)
{
    foreach (var candidate in EnumerateSourceCandidates(sourceFilePath, localFolderPath, sermonCode, title, year))
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }

        var fileName = Path.GetFileName(candidate);
        if (!string.IsNullOrWhiteSpace(fileName) && pdfIndex.TryGetValue(fileName, out var byFileName))
        {
            return byFileName;
        }

        var key = NormalizeKey(Path.GetFileNameWithoutExtension(candidate));
        if (!string.IsNullOrWhiteSpace(key) && pdfIndex.TryGetValue(key, out var byKey))
        {
            return byKey;
        }
    }

    return string.Empty;
}

static IEnumerable<string> EnumerateSourceCandidates(
    string sourceFilePath,
    string localFolderPath,
    string sermonCode,
    string title,
    int year)
{
    if (!string.IsNullOrWhiteSpace(sourceFilePath))
    {
        yield return sourceFilePath;

        if (!Path.IsPathRooted(sourceFilePath))
        {
            if (!string.IsNullOrWhiteSpace(localFolderPath))
            {
                yield return Path.Combine(localFolderPath, sourceFilePath);
            }

            yield return Path.Combine(DefaultBranhamPdfRoot, sourceFilePath);
        }
    }

    var normalizedCode = sermonCode.Trim();
    if (!string.IsNullOrWhiteSpace(normalizedCode))
    {
        yield return Path.Combine(DefaultBranhamPdfRoot, year.ToString(), $"{normalizedCode}.pdf");
        yield return Path.Combine(DefaultBranhamPdfRoot, $"{normalizedCode}.pdf");
    }

    var normalizedTitle = string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    if (!string.IsNullOrWhiteSpace(normalizedTitle))
    {
        yield return Path.Combine(DefaultBranhamPdfRoot, year.ToString(), $"{normalizedTitle}.pdf");
    }
}

static IReadOnlyList<string> FindSuspiciousReasons(string text)
{
    var reasons = new List<string>();
    var normalized = NormalizeWhitespace(text);
    var lower = normalized.ToLowerInvariant();

    if (normalized.Contains('@', StringComparison.Ordinal))
    {
        reasons.Add("Contains @ symbol");
    }

    if (normalized.Contains('#', StringComparison.Ordinal))
    {
        reasons.Add("Contains # symbol");
    }

    if (normalized.Contains('%', StringComparison.Ordinal))
    {
        reasons.Add("Contains % symbol");
    }

    if (AuditRegexes.RepeatedSymbolRegex().IsMatch(normalized))
    {
        reasons.Add("Repeated symbols");
    }

    if (AuditRegexes.BrokenUppercaseWordRegex().IsMatch(normalized) ||
        AuditRegexes.BrokenLowercaseWordRegex().IsMatch(normalized))
    {
        reasons.Add("Broken spaced letters");
    }

    if (AuditRegexes.WeirdEncodingRegex().IsMatch(normalized) || normalized.Contains('\uFFFD'))
    {
        reasons.Add("Weird encoding characters");
    }

    if (AuditRegexes.ControlCharacterRegex().IsMatch(text))
    {
        reasons.Add("Unexpected control characters");
    }

    if (AuditRegexes.RepeatedWhitespaceRegex().IsMatch(text))
    {
        reasons.Add("Broken or repeated spacing");
    }

    if (LooksLikeCopyrightOrFooter(lower))
    {
        reasons.Add("Copyright or footer-looking text");
    }

    if (LooksLikePdfHeaderOrFooter(normalized))
    {
        reasons.Add("PDF header/footer-looking text");
    }

    if (LooksLikeVeryShortJunk(normalized))
    {
        reasons.Add("Very short junk paragraph");
    }

    return reasons;
}

static bool LooksLikeCopyrightOrFooter(string lower)
{
    return lower.Contains("copyright", StringComparison.Ordinal) ||
           lower.Contains("all rights reserved", StringComparison.Ordinal) ||
           lower.Contains("voice of god recordings", StringComparison.Ordinal) ||
           lower.Contains("printed in", StringComparison.Ordinal) ||
           lower.Contains("www.", StringComparison.Ordinal) ||
           lower.Contains("http://", StringComparison.Ordinal) ||
           lower.Contains("https://", StringComparison.Ordinal);
}

static bool LooksLikePdfHeaderOrFooter(string text)
{
    var trimmed = text.Trim();
    return AuditRegexes.PageOnlyRegex().IsMatch(trimmed) ||
           AuditRegexes.PageLabelRegex().IsMatch(trimmed) ||
           AuditRegexes.BranhamPdfHeaderRegex().IsMatch(trimmed) ||
           trimmed.Equals("William Marrion Branham", StringComparison.OrdinalIgnoreCase);
}

static bool LooksLikeVeryShortJunk(string text)
{
    var trimmed = text.Trim();
    return trimmed.Length is > 0 and <= 4 &&
           !trimmed.Any(char.IsLetterOrDigit);
}

static string CreateSnippet(string text)
{
    var snippet = NormalizeWhitespace(text);
    return snippet.Length <= 280 ? snippet : snippet[..280] + "...";
}

static string NormalizeWhitespace(string value)
{
    return string.Join(' ', value.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

static string NormalizeKey(string value)
{
    var builder = new StringBuilder(value.Length);
    foreach (var character in value.ToLowerInvariant())
    {
        if (char.IsLetterOrDigit(character))
        {
            builder.Append(character);
        }
    }

    return builder.ToString();
}

static async Task WriteCsvAsync(string path, IReadOnlyList<Finding> findings)
{
    await using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    await writer.WriteLineAsync("sermon_code,title,year,paragraph_number,suspicious_reason,paragraph_text_snippet,possible_source_pdf_path");
    foreach (var finding in findings)
    {
        await writer.WriteLineAsync(string.Join(
            ',',
            Csv(finding.SermonCode),
            Csv(finding.Title),
            finding.Year.ToString(),
            finding.ParagraphNumber.ToString(),
            Csv(finding.SuspiciousReason),
            Csv(finding.ParagraphTextSnippet),
            Csv(finding.PossibleSourcePdfPath)));
    }
}

static async Task WriteTextReportAsync(
    string path,
    string databasePath,
    string pdfRoot,
    int sermonCount,
    int paragraphCount,
    int mappedSermonCount,
    int unmappedSermonCount,
    IReadOnlyList<Finding> findings,
    IReadOnlyDictionary<string, int> reasonCounts)
{
    await using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    await writer.WriteLineAsync("Brother Branham Import Quality Audit");
    await writer.WriteLineAsync("====================================");
    await writer.WriteLineAsync();
    await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    await writer.WriteLineAsync($"Database: {databasePath}");
    await writer.WriteLineAsync($"PDF root: {pdfRoot}");
    await writer.WriteLineAsync("Mode: read-only audit; no database changes were made.");
    await writer.WriteLineAsync();
    await writer.WriteLineAsync($"Sermons scanned: {sermonCount:N0}");
    await writer.WriteLineAsync($"Paragraphs scanned: {paragraphCount:N0}");
    await writer.WriteLineAsync($"Suspicious paragraphs: {findings.Count:N0}");
    await writer.WriteLineAsync($"Sermons mapped to possible PDFs: {mappedSermonCount:N0}");
    await writer.WriteLineAsync($"Sermons without mapped PDF path: {unmappedSermonCount:N0}");
    await writer.WriteLineAsync();
    await writer.WriteLineAsync("Reason counts:");
    foreach (var reason in reasonCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key))
    {
        await writer.WriteLineAsync($"- {reason.Key}: {reason.Value:N0}");
    }

    await writer.WriteLineAsync();
    await writer.WriteLineAsync("First suspicious paragraphs:");
    foreach (var finding in findings.Take(75))
    {
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"{finding.SermonCode} | {finding.Title} | {finding.Year} | Paragraph {finding.ParagraphNumber}");
        await writer.WriteLineAsync($"Reason: {finding.SuspiciousReason}");
        await writer.WriteLineAsync($"Source PDF: {finding.PossibleSourcePdfPath}");
        await writer.WriteLineAsync($"Snippet: {finding.ParagraphTextSnippet}");
    }

    await writer.WriteLineAsync();
    await writer.WriteLineAsync($"Full CSV: {CsvReportPath}");
}

static string Csv(string value)
{
    return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}

internal sealed record Finding(
    string SermonCode,
    string Title,
    int Year,
    int ParagraphNumber,
    string SuspiciousReason,
    string ParagraphTextSnippet,
    string PossibleSourcePdfPath);

internal static partial class AuditRegexes
{
    [GeneratedRegex(@"([@#%*_=~\-])\1{2,}")]
    public static partial Regex RepeatedSymbolRegex();

    [GeneratedRegex(@"\b(?:[A-Z]\s+){2,}[A-Z]\b")]
    public static partial Regex BrokenUppercaseWordRegex();

    [GeneratedRegex(@"\b(?:[a-z]\s+){4,}[a-z]\b")]
    public static partial Regex BrokenLowercaseWordRegex();

    [GeneratedRegex(@"[ÃÂâ€]")]
    public static partial Regex WeirdEncodingRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]")]
    public static partial Regex ControlCharacterRegex();

    [GeneratedRegex(@"[ \t]{4,}")]
    public static partial Regex RepeatedWhitespaceRegex();

    [GeneratedRegex(@"^(?:page\s*)?\d{1,4}$", RegexOptions.IgnoreCase)]
    public static partial Regex PageOnlyRegex();

    [GeneratedRegex(@"^page\s+\d+\s+(?:of\s+\d+)?$", RegexOptions.IgnoreCase)]
    public static partial Regex PageLabelRegex();

    [GeneratedRegex(@"^\d{2}-\d{4}\s+.*$", RegexOptions.IgnoreCase)]
    public static partial Regex BranhamPdfHeaderRegex();
}
