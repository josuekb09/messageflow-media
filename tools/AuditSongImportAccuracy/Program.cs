using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;

const string SongRoot = @"D:\SONG PRESENTATION";
const string ChoirRoot = @"D:\SONG PRESENTATION\choir";
const string IgnoredFolderName = "chruch service";
const string OutputDirectory = @"D:\MessageFlow Archive\SongImportTest";
const string ReportPath = OutputDirectory + @"\song_import_accuracy_report.txt";
const string CsvPath = OutputDirectory + @"\song_import_accuracy_issues.csv";

Directory.CreateDirectory(OutputDirectory);

var databasePath = args.FirstOrDefault(arg => !string.IsNullOrWhiteSpace(arg)) ?? MessageFlowDatabase.DefaultDatabasePath;
if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return 1;
}

var files = DiscoverPresentationFiles([SongRoot, ChoirRoot], IgnoredFolderName);
var expectedSongs = files.Select(ExtractSong).ToList();
var importedSongs = await LoadImportedSongsAsync(databasePath);
var issues = new List<AccuracyIssue>();

foreach (var expectedSong in expectedSongs)
{
    if (!expectedSong.Success)
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            0,
            "ExtractionFailed",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            expectedSong.Error));
        continue;
    }

    if (!importedSongs.TryGetValue(expectedSong.SourceFile, out var importedSong))
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            0,
            "MissingImportedSong",
            string.Join($"{Environment.NewLine}{Environment.NewLine}", expectedSong.Sections.Select(section => section.Text)),
            string.Empty,
            string.Empty,
            string.Empty,
            "No active Songs row was found for this source file."));
        continue;
    }

    if (expectedSong.Sections.Count != importedSong.Sections.Count)
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            importedSong.Title,
            0,
            "SlideCountMismatch",
            expectedSong.Sections.Count.ToString(),
            importedSong.Sections.Count.ToString(),
            string.Empty,
            string.Empty,
            "The number of non-empty PowerPoint slides does not match imported song sections."));
    }

    var compareCount = Math.Min(expectedSong.Sections.Count, importedSong.Sections.Count);
    for (var index = 0; index < compareCount; index++)
    {
        CompareSection(expectedSong, expectedSong.Sections[index], importedSong.Sections[index], issues);
    }

    AddMissingSectionIssues(expectedSong, importedSong, issues);
    AddRegression116IssueIfNeeded(expectedSong, importedSong, issues);
}

await WriteCsvAsync(CsvPath, issues);
await WriteReportAsync(ReportPath, databasePath, expectedSongs, importedSongs, issues);

Console.WriteLine($"PowerPoint files audited: {expectedSongs.Count:N0}");
Console.WriteLine($"Imported songs found: {importedSongs.Count:N0}");
Console.WriteLine($"Accuracy issues found: {issues.Count:N0}");
Console.WriteLine($"Report: {ReportPath}");
Console.WriteLine($"CSV: {CsvPath}");

return issues.Count == 0 ? 0 : 2;

static List<string> DiscoverPresentationFiles(IEnumerable<string> roots, string ignoredFolderName)
{
    var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var root in roots)
    {
        if (!Directory.Exists(root))
        {
            continue;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (IsUnderIgnoredFolder(file, ignoredFolderName))
            {
                continue;
            }

            var extension = Path.GetExtension(file);
            if (!extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Path.GetFileName(file).StartsWith("~$", StringComparison.Ordinal))
            {
                continue;
            }

            files.Add(Path.GetFullPath(file));
        }
    }

    return files.ToList();
}

