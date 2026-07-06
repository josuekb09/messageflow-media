using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MessageFlow.Core.Songs;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

const string SongRoot = @"D:\SONG PRESENTATION";
const string ChoirRoot = @"D:\SONG PRESENTATION\choir";
const string IgnoredFolderName = "chruch service";
const string OutputDirectory = @"D:\MessageFlow Archive\SongImportTest";
const string PreviewReportPath = OutputDirectory + @"\song_import_preview_report.txt";
const string ApplyReportPath = OutputDirectory + @"\song_import_apply_report.txt";
const string RequiredBackupPath = @"D:\MessageFlow Archive\Database Backups\messageflow_before_songs_feature.db";

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));
Directory.CreateDirectory(OutputDirectory);

var databasePath = MessageFlowDatabase.DefaultDatabasePath;
if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return 1;
}

var files = DiscoverPresentationFiles([SongRoot, ChoirRoot], IgnoredFolderName);
var candidates = files.Select(ExtractSong).ToList();
var reportPath = apply ? ApplyReportPath : PreviewReportPath;

if (!apply)
{
    await WriteReportAsync(reportPath, databasePath, candidates, apply, new ImportApplySummary());
    PrintSummary(candidates, reportPath, "Preview only; no database writes were made.");
    return candidates.Any(candidate => !candidate.Success) ? 2 : 0;
}

try
{
    EnsureRequiredBackup(databasePath, RequiredBackupPath);
}
catch (Exception ex)
{
    Console.WriteLine($"Backup verification failed: {ex.Message}");
    return 3;
}

await MessageFlowDatabaseRepair.RepairAsync(databasePath, Console.WriteLine);

var options = new DbContextOptionsBuilder<MessageFlowDbContext>()
    .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString())
    .Options;

await using var dbContext = new MessageFlowDbContext(options);
var applySummary = await ApplySongsAsync(dbContext, candidates);
await WriteReportAsync(reportPath, databasePath, candidates, apply, applySummary);

PrintSummary(candidates, reportPath, $"Imported: {applySummary.Inserted:N0}; updated: {applySummary.Updated:N0}; duplicate content detected: {applySummary.DuplicateContentDetected:N0}.");
return candidates.Any(candidate => !candidate.Success) ? 2 : 0;

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

static ImportedSongCandidate ExtractSong(string sourceFile)
{
    var warnings = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
    var extension = Path.GetExtension(sourceFile);
    string error;
    List<RawSlide> rawSlides;
    if (extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase))
    {
        rawSlides = TryExtractPptx(sourceFile, warnings, out error);
    }
    else
    {
        rawSlides = LegacyPptNotExtracted(warnings, out error);
    }

    var sections = new List<ImportedSongSection>();
    foreach (var slide in rawSlides)
    {
        var cleanedLines = slide.Lines
            .Select(CleanLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (cleanedLines.Count == 0)
        {
            continue;
        }

        var sectionText = string.Join(Environment.NewLine, cleanedLines);
        var sectionType = DetectSectionType(cleanedLines);
        var sectionLabel = CreateSectionLabel(sectionType, slide.SlideNumber);
        sections.Add(new ImportedSongSection(
            sections.Count + 1,
            sectionType,
            sectionLabel,
            sectionText,
            SongTextNormalizer.Normalize(sectionText)));
    }

    if (sections.Count == 0)
    {
        warnings.Add("No readable lyric text found.");
    }

    var title = DetectTitle(sourceFile, sections);
    var normalizedTitle = SongTextNormalizer.Normalize(title);
    var contentHash = ComputeContentHash(normalizedTitle, sections);

    return new ImportedSongCandidate(
        SourceFile: Path.GetFullPath(sourceFile),
        SourceFolder: GetSourceFolder(sourceFile),
        FileName: Path.GetFileName(sourceFile),
        Title: title,
        NormalizedTitle: normalizedTitle,
        ContentHash: contentHash,
        Success: string.IsNullOrWhiteSpace(error),
        Error: error,
        Warnings: warnings.ToList(),
        Sections: sections);
}

static List<RawSlide> TryExtractPptx(string sourceFile, ISet<string> warnings, out string error)
{
    error = string.Empty;

    try
    {
        using var archive = ZipFile.OpenRead(sourceFile);
        var slideEntries = ResolveSlideEntries(archive);
        var slides = new List<RawSlide>();

        for (var index = 0; index < slideEntries.Count; index++)
        {
            var entry = archive.GetEntry(slideEntries[index]);
            if (entry is null)
            {
                continue;
            }

            slides.Add(new RawSlide(index + 1, ExtractTextFromSlideXml(entry)));
        }

        return slides;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException)
    {
        error = $"PPTX extraction failed: {ex.Message}";
        warnings.Add(error);
        return [];
    }
}

