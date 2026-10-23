using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MessageFlow.Core.Text;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;

// Restores the line breaks of song sections imported before the PPTX readers understood
// <a:br/> (Shift+Enter) breaks. Each stored section is matched to its source slide by the
// "Slide N" in its label, and it is rewritten only when the stored text and the source slide
// contain exactly the same non-whitespace characters, so no word is ever added, removed or
// changed: only the whitespace between the source lines becomes a line break again.
//
// Usage:
//   RepairSongLineBreaks --database <db> --english-source "E:\SONG PRESENTATION"
//                        --swahili-source <folder of the extracted Nyimbo-za-Kiswahili.zip> [--apply | --verify]
//
// --verify exits with 1 when any section's line structure differs from its source slide, or
// when a source cannot be read, so it doubles as the "no collapsed song lines" release check.

Console.OutputEncoding = Encoding.UTF8;

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));
var verify = args.Any(arg => string.Equals(arg, "--verify", StringComparison.OrdinalIgnoreCase));
var databasePath = GetOption(args, "--database") ?? MessageFlowDatabase.DefaultDatabasePath;
var englishSource = GetOption(args, "--english-source");
var swahiliSource = GetOption(args, "--swahili-source");

if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return 1;
}

const string EnglishImportRoot = @"D:\SONG PRESENTATION\";
const string SwahiliKeyPrefix = "swahili-pptx://";

var swahiliFiles = new Dictionary<string, string>(StringComparer.Ordinal);
if (!string.IsNullOrWhiteSpace(swahiliSource) && Directory.Exists(swahiliSource))
{
    foreach (var file in Directory.EnumerateFiles(swahiliSource, "*.pptx", SearchOption.AllDirectories))
    {
        // Same key the Swahili importer stored in Songs.SourceFilePath.
        var slug = WhitespaceRegex().Replace(Path.GetFileNameWithoutExtension(file).Trim(), " ");
        swahiliFiles.TryAdd(SwahiliKeyPrefix + slug, file);
    }
}

await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
}.ToString());
await connection.OpenAsync();

var sections = new List<SectionRow>();
await using (var command = connection.CreateCommand())
{
    command.CommandText =
        """
        SELECT ss.Id, s.Id, s.Language, s.Title, s.SourceFilePath, ss.SectionLabel, ss.Text
        FROM SongSections ss
        JOIN Songs s ON s.Id = ss.SongId
        WHERE s.SourceFilePath LIKE '%.pptx' OR s.SourceFilePath LIKE 'swahili-pptx://%'
        ORDER BY s.Id, ss.SectionOrder
        """;
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        sections.Add(new SectionRow(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6)));
    }
}

var slideCache = new Dictionary<string, List<List<string>>?>(StringComparer.OrdinalIgnoreCase);
var updates = new List<(SectionRow Row, string NewText)>();
var unchanged = 0;
var missingSource = 0;
var mismatches = new List<string>();

foreach (var row in sections)
{
    var file = ResolveSourceFile(row.SourcePath);
    if (file is null)
    {
        missingSource++;
        continue;
    }

    if (!slideCache.TryGetValue(file, out var slides))
    {
        slides = TryReadSlides(file);
        slideCache[file] = slides;
    }

    var slideNumber = SlideNumberFromLabel(row.Label);
    if (slides is null || slideNumber < 1 || slideNumber > slides.Count)
    {
        mismatches.Add($"#{row.SectionId} {row.Title} [{row.Label}]: source slide not found in {Path.GetFileName(file)}");
        continue;
    }

    var rebuilt = RestoreLineBreaks(row.Text, slides[slideNumber - 1]);
    if (rebuilt is null)
    {
        mismatches.Add($"#{row.SectionId} {row.Title} [{row.Label}]: stored characters differ from the source slide; left unchanged");
        continue;
    }

    if (string.Equals(rebuilt, row.Text, StringComparison.Ordinal))
    {
        unchanged++;
        continue;
    }

    updates.Add((row, rebuilt));
}

Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"PPTX song sections: {sections.Count}");
Console.WriteLine($"Already correct: {unchanged}");
Console.WriteLine($"Line breaks to restore: {updates.Count}");
foreach (var group in updates.GroupBy(update => update.Row.Language).OrderBy(group => group.Key))
{
    Console.WriteLine($"  {group.Key}: {group.Count()} sections in {group.Select(update => update.Row.SongId).Distinct().Count()} songs");
}

Console.WriteLine($"Source file not available: {missingSource}");
Console.WriteLine($"Skipped (not provably identical): {mismatches.Count}");
foreach (var mismatch in mismatches.Take(40))
{
    Console.WriteLine($"  {mismatch}");
}

foreach (var (row, newText) in updates.Take(3))
{
    Console.WriteLine();
    Console.WriteLine($"Example #{row.SectionId} {row.Title} [{row.Label}]");
    Console.WriteLine("  before: " + row.Text.Replace("\r\n", " | ", StringComparison.Ordinal));
    Console.WriteLine("  after : " + newText.Replace("\r\n", " | ", StringComparison.Ordinal));
}

if (verify)
{
    var passed = updates.Count == 0 && missingSource == 0 && mismatches.Count == 0;
    Console.WriteLine();
    Console.WriteLine(passed
        ? $"PASS: all {sections.Count} PPTX song sections keep the line breaks of their source slides."
        : "FAIL: some song sections do not match the line breaks of their source slides.");
    return passed ? 0 : 1;
}