static bool IsUnderIgnoredFolder(string file, string ignoredFolderName)
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(file));
    while (!string.IsNullOrWhiteSpace(directory))
    {
        if (string.Equals(Path.GetFileName(directory), ignoredFolderName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        directory = Path.GetDirectoryName(directory);
    }

    return false;
}

static ExpectedSong ExtractSong(string sourceFile)
{
    var extension = Path.GetExtension(sourceFile);
    if (!extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase))
    {
        return new ExpectedSong(
            Path.GetFullPath(sourceFile),
            CleanTitle(Path.GetFileNameWithoutExtension(sourceFile)),
            false,
            "Only .pptx direct XML auditing is enabled. Legacy .ppt files should be converted or inspected separately.",
            []);
    }

    try
    {
        using var archive = ZipFile.OpenRead(sourceFile);
        var slideEntries = ResolveSlideEntries(archive);
        var sections = new List<ExpectedSection>();

        for (var index = 0; index < slideEntries.Count; index++)
        {
            var entry = archive.GetEntry(slideEntries[index]);
            if (entry is null)
            {
                continue;
            }

            var slideNumber = index + 1;
            var cleanedLines = ExtractTextFromSlideXml(entry)
                .Select(CleanLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (cleanedLines.Count == 0)
            {
                continue;
            }

            var sectionType = DetectSectionType(cleanedLines);
            sections.Add(new ExpectedSection(
                slideNumber,
                sections.Count + 1,
                CreateSectionLabel(sectionType, slideNumber),
                string.Join(Environment.NewLine, cleanedLines)));
        }

        return new ExpectedSong(
            Path.GetFullPath(sourceFile),
            DetectTitle(sourceFile, sections),
            true,
            string.Empty,
            sections);
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException)
    {
        return new ExpectedSong(
            Path.GetFullPath(sourceFile),
            CleanTitle(Path.GetFileNameWithoutExtension(sourceFile)),
            false,
            ex.Message,
            []);
    }
}

static List<string> ResolveSlideEntries(ZipArchive archive)
{
    var presentationEntry = archive.GetEntry("ppt/presentation.xml");
    var relationshipsEntry = archive.GetEntry("ppt/_rels/presentation.xml.rels");

    if (presentationEntry is not null && relationshipsEntry is not null)
    {
        try
        {
            using var presentationStream = presentationEntry.Open();
            using var relationshipsStream = relationshipsEntry.Open();
            var presentation = XDocument.Load(presentationStream);
            var relationships = XDocument.Load(relationshipsStream);
            XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
            XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";

            var relationshipTargets = relationships
                .Root?
                .Elements(rel + "Relationship")
                .Where(element => (string?)element.Attribute("Target") is not null)
                .ToDictionary(
                    element => (string)element.Attribute("Id")!,
                    element => NormalizePartPath("ppt", (string)element.Attribute("Target")!),
                    StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var ordered = presentation
                .Descendants(p + "sldId")
                .Select(element => (string?)element.Attribute(r + "id"))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => relationshipTargets.TryGetValue(id!, out var target) ? target : string.Empty)
                .Where(target => target.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ordered.Count > 0)
            {
                return ordered;
            }
        }
        catch
        {
            // Fall back to numeric slide entry order.
        }
    }

    return archive
        .Entries
        .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        .OrderBy(entry => ExtractSlideNumber(entry.FullName))
        .Select(entry => entry.FullName)
        .ToList();
}

static string NormalizePartPath(string baseFolder, string target)
{
    var combined = target.StartsWith("/", StringComparison.Ordinal)
        ? target.TrimStart('/')
        : $"{baseFolder}/{target}";

    var parts = new Stack<string>();
    foreach (var part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
    {
        if (part == ".")
        {
            continue;
        }

        if (part == "..")
        {
            if (parts.Count > 0)
            {
                parts.Pop();
            }

            continue;
        }

        parts.Push(part);
    }

    return string.Join('/', parts.Reverse());
}

static int ExtractSlideNumber(string entryName)
{
    var match = Regex.Match(entryName, @"slide(?<number>\d+)\.xml$", RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups["number"].Value, out var number) ? number : int.MaxValue;
}

static List<string> ExtractTextFromSlideXml(ZipArchiveEntry entry)
{
    using var stream = entry.Open();
    var document = XDocument.Load(stream);
    XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";

    return document
        .Descendants(a + "p")
        .Select(paragraph => string.Concat(paragraph.Descendants(a + "t").Select(element => element.Value ?? string.Empty)).Trim())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToList();
}

static string CleanLine(string value)
{
    var cleaned = value
        .Replace('\u00A0', ' ')
        .Replace('\u200B', ' ')
        .Replace('\u200C', ' ')
        .Replace('\u200D', ' ')
        .Replace('\uFEFF', ' ')
        .Replace('\u00AD', '\0');

    cleaned = SoftHyphenJoinRegex().Replace(cleaned, string.Empty);
    cleaned = cleaned.Replace("\0", string.Empty, StringComparison.Ordinal);
    cleaned = SpacedLettersRegex().Replace(cleaned, match => match.Value.Replace(" ", string.Empty, StringComparison.Ordinal));
    cleaned = ControlCharactersRegex().Replace(cleaned, string.Empty);
    cleaned = WhitespaceRegex().Replace(cleaned, " ");
    return cleaned.Trim();
}

static string DetectSectionType(IReadOnlyList<string> lines)
{
    var firstLine = lines.FirstOrDefault() ?? string.Empty;
    if (Regex.IsMatch(firstLine, @"^\s*(chorus|ch:|refrain)\b", RegexOptions.IgnoreCase))
    {
        return "Chorus";
    }

    if (Regex.IsMatch(firstLine, @"^\s*(verse|vs\.?|v)\s*\d*", RegexOptions.IgnoreCase))
    {
        return "Verse";
    }

    if (Regex.IsMatch(firstLine, @"^\s*(bridge|ending|tag)\b", RegexOptions.IgnoreCase))
    {
        var word = firstLine.Split(' ', ':', '-').First();
        return string.IsNullOrWhiteSpace(word) ? "Slide" : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }

    return "Slide";
}

static string CreateSectionLabel(string sectionType, int slideNumber)
{
    return sectionType.Equals("Slide", StringComparison.OrdinalIgnoreCase)
        ? $"Slide {slideNumber}"
        : $"{sectionType} - Slide {slideNumber}";
}

static string DetectTitle(string sourceFile, IReadOnlyList<ExpectedSection> sections)
{
    var fileTitle = CleanTitle(Path.GetFileNameWithoutExtension(sourceFile));
    var firstLine = sections
        .Select(section => section.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
        .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

    if (!string.IsNullOrWhiteSpace(firstLine) &&
        firstLine.Length <= 80 &&
        SongTextNormalizer.Normalize(firstLine).Length >= 4 &&
        !Regex.IsMatch(firstLine, @"^\s*(verse|chorus|refrain|slide)\b", RegexOptions.IgnoreCase))
    {
        return CleanTitle(firstLine);
    }

    return fileTitle;
}

static string CleanTitle(string value)
{
    var cleaned = CleanLine(value)
        .Replace('_', ' ')
        .Replace('-', ' ');
    cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();
    return string.IsNullOrWhiteSpace(cleaned) ? "Untitled Song" : cleaned;
}

static async Task<Dictionary<string, ImportedSong>> LoadImportedSongsAsync(string databasePath)
{
    var songs = new Dictionary<string, ImportedSong>(StringComparer.OrdinalIgnoreCase);
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        SELECT
            s."SourceFilePath",
            s."Title",
            ss."SectionOrder",
            ss."SectionLabel",
            ss."Text"
        FROM "Songs" s
        LEFT JOIN "SongSections" ss ON ss."SongId" = s."Id"
        WHERE s."IsActive" = 1
        ORDER BY s."SourceFilePath", ss."SectionOrder";
        """;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var sourceFile = reader.GetString(0);
        if (!songs.TryGetValue(sourceFile, out var song))
        {
            song = new ImportedSong(sourceFile, reader.GetString(1), []);
            songs[sourceFile] = song;
        }

        if (!reader.IsDBNull(2))
        {
            song.Sections.Add(new ImportedSection(
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4)));
        }
    }

    return songs;
}

static void CompareSection(
    ExpectedSong expectedSong,
    ExpectedSection expectedSection,
    ImportedSection actualSection,
    ICollection<AccuracyIssue> issues)
{
    if (string.Equals(expectedSection.Text, actualSection.Text, StringComparison.Ordinal))
    {
        return;
    }

    var expectedLines = SplitLines(expectedSection.Text);
    var actualLines = SplitLines(actualSection.Text);
    var missingLines = FindMissingLines(expectedLines, actualLines);
    var changedLines = FindChangedLines(expectedLines, actualLines);
    var expectedLast = expectedLines.LastOrDefault() ?? string.Empty;
    var actualLast = actualLines.LastOrDefault() ?? string.Empty;

    if (missingLines.Count > 0)
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "MissingLines",
            expectedSection.Text,
            actualSection.Text,
            string.Join(" | ", missingLines),
            string.Empty,
            "One or more expected slide lines are absent from the imported section."));
    }

    var repeatedLinesRemoved = expectedLines
        .GroupBy(line => line, StringComparer.Ordinal)
        .Where(group => group.Count() > actualLines.Count(line => string.Equals(line, group.Key, StringComparison.Ordinal)) &&
                        group.Count() > 1)
        .Select(group => group.Key)
        .ToList();
    if (repeatedLinesRemoved.Count > 0)
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "RepeatedLinesRemoved",
            expectedSection.Text,
            actualSection.Text,
            string.Join(" | ", repeatedLinesRemoved),
            string.Empty,
            "A line appears repeatedly in PowerPoint but fewer times in the database."));
    }

    if (!string.Equals(expectedLast, actualLast, StringComparison.Ordinal))
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "MissingTrailingLines",
            expectedSection.Text,
            actualSection.Text,
            expectedLast,
            string.Empty,
            "The final imported line does not match the final PowerPoint line."));
    }

    if (actualSection.Text.Length < expectedSection.Text.Length &&
        expectedSection.Text.StartsWith(actualSection.Text, StringComparison.Ordinal))
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "SectionTextTruncated",
            expectedSection.Text,
            actualSection.Text,
            string.Empty,
            string.Empty,
            "Imported text is a leading prefix of the PowerPoint slide text."));
    }

    if (changedLines.Count > 0 && missingLines.Count == 0)
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "ChangedWording",
            expectedSection.Text,
            actualSection.Text,
            string.Empty,
            string.Join(" | ", changedLines),
            "Expected and actual lines differ at the same positions."));
    }

    if (SongTextNormalizer.Normalize(expectedSection.Text) == SongTextNormalizer.Normalize(actualSection.Text) &&
        !string.Equals(expectedSection.Text, actualSection.Text, StringComparison.Ordinal))
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            expectedSong.Title,
            expectedSection.SlideNumber,
            "SuspiciousFormattingChange",
            expectedSection.Text,
            actualSection.Text,
            string.Empty,
            string.Join(" | ", changedLines),
            "Normalized words match, but punctuation, line breaks, or capitalization changed."));
    }
}

static void AddMissingSectionIssues(
    ExpectedSong expectedSong,
    ImportedSong importedSong,
    ICollection<AccuracyIssue> issues)
{
    if (expectedSong.Sections.Count <= importedSong.Sections.Count)
    {
        return;
    }

    foreach (var missingSection in expectedSong.Sections.Skip(importedSong.Sections.Count))
    {
        issues.Add(new AccuracyIssue(
            expectedSong.SourceFile,
            importedSong.Title,
            missingSection.SlideNumber,
            "MissingSection",
            missingSection.Text,
            string.Empty,
            string.Join(" | ", SplitLines(missingSection.Text)),
            string.Empty,
            "PowerPoint has a non-empty slide without a matching imported section."));
    }
}

static void AddRegression116IssueIfNeeded(
    ExpectedSong expectedSong,
    ImportedSong importedSong,
    ICollection<AccuracyIssue> issues)
{
    var fileName = Path.GetFileName(expectedSong.SourceFile);
    if (!fileName.StartsWith("116.", StringComparison.OrdinalIgnoreCase) ||
        !fileName.Contains("WON", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var expectedSlide = expectedSong.Sections.FirstOrDefault(section => section.SlideNumber == 2);
    var expectedSlideIndex = expectedSlide is null
        ? -1
        : expectedSong.Sections
            .Select((section, index) => new { section, index })
            .FirstOrDefault(item => item.section.SlideNumber == 2)?.index ?? -1;
    var actualSlide = expectedSlideIndex >= 0 && expectedSlideIndex < importedSong.Sections.Count
        ? importedSong.Sections[expectedSlideIndex]
        : null;
    const string repeatedLine = "Won’t it be wonderful there?";
    var expectedCount = expectedSlide is null ? 0 : SplitLines(expectedSlide.Text).Count(line => line == repeatedLine);
    var actualLines = actualSlide is null ? new List<string>() : SplitLines(actualSlide.Text);
    var actualCount = actualLines.Count(line => line == repeatedLine);
    var actualEndsWithRepeatedLine = actualLines.LastOrDefault() == repeatedLine;

    if (expectedCount < 2 || actualCount >= expectedCount && actualEndsWithRepeatedLine)
    {
        return;
    }

    issues.Add(new AccuracyIssue(
        expectedSong.SourceFile,
        importedSong.Title,
        2,
        "Regression116RepeatedEndingMissing",
        expectedSlide?.Text ?? string.Empty,
        actualSlide?.Text ?? string.Empty,
        repeatedLine,
        string.Empty,
        "116. WON’T IT BE WONDERFUL slide 2 must include the final repeated ending line."));
}

static List<string> SplitLines(string text)
{
    return text
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}

static List<string> FindMissingLines(IReadOnlyList<string> expectedLines, IReadOnlyList<string> actualLines)
{
    var actualCounts = actualLines
        .GroupBy(line => line, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    var missing = new List<string>();

    foreach (var expectedLine in expectedLines)
    {
        if (!actualCounts.TryGetValue(expectedLine, out var count) || count == 0)
        {
            missing.Add(expectedLine);
            continue;
        }

        actualCounts[expectedLine] = count - 1;
    }

    return missing;
}

static List<string> FindChangedLines(IReadOnlyList<string> expectedLines, IReadOnlyList<string> actualLines)
{
    var changes = new List<string>();
    var count = Math.Min(expectedLines.Count, actualLines.Count);
    for (var index = 0; index < count; index++)
    {
        if (!string.Equals(expectedLines[index], actualLines[index], StringComparison.Ordinal))
        {
            changes.Add($"Line {index + 1}: expected [{expectedLines[index]}], actual [{actualLines[index]}]");
        }
    }

    if (expectedLines.Count != actualLines.Count)
    {
        changes.Add($"Line count expected {expectedLines.Count}, actual {actualLines.Count}");
    }

    return changes;
}

static async Task WriteCsvAsync(string csvPath, IReadOnlyList<AccuracyIssue> issues)
{
    var builder = new StringBuilder();
    builder.AppendLine("sourceFile,songTitle,slideNumber,issueType,expectedText,actualText,missingLines,changedLines,notes");
    foreach (var issue in issues)
    {
        builder.AppendLine(string.Join(
            ',',
            Csv(issue.SourceFile),
            Csv(issue.SongTitle),
            issue.SlideNumber.ToString(),
            Csv(issue.IssueType),
            Csv(issue.ExpectedText),
            Csv(issue.ActualText),
            Csv(issue.MissingLines),
            Csv(issue.ChangedLines),
            Csv(issue.Notes)));
    }

    await File.WriteAllTextAsync(csvPath, builder.ToString(), Encoding.UTF8);
}

static async Task WriteReportAsync(
    string reportPath,
    string databasePath,
    IReadOnlyList<ExpectedSong> expectedSongs,
    IReadOnlyDictionary<string, ImportedSong> importedSongs,
    IReadOnlyList<AccuracyIssue> issues)
{
    var regressionIssueCount = issues.Count(issue => issue.IssueType == "Regression116RepeatedEndingMissing");
    var builder = new StringBuilder();
    builder.AppendLine("MessageFlow Song Import Accuracy Report");
    builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"Source roots: {SongRoot}; {ChoirRoot}");
    builder.AppendLine($"Ignored folder: {IgnoredFolderName}");
    builder.AppendLine();
    builder.AppendLine($"PowerPoint files audited: {expectedSongs.Count:N0}");
    builder.AppendLine($"Imported active songs found: {importedSongs.Count:N0}");
    builder.AppendLine($"Accuracy issues found: {issues.Count:N0}");
    builder.AppendLine($"116 slide 2 regression issues: {regressionIssueCount:N0}");
    builder.AppendLine();

    foreach (var group in issues.GroupBy(issue => issue.IssueType).OrderBy(group => group.Key))
    {
        builder.AppendLine($"{group.Key}: {group.Count():N0}");
    }

    builder.AppendLine();
    builder.AppendLine("116. WON’T IT BE WONDERFUL slide 2 expected lines:");
    var regressionSong = expectedSongs.FirstOrDefault(song =>
        Path.GetFileName(song.SourceFile).StartsWith("116.", StringComparison.OrdinalIgnoreCase) &&
        Path.GetFileName(song.SourceFile).Contains("WON", StringComparison.OrdinalIgnoreCase));
    var regressionSlide = regressionSong?.Sections.FirstOrDefault(section => section.SlideNumber == 2);
    builder.AppendLine(regressionSlide?.Text ?? "Not found.");
    builder.AppendLine();
    builder.AppendLine("First issues:");

    foreach (var issue in issues.Take(40))
    {
        builder.AppendLine($"- {issue.IssueType}: {issue.SourceFile} slide {issue.SlideNumber}");
        if (!string.IsNullOrWhiteSpace(issue.MissingLines))
        {
            builder.AppendLine($"  Missing: {issue.MissingLines}");
        }

        if (!string.IsNullOrWhiteSpace(issue.ChangedLines))
        {
            builder.AppendLine($"  Changed: {issue.ChangedLines}");
        }

        if (!string.IsNullOrWhiteSpace(issue.Notes))
        {
            builder.AppendLine($"  Notes: {issue.Notes}");
        }
    }

    await File.WriteAllTextAsync(reportPath, builder.ToString(), Encoding.UTF8);
}

static string Csv(string value)
{
    return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

record ExpectedSong(
    string SourceFile,
    string Title,
    bool Success,
    string Error,
    IReadOnlyList<ExpectedSection> Sections);

record ExpectedSection(
    int SlideNumber,
    int SectionOrder,
    string SectionLabel,
    string Text);

record ImportedSong(
    string SourceFile,
    string Title,
    List<ImportedSection> Sections);

record ImportedSection(
    int SectionOrder,
    string SectionLabel,
    string Text);

record AccuracyIssue(
    string SourceFile,
    string SongTitle,
    int SlideNumber,
    string IssueType,
    string ExpectedText,
    string ActualText,
    string MissingLines,
    string ChangedLines,
    string Notes);

partial class Program
{
    [GeneratedRegex(@"(?<=\p{L})\u0000\s*(?=\p{Ll})")]
    private static partial Regex SoftHyphenJoinRegex();

    [GeneratedRegex(@"\b(?:\p{Lu}\s+){2,}\p{Lu}\b")]
    private static partial Regex SpacedLettersRegex();

    [GeneratedRegex(@"[\u0001-\u0008\u000B\u000C\u000E-\u001F]")]
    private static partial Regex ControlCharactersRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