static List<RawSlide> LegacyPptNotExtracted(ISet<string> warnings, out string error)
{
    error = "Legacy .ppt extraction is not enabled in this importer; convert to .pptx or use the inspection fallback first.";
    warnings.Add(error);
    return [];
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
            // Fall back to numeric slide entry order below.
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
        return CultureInfoTitle(firstLine.Split(' ', ':', '-').First());
    }

    return "Slide";
}

static string CreateSectionLabel(string sectionType, int slideNumber)
{
    return sectionType.Equals("Slide", StringComparison.OrdinalIgnoreCase)
        ? $"Slide {slideNumber}"
        : $"{sectionType} - Slide {slideNumber}";
}

static string DetectTitle(string sourceFile, IReadOnlyList<ImportedSongSection> sections)
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

static string GetSourceFolder(string sourceFile)
{
    var fullPath = Path.GetFullPath(sourceFile);
    var choirPath = Path.GetFullPath(ChoirRoot);
    return fullPath.StartsWith(choirPath, StringComparison.OrdinalIgnoreCase) ? "Choir" : "Songs";
}

static string ComputeContentHash(string normalizedTitle, IReadOnlyList<ImportedSongSection> sections)
{
    var builder = new StringBuilder();
    builder.AppendLine(normalizedTitle);
    foreach (var section in sections)
    {
        builder.AppendLine(section.SectionLabel);
        builder.AppendLine(section.NormalizedText);
    }

    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexString(bytes);
}

static async Task<ImportApplySummary> ApplySongsAsync(
    MessageFlowDbContext dbContext,
    IReadOnlyList<ImportedSongCandidate> candidates)
{
    var summary = new ImportApplySummary();
    var importedAtUtc = DateTime.UtcNow;
    var knownHashes = await dbContext.Songs
        .AsNoTracking()
        .Select(song => new { song.Id, song.ContentHash, song.SourceFilePath })
        .ToListAsync();

    foreach (var candidate in candidates.Where(candidate => candidate.Success && candidate.Sections.Count > 0))
    {
        var existingSong = await dbContext.Songs
            .Include(song => song.Sections)
            .FirstOrDefaultAsync(song => song.SourceFilePath == candidate.SourceFile);

        if (knownHashes.Any(song =>
                song.ContentHash == candidate.ContentHash &&
                !string.Equals(song.SourceFilePath, candidate.SourceFile, StringComparison.OrdinalIgnoreCase)))
        {
            summary.DuplicateContentDetected++;
        }

        if (existingSong is null)
        {
            existingSong = new Song
            {
                SourceFilePath = candidate.SourceFile
            };
            dbContext.Songs.Add(existingSong);
            summary.Inserted++;
        }
        else
        {
            dbContext.SongSections.RemoveRange(existingSong.Sections);
            summary.Updated++;
        }

        existingSong.Title = candidate.Title;
        existingSong.NormalizedTitle = candidate.NormalizedTitle;
        existingSong.SourceFolder = candidate.SourceFolder;
        existingSong.FileName = candidate.FileName;
        existingSong.ImportedAtUtc = importedAtUtc;
        existingSong.ContentHash = candidate.ContentHash;
        existingSong.WarningSummary = string.Join("; ", candidate.Warnings);
        existingSong.IsActive = true;

        foreach (var section in candidate.Sections)
        {
            existingSong.Sections.Add(new SongSection
            {
                SectionOrder = section.SectionOrder,
                SectionType = section.SectionType,
                SectionLabel = section.SectionLabel,
                Text = section.Text,
                NormalizedText = section.NormalizedText
            });
        }

        knownHashes.Add(new { existingSong.Id, ContentHash = candidate.ContentHash, SourceFilePath = candidate.SourceFile });
    }

    summary.FailedExtraction = candidates.Count(candidate => !candidate.Success);
    summary.ZeroText = candidates.Count(candidate => candidate.Sections.Count == 0);
    await dbContext.SaveChangesAsync();
    return summary;
}

static void EnsureRequiredBackup(string databasePath, string backupPath)
{
    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

    if (!File.Exists(backupPath))
    {
        File.Copy(databasePath, backupPath, overwrite: false);
    }

    var databaseInfo = new FileInfo(databasePath);
    var backupInfo = new FileInfo(backupPath);
    if (!backupInfo.Exists || backupInfo.Length <= 0)
    {
        throw new InvalidOperationException($"Backup file is missing or empty: {backupPath}");
    }

    if (backupInfo.Length != databaseInfo.Length)
    {
        throw new InvalidOperationException($"Backup size does not match current database. Backup: {backupInfo.Length:N0}; database: {databaseInfo.Length:N0}.");
    }
}