if (!apply)
{
    Console.WriteLine();
    Console.WriteLine("Preview only. Pass --apply to write the restored line breaks.");
    return 0;
}

await using (var transaction = (SqliteTransaction)await connection.BeginTransactionAsync())
{
    foreach (var (row, newText) in updates)
    {
        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE SongSections SET Text = $text, NormalizedText = $normalized WHERE Id = $id";
        update.Parameters.AddWithValue("$text", newText);
        update.Parameters.AddWithValue("$normalized", SongTextNormalizer.Normalize(newText));
        update.Parameters.AddWithValue("$id", row.SectionId);
        await update.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
}

Console.WriteLine($"Applied {updates.Count} section updates.");
return 0;

string? ResolveSourceFile(string sourcePath)
{
    if (sourcePath.StartsWith(SwahiliKeyPrefix, StringComparison.Ordinal))
    {
        // A re-import of the same file was stored as "<key>/2"; it still comes from that file.
        var key = Regex.Replace(sourcePath, @"/\d+$", string.Empty);
        return swahiliFiles.TryGetValue(key, out var swahiliFile) ? swahiliFile : null;
    }

    if (File.Exists(sourcePath))
    {
        return sourcePath;
    }

    if (!string.IsNullOrWhiteSpace(englishSource) &&
        sourcePath.StartsWith(EnglishImportRoot, StringComparison.OrdinalIgnoreCase))
    {
        var candidate = Path.Combine(englishSource, sourcePath[EnglishImportRoot.Length..]);
        return File.Exists(candidate) ? candidate : null;
    }

    return null;
}

static List<List<string>>? TryReadSlides(string file)
{
    try
    {
        using var archive = ZipFile.OpenRead(file);
        var slides = new List<List<string>>();
        foreach (var entryName in ResolveSlideEntries(archive))
        {
            var entry = archive.GetEntry(entryName);
            if (entry is null)
            {
                // The importers skip a missing part without consuming a slide number.
                continue;
            }

            using var stream = entry.Open();
            slides.Add(PresentationParagraphText.GetSlideLines(XDocument.Load(stream)));
        }

        return slides;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException)
    {
        return null;
    }
}

// Splits the stored text where the source slide breaks its lines. Returns null unless the
// stored text and the slide hold the same characters once whitespace (and the invisible
// characters the importers strip) is ignored.
static string? RestoreLineBreaks(string storedText, IReadOnlyList<string> sourceLines)
{
    var lineLengths = sourceLines
        .Select(line => Comparable(line).Length)
        .Where(length => length > 0)
        .ToList();
    var sourceComparable = string.Concat(sourceLines.Select(Comparable));
    if (!string.Equals(sourceComparable, Comparable(storedText), StringComparison.Ordinal))
    {
        return null;
    }

    var lines = new List<string>(lineLengths.Count);
    var current = new StringBuilder();
    var lineIndex = 0;
    var consumed = 0;
    foreach (var character in storedText)
    {
        if (lineIndex < lineLengths.Count - 1 && consumed == lineLengths[lineIndex] && IsComparable(character))
        {
            lines.Add(current.ToString().Trim());
            current.Clear();
            lineIndex++;
            consumed = 0;
        }

        current.Append(character);
        if (IsComparable(character))
        {
            consumed++;
        }
    }

    lines.Add(current.ToString().Trim());
    return string.Join("\r\n", lines.Select(line => WhitespaceRegex().Replace(line, " ")));
}

static string Comparable(string value)
{
    var builder = new StringBuilder(value.Length);
    foreach (var character in value)
    {
        if (IsComparable(character))
        {
            builder.Append(character);
        }
    }

    return builder.ToString();
}

static bool IsComparable(char character) =>
    !char.IsWhiteSpace(character) &&
    !char.IsControl(character) &&
    character is not ('\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\u00AD');

static int SlideNumberFromLabel(string label)
{
    var match = Regex.Match(label, @"Slide\s+(\d+)\s*$", RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : -1;
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

            var targets = relationships.Root?
                .Elements(rel + "Relationship")
                .Where(element => (string?)element.Attribute("Target") is not null)
                .ToDictionary(
                    element => (string)element.Attribute("Id")!,
                    element => NormalizePartPath("ppt", (string)element.Attribute("Target")!),
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var ordered = presentation
                .Descendants(p + "sldId")
                .Select(element => (string?)element.Attribute(r + "id"))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => targets.TryGetValue(id!, out var target) ? target : string.Empty)
                .Where(target => target.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (ordered.Count > 0)
            {
                return ordered;
            }
        }
        catch (System.Xml.XmlException)
        {
            // Fall back to numeric slide order, as the importers do.
        }
    }

    return archive.Entries
        .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        .OrderBy(entry =>
        {
            var match = Regex.Match(entry.FullName, @"slide(\d+)\.xml$", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : int.MaxValue;
        })
        .Select(entry => entry.FullName)
        .ToList();
}

static string NormalizePartPath(string baseFolder, string target)
{
    var combined = target.StartsWith('/') ? target.TrimStart('/') : $"{baseFolder}/{target}";
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

static string? GetOption(string[] values, string name)
{
    var index = Array.FindIndex(values, value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
}

internal sealed record SectionRow(
    int SectionId,
    int SongId,
    string Language,
    string Title,
    string SourcePath,
    string Label,
    string Text);

internal static partial class Program
{
    [GeneratedRegex(@"[ \t\u00A0]+")]
    private static partial Regex WhitespaceRegex();
}