static async Task WriteReportAsync(
    string reportPath,
    string databasePath,
    IReadOnlyList<ImportedSongCandidate> candidates,
    bool apply,
    ImportApplySummary applySummary)
{
    var duplicateTitleCount = candidates
        .Where(candidate => candidate.Success && candidate.Sections.Count > 0)
        .GroupBy(candidate => candidate.NormalizedTitle)
        .Count(group => group.Count() > 1);
    var failed = candidates.Count(candidate => !candidate.Success);
    var zeroText = candidates.Count(candidate => candidate.Sections.Count == 0);
    var suspicious = candidates.Count(candidate => candidate.Warnings.Count > 0);

    var builder = new StringBuilder();
    builder.AppendLine("MessageFlow Song Presentation Import Report");
    builder.AppendLine($"Mode: {(apply ? "APPLY" : "PREVIEW")}");
    builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"Backup required: {RequiredBackupPath}");
    builder.AppendLine($"Source roots: {SongRoot}; {ChoirRoot}");
    builder.AppendLine($"Ignored folder: {IgnoredFolderName}");
    builder.AppendLine();
    builder.AppendLine($"PowerPoint files found: {candidates.Count:N0}");
    builder.AppendLine($"Extracted successfully: {candidates.Count - failed:N0}");
    builder.AppendLine($"Failed extraction: {failed:N0}");
    builder.AppendLine($"Files with no text: {zeroText:N0}");
    builder.AppendLine($"Files with warnings: {suspicious:N0}");
    builder.AppendLine($"Duplicate normalized titles detected: {duplicateTitleCount:N0}");
    builder.AppendLine();
    builder.AppendLine("Apply summary:");
    builder.AppendLine($"  Inserted: {applySummary.Inserted:N0}");
    builder.AppendLine($"  Updated: {applySummary.Updated:N0}");
    builder.AppendLine($"  Duplicate content detected: {applySummary.DuplicateContentDetected:N0}");
    builder.AppendLine($"  Failed extraction: {applySummary.FailedExtraction:N0}");
    builder.AppendLine($"  Zero text: {applySummary.ZeroText:N0}");
    builder.AppendLine();
    builder.AppendLine("Recommended import strategy:");
    builder.AppendLine("- Keep one song record per source presentation.");
    builder.AppendLine("- Preserve each slide as an ordered song section for projection navigation.");
    builder.AppendLine("- Use title and lyric LIKE search now; add FTS later only if song volume grows meaningfully.");
    builder.AppendLine("- Do not auto-delete songs when a source file disappears; mark inactive only after operator review.");
    builder.AppendLine();
    builder.AppendLine("116. WON’T IT BE WONDERFUL slide 2 preview:");
    var regressionSong = candidates.FirstOrDefault(candidate =>
        candidate.FileName.StartsWith("116.", StringComparison.OrdinalIgnoreCase) &&
        candidate.FileName.Contains("WON", StringComparison.OrdinalIgnoreCase));
    var regressionSlide = regressionSong?.Sections.FirstOrDefault(section =>
        section.SectionLabel.EndsWith("Slide 2", StringComparison.OrdinalIgnoreCase));
    builder.AppendLine(regressionSlide?.Text ?? "Not found.");
    builder.AppendLine();
    builder.AppendLine("Samples:");

    foreach (var candidate in candidates.Take(30))
    {
        builder.AppendLine($"- {candidate.Title} ({candidate.FileName})");
        builder.AppendLine($"  Sections: {candidate.Sections.Count:N0}; Warnings: {string.Join("; ", candidate.Warnings)}");
        foreach (var section in candidate.Sections.Take(2))
        {
            var preview = section.Text.Replace(Environment.NewLine, " / ", StringComparison.Ordinal);
            if (preview.Length > 160)
            {
                preview = $"{preview[..160]}...";
            }

            builder.AppendLine($"  {section.SectionLabel}: {preview}");
        }
    }

    await File.WriteAllTextAsync(reportPath, builder.ToString(), Encoding.UTF8);
}

static void PrintSummary(IReadOnlyList<ImportedSongCandidate> candidates, string reportPath, string message)
{
    var failed = candidates.Count(candidate => !candidate.Success);
    var zeroText = candidates.Count(candidate => candidate.Sections.Count == 0);

    Console.WriteLine($"PowerPoint files found: {candidates.Count:N0}");
    Console.WriteLine($"Extracted successfully: {candidates.Count - failed:N0}");
    Console.WriteLine($"Failed extraction: {failed:N0}");
    Console.WriteLine($"Files with no text: {zeroText:N0}");
    Console.WriteLine(message);
    Console.WriteLine($"Report: {reportPath}");
}

static string CultureInfoTitle(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}

record RawSlide(int SlideNumber, IReadOnlyList<string> Lines);

record ImportedSongSection(
    int SectionOrder,
    string SectionType,
    string SectionLabel,
    string Text,
    string NormalizedText);

record ImportedSongCandidate(
    string SourceFile,
    string SourceFolder,
    string FileName,
    string Title,
    string NormalizedTitle,
    string ContentHash,
    bool Success,
    string Error,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ImportedSongSection> Sections);

sealed class ImportApplySummary
{
    public int Inserted { get; set; }

    public int Updated { get; set; }

    public int DuplicateContentDetected { get; set; }

    public int FailedExtraction { get; set; }

    public int ZeroText { get; set; }
}

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
