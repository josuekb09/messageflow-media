using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string SongRoot = @"D:\SONG PRESENTATION";
const string ChoirRoot = @"D:\SONG PRESENTATION\choir";
const string IgnoredFolderName = "chruch service";
const string OutputDirectory = @"D:\MessageFlow Archive\SongImportTest";
const string TextReportPath = OutputDirectory + @"\song_presentation_inspection_report.txt";
const string CsvOutputPath = OutputDirectory + @"\song_extracted_samples.csv";
const string JsonOutputPath = OutputDirectory + @"\song_extracted_samples.json";

var scanRoots = new[] { SongRoot, ChoirRoot };
Directory.CreateDirectory(OutputDirectory);

var discovered = DiscoverPresentationFiles(scanRoots, IgnoredFolderName);
var songs = new List<SongInspection>();
var temporaryFilesSkipped = discovered.TemporaryFilesSkipped;

foreach (var file in discovered.Files)
{
    Console.WriteLine($"Inspecting {file}");
    songs.Add(InspectPresentation(file));
}

var summary = InspectionSummary.From(songs, discovered.Files.Count, temporaryFilesSkipped);

await WriteCsvAsync(CsvOutputPath, songs);
await WriteJsonAsync(JsonOutputPath, scanRoots, summary, songs);
await WriteReportAsync(TextReportPath, scanRoots, summary, songs);

Console.WriteLine();
Console.WriteLine($"PowerPoint files found: {summary.TotalPowerPointFiles:N0}");
Console.WriteLine($"Files successfully read: {summary.FilesSuccessfullyRead:N0}");
Console.WriteLine($"Files failed: {summary.FilesFailed:N0}");
Console.WriteLine($"Files with no text: {summary.FilesWithNoText:N0}");
Console.WriteLine($"Files with suspicious characters: {summary.FilesWithSuspiciousCharacters:N0}");
Console.WriteLine($"Files with very broken text: {summary.FilesWithVeryBrokenText:N0}");
Console.WriteLine($"Report: {TextReportPath}");
Console.WriteLine($"CSV: {CsvOutputPath}");
Console.WriteLine($"JSON: {JsonOutputPath}");

return summary.FilesFailed == 0 ? 0 : 2;

static DiscoveryResult DiscoverPresentationFiles(IEnumerable<string> roots, string ignoredFolderName)
{
    var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
    var temporaryFilesSkipped = 0;

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
                temporaryFilesSkipped++;
                continue;
            }

            files.Add(Path.GetFullPath(file));
        }
    }

    return new DiscoveryResult(files.ToList(), temporaryFilesSkipped);
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

static SongInspection InspectPresentation(string sourceFile)
{
    var extension = Path.GetExtension(sourceFile);
    ExtractionResult extractionResult;

    if (extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase))
    {
        extractionResult = TryExtractPptx(sourceFile);
        if (!extractionResult.Success && OperatingSystem.IsWindows())
        {
            var fallback = TryExtractWithPowerPointCom(sourceFile);
            if (fallback.Success)
            {
                fallback.Warnings.Add("PPTX ZIP/XML extraction failed; read-only PowerPoint COM fallback succeeded.");
                extractionResult = fallback;
            }
        }
    }
    else if (extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase))
    {
        extractionResult = OperatingSystem.IsWindows()
            ? TryExtractWithPowerPointCom(sourceFile)
            : ExtractionResult.Failure("Legacy .ppt extraction requires PowerPoint COM on Windows.");
    }
    else
    {
        extractionResult = ExtractionResult.Failure($"Unsupported extension: {extension}");
    }

    var slides = BuildCleanSlides(extractionResult.Slides);
    var title = DetectTitle(sourceFile, slides);
    var warnings = new SortedSet<string>(extractionResult.Warnings, StringComparer.OrdinalIgnoreCase);

    if (slides.Count == 0 || slides.All(slide => slide.Lines.Count == 0))
    {
        warnings.Add("No readable slide text found.");
    }

    if (slides.Any(slide => slide.Lines.Any(line => line.Warnings.Any(IsSuspiciousCharacterWarning))))
    {
        warnings.Add("One or more lines contain suspicious characters.");
    }

    if (slides.Any(slide => slide.Lines.Any(line => line.Warnings.Any(IsBrokenTextWarning))))
    {
        warnings.Add("One or more lines look like very broken text.");
    }

    return new SongInspection(
        SourceFile: sourceFile,
        FileName: Path.GetFileName(sourceFile),
        Extension: extension,
        DetectedTitle: title,
        Status: extractionResult.Success ? "Read" : "Failed",
        Extractor: extractionResult.Extractor,
        Error: extractionResult.Error,
        Warnings: warnings.ToList(),
        Slides: slides,
        PossibleVerses: slides
            .SelectMany(slide => slide.Lines)
            .Where(line => line.SectionType.StartsWith("Verse", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.SectionType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList(),
        HasPossibleChorus: slides
            .SelectMany(slide => slide.Lines)
            .Any(line => line.SectionType.Equals("Chorus", StringComparison.OrdinalIgnoreCase) ||
                         line.SectionType.Equals("Refrain", StringComparison.OrdinalIgnoreCase)));
}

static ExtractionResult TryExtractPptx(string sourceFile)
{
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

        return ExtractionResult.Ok("PPTX ZIP/XML", slides);
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException)
    {
        return ExtractionResult.Failure($"PPTX extraction failed: {ex.Message}", "PPTX ZIP/XML");
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
        : $"{baseFolder.TrimEnd('/')}/{target}";

    var parts = new Stack<string>();
    foreach (var part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
    {
        if (part.Equals(".", StringComparison.Ordinal))
        {
            continue;
        }

        if (part.Equals("..", StringComparison.Ordinal))
        {
            if (parts.Count > 0)
            {
                parts.Pop();
            }

            continue;
        }

        parts.Push(part);
    }

    return string.Join("/", parts.Reverse());
}

static int ExtractSlideNumber(string entryName)
{
    var match = Regex.Match(entryName, @"slide(?<number>\d+)\.xml$", RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups["number"].Value, out var number)
        ? number
        : int.MaxValue;
}

static IReadOnlyList<string> ExtractTextFromSlideXml(ZipArchiveEntry entry)
{
    using var stream = entry.Open();
    var document = XDocument.Load(stream);
    XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
    var lines = new List<string>();

    foreach (var paragraph in document.Descendants(a + "p"))
    {
        var text = string.Concat(paragraph.Descendants(a + "t").Select(value => value.Value));
        if (!string.IsNullOrWhiteSpace(text))
        {
            lines.Add(text);
        }
    }

    return lines;
}

static ExtractionResult TryExtractWithPowerPointCom(string sourceFile)
{
    if (!OperatingSystem.IsWindows())
    {
        return ExtractionResult.Failure("PowerPoint COM extraction is available only on Windows.", "PowerPoint COM");
    }

    Type? appType;
    try
    {
        appType = Type.GetTypeFromProgID("PowerPoint.Application");
    }
    catch (Exception ex)
    {
        return ExtractionResult.Failure($"PowerPoint COM lookup failed: {ex.Message}", "PowerPoint COM");
    }

    if (appType is null)
    {
        return ExtractionResult.Failure("PowerPoint COM is not available on this computer.", "PowerPoint COM");
    }

    object? app = null;
    object? presentation = null;

    try
    {
        dynamic powerPoint = Activator.CreateInstance(appType)!;
        app = powerPoint;
        powerPoint.DisplayAlerts = 0;

        dynamic presentations = powerPoint.Presentations;
        presentation = presentations.Open(sourceFile, -1, 0, 0);
        dynamic deck = presentation;
        var slides = new List<RawSlide>();

        foreach (dynamic slide in deck.Slides)
        {
            var slideNumber = SafeInt(slide.SlideIndex);
            var lines = new List<string>();
            foreach (dynamic shape in slide.Shapes)
            {
                ExtractShapeTextWithCom(shape, lines);
            }

            slides.Add(new RawSlide(slideNumber == 0 ? slides.Count + 1 : slideNumber, lines));
        }

        deck.Close();
        presentation = null;
        powerPoint.Quit();
        app = null;

        return ExtractionResult.Ok("PowerPoint COM", slides);
    }
    catch (Exception ex)
    {
        return ExtractionResult.Failure($"PowerPoint COM extraction failed: {ex.Message}", "PowerPoint COM");
    }
    finally
    {
        TryCloseComPresentation(presentation);
        TryQuitComApplication(app);
    }
}

static void ExtractShapeTextWithCom(dynamic shape, List<string> lines)
{
    try
    {
        if (SafeInt(shape.HasTextFrame) != 0 && SafeInt(shape.TextFrame.HasText) != 0)
        {
            var text = Convert.ToString(shape.TextFrame.TextRange.Text, CultureInfo.InvariantCulture);
            AddComTextLines(text, lines);
        }
    }
    catch
    {
        // Some PowerPoint shapes throw when a text frame property is queried.
    }

    try
    {
        var groupItems = shape.GroupItems;
        foreach (dynamic child in groupItems)
        {
            ExtractShapeTextWithCom(child, lines);
        }
    }
    catch
    {
        // Shape is not a group, or the group item is inaccessible.
    }
}

static void AddComTextLines(string? text, List<string> lines)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return;
    }

    foreach (var line in text.Split(new[] { "\r\n", "\n", "\r", "\v" }, StringSplitOptions.None))
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            lines.Add(line);
        }
    }
}

static int SafeInt(dynamic value)
{
    try
    {
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }
    catch
    {
        return 0;
    }
}

static void TryCloseComPresentation(object? presentation)
{
    if (presentation is null)
    {
        return;
    }

    try
    {
        dynamic deck = presentation;
        deck.Close();
    }
    catch
    {
        // Best-effort cleanup.
    }
    finally
    {
        ReleaseComObject(presentation);
    }
}

static void TryQuitComApplication(object? app)
{
    if (app is null)
    {
        return;
    }

    try
    {
        dynamic powerPoint = app;
        powerPoint.Quit();
    }
    catch
    {
        // Best-effort cleanup.
    }
    finally
    {
        ReleaseComObject(app);
    }
}

static void ReleaseComObject(object value)
{
    try
    {
        if (Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
    catch
    {
        // Best-effort cleanup.
    }
}

static List<SlideInspection> BuildCleanSlides(IReadOnlyList<RawSlide> rawSlides)
{
    var slides = new List<SlideInspection>();
    var currentSection = "Lyric";

    foreach (var rawSlide in rawSlides.OrderBy(slide => slide.SlideNumber))
    {
        var lines = new List<LyricLine>();
        var lineNumber = 0;

        foreach (var rawLine in rawSlide.Lines)
        {
            var clean = CleanLine(rawLine);
            if (string.IsNullOrWhiteSpace(clean.Text))
            {
                continue;
            }

            var section = DetectSection(clean.Text, currentSection);
            if (!string.IsNullOrWhiteSpace(section.NewCurrentSection))
            {
                currentSection = section.NewCurrentSection;
            }

            var lyricText = section.LyricText;
            if (string.IsNullOrWhiteSpace(lyricText) && section.IsSectionLabelOnly)
            {
                lineNumber++;
                lines.Add(new LyricLine(
                    LineNumber: lineNumber,
                    SectionType: currentSection,
                    LyricText: string.Empty,
                    IsSectionLabel: true,
                    Warnings: clean.Warnings));
                continue;
            }

            if (string.IsNullOrWhiteSpace(lyricText))
            {
                continue;
            }

            lineNumber++;
            lines.Add(new LyricLine(
                LineNumber: lineNumber,
                SectionType: currentSection,
                LyricText: lyricText,
                IsSectionLabel: section.IsSectionLabelOnly,
                Warnings: clean.Warnings));
        }

        slides.Add(new SlideInspection(rawSlide.SlideNumber, lines));
    }

    return slides;
}

static CleanResult CleanLine(string rawLine)
{
    var warnings = new List<string>();
    var line = rawLine
        .Replace('\u00A0', ' ')
        .Replace('\u2007', ' ')
        .Replace('\u202F', ' ');

    if (line.Contains('\uFFFD', StringComparison.Ordinal))
    {
        warnings.Add("Suspicious replacement character found.");
    }

    line = RemoveControlAndFormattingCharacters(line, warnings);
    line = Regex.Replace(line, @"^[\s\u2022\u25CF\u25AA\u25E6\u00B7\-\u2013\u2014]+", string.Empty);
    line = RepairConfidentLetterSpacing(line, warnings);
    line = Regex.Replace(line, @"\s+", " ").Trim();

    if (LooksVeryBroken(line))
    {
        warnings.Add("Very broken text pattern.");
    }

    return new CleanResult(line, warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
}

static string RemoveControlAndFormattingCharacters(string text, List<string> warnings)
{
    var builder = new StringBuilder(text.Length);

    foreach (var rune in text.EnumerateRunes())
    {
        var category = Rune.GetUnicodeCategory(rune);
        if (category is UnicodeCategory.Format or UnicodeCategory.Surrogate)
        {
            warnings.Add("Suspicious formatting character removed.");
            continue;
        }

        if (category is UnicodeCategory.Control)
        {
            if (rune.Value is not '\r' and not '\n' and not '\t')
            {
                warnings.Add("Suspicious control character removed.");
            }

            builder.Append(' ');
            continue;
        }

        if (category is UnicodeCategory.OtherSymbol or UnicodeCategory.MathSymbol or UnicodeCategory.CurrencySymbol)
        {
            warnings.Add("Suspicious symbol removed.");
            builder.Append(' ');
            continue;
        }

        builder.Append(rune.ToString());
    }

    return builder.ToString();
}

static string RepairConfidentLetterSpacing(string line, List<string> warnings)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        return string.Empty;
    }

    var hadWideWordGaps = Regex.IsMatch(line, @"\s{2,}");
    if (!hadWideWordGaps)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is >= 3 and <= 10 && tokens.All(IsSingleLetterToken))
        {
            warnings.Add("Broken letter spacing repaired.");
            return string.Concat(tokens);
        }

        return line;
    }

    var parts = Regex.Split(line, @"(\s{2,})");
    var repairedAny = false;
    for (var index = 0; index < parts.Length; index++)
    {
        if (string.IsNullOrWhiteSpace(parts[index]))
        {
            parts[index] = " ";
            continue;
        }

        var tokens = parts[index].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2 && tokens.All(IsSingleLetterToken))
        {
            parts[index] = string.Concat(tokens);
            repairedAny = true;
        }
    }

    if (repairedAny)
    {
        warnings.Add("Broken letter spacing repaired.");
    }

    return string.Concat(parts);
}

static bool IsSingleLetterToken(string token)
{
    var runes = token.EnumerateRunes().ToArray();
    return runes.Length == 1 && Rune.GetUnicodeCategory(runes[0]) is
        UnicodeCategory.UppercaseLetter or
        UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or
        UnicodeCategory.ModifierLetter or
        UnicodeCategory.OtherLetter;
}

static bool LooksVeryBroken(string text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return false;
    }

    var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length >= 8 && tokens.Count(IsSingleLetterToken) >= tokens.Length * 0.7)
    {
        return true;
    }

    var letters = text.EnumerateRunes().Count(rune => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.UppercaseLetter or
        UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or
        UnicodeCategory.ModifierLetter or
        UnicodeCategory.OtherLetter);
    var punctuationAndSymbols = text.EnumerateRunes().Count(rune => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.OtherPunctuation or
        UnicodeCategory.MathSymbol or
        UnicodeCategory.OtherSymbol);

    return letters > 0 && punctuationAndSymbols > letters;
}

static SectionDetection DetectSection(string text, string currentSection)
{
    var sectionMatch = Regex.Match(
        text,
        @"^\s*(?<label>verse|v|stanza|chorus|refrain|bridge|tag|ending|coda)\s*(?<number>[0-9ivxIVX]+|one|two|three|four|five|six)?\s*[:\).\-\u2013\u2014]?\s*(?<rest>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    if (!sectionMatch.Success)
    {
        return new SectionDetection(currentSection, text, false);
    }

    var label = sectionMatch.Groups["label"].Value;
    var number = sectionMatch.Groups["number"].Value;
    var rest = sectionMatch.Groups["rest"].Value.Trim();
    var section = NormalizeSection(label, number);

    return new SectionDetection(
        NewCurrentSection: section,
        LyricText: rest,
        IsSectionLabelOnly: string.IsNullOrWhiteSpace(rest));
}

static string NormalizeSection(string label, string number)
{
    if (label.Equals("chorus", StringComparison.OrdinalIgnoreCase))
    {
        return "Chorus";
    }

    if (label.Equals("refrain", StringComparison.OrdinalIgnoreCase))
    {
        return "Refrain";
    }

    if (label.Equals("bridge", StringComparison.OrdinalIgnoreCase))
    {
        return "Bridge";
    }

    if (label.Equals("tag", StringComparison.OrdinalIgnoreCase))
    {
        return "Tag";
    }

    if (label.Equals("ending", StringComparison.OrdinalIgnoreCase) ||
        label.Equals("coda", StringComparison.OrdinalIgnoreCase))
    {
        return "Ending";
    }

    var normalizedNumber = NormalizeSectionNumber(number);
    return string.IsNullOrWhiteSpace(normalizedNumber) ? "Verse" : $"Verse {normalizedNumber}";
}

static string NormalizeSectionNumber(string number)
{
    if (string.IsNullOrWhiteSpace(number))
    {
        return string.Empty;
    }

    return number.ToLowerInvariant() switch
    {
        "one" => "1",
        "two" => "2",
        "three" => "3",
        "four" => "4",
        "five" => "5",
        "six" => "6",
        _ => number.ToUpperInvariant()
    };
}

static string DetectTitle(string sourceFile, IReadOnlyList<SlideInspection> slides)
{
    var fileTitle = CleanTitleFromFileName(sourceFile);
    var firstSlideLines = slides.FirstOrDefault()?.Lines
        .Where(line => !line.IsSectionLabel && !string.IsNullOrWhiteSpace(line.LyricText))
        .Select(line => line.LyricText)
        .ToList() ?? new List<string>();

    if (firstSlideLines.Count is > 0 and <= 2)
    {
        var firstLine = firstSlideLines[0];
        if (firstLine.Length <= 80 && !firstLine.EndsWith(",", StringComparison.Ordinal))
        {
            return firstLine;
        }
    }

    return fileTitle;
}

static string CleanTitleFromFileName(string sourceFile)
{
    var title = Path.GetFileNameWithoutExtension(sourceFile);
    title = title.StartsWith("~$", StringComparison.Ordinal) ? title[2..] : title;
    title = Regex.Replace(title, @"^\s*\d+\s*[A-Za-z]?\s*[\.\-\)]?\s*", string.Empty);
    title = Regex.Replace(title, @"\b(yellow\s+song\s+book|yellow\s+s\s+book|yellow\s+song)\b", string.Empty, RegexOptions.IgnoreCase);
    title = title.Replace('_', ' ');
    title = Regex.Replace(title, @"\s+", " ").Trim(' ', '.', '-', '_');

    return string.IsNullOrWhiteSpace(title)
        ? Path.GetFileNameWithoutExtension(sourceFile)
        : title;
}

static bool IsSuspiciousCharacterWarning(string warning) =>
    warning.Contains("Suspicious", StringComparison.OrdinalIgnoreCase);

static bool IsBrokenTextWarning(string warning) =>
    warning.Contains("broken", StringComparison.OrdinalIgnoreCase);

static async Task WriteCsvAsync(string path, IReadOnlyList<SongInspection> songs)
{
    await using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    await writer.WriteLineAsync("sourceFile,detectedTitle,slideNumber,sectionType,lineNumber,lyricText,warnings");

    foreach (var song in songs)
    {
        foreach (var slide in song.Slides)
        {
            foreach (var line in slide.Lines.Where(line => !string.IsNullOrWhiteSpace(line.LyricText)))
            {
                await writer.WriteLineAsync(string.Join(
                    ",",
                    Csv(song.SourceFile),
                    Csv(song.DetectedTitle),
                    slide.SlideNumber.ToString(CultureInfo.InvariantCulture),
                    Csv(line.SectionType),
                    line.LineNumber.ToString(CultureInfo.InvariantCulture),
                    Csv(line.LyricText),
                    Csv(string.Join("; ", line.Warnings))));
            }
        }
    }
}

static string Csv(string value)
{
    var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
    return $"\"{escaped}\"";
}

static async Task WriteJsonAsync(
    string path,
    IReadOnlyList<string> sourceRoots,
    InspectionSummary summary,
    IReadOnlyList<SongInspection> songs)
{
    var payload = new JsonInspectionOutput(
        GeneratedAt: DateTimeOffset.Now,
        SourceRoots: sourceRoots,
        IgnoredFolders: new[] { IgnoredFolderName },
        OutputDirectory: OutputDirectory,
        Summary: summary,
        Songs: songs);

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, options), new UTF8Encoding(false));
}

static async Task WriteReportAsync(
    string path,
    IReadOnlyList<string> sourceRoots,
    InspectionSummary summary,
    IReadOnlyList<SongInspection> songs)
{
    var builder = new StringBuilder();
    builder.AppendLine("MessageFlow Song Presentation Inspection");
    builder.AppendLine("========================================");
    builder.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
    builder.AppendLine();
    builder.AppendLine("Scope");
    builder.AppendLine("-----");
    foreach (var root in sourceRoots)
    {
        builder.AppendLine($"- {root}");
    }
    builder.AppendLine($"- Ignored folder name: {IgnoredFolderName}");
    builder.AppendLine("- Read-only prototype: no database writes, no PowerPoint file writes, no file moves.");
    builder.AppendLine();
    builder.AppendLine("Summary");
    builder.AppendLine("-------");
    builder.AppendLine($"Total PowerPoint files found: {summary.TotalPowerPointFiles:N0}");
    builder.AppendLine($"Office temporary PowerPoint files skipped: {summary.TemporaryFilesSkipped:N0}");
    builder.AppendLine($"PPTX files found: {summary.PptxFilesFound:N0}");
    builder.AppendLine($"PPT files found: {summary.PptFilesFound:N0}");
    builder.AppendLine($"Files successfully read: {summary.FilesSuccessfullyRead:N0}");
    builder.AppendLine($"Files failed: {summary.FilesFailed:N0}");
    builder.AppendLine($"Files with no text: {summary.FilesWithNoText:N0}");
    builder.AppendLine($"Files with suspicious characters: {summary.FilesWithSuspiciousCharacters:N0}");
    builder.AppendLine($"Files with very broken text: {summary.FilesWithVeryBrokenText:N0}");
    builder.AppendLine($"Slides read: {summary.SlidesRead:N0}");
    builder.AppendLine($"Lyric/sample lines written: {summary.LyricLinesWritten:N0}");
    builder.AppendLine();
    builder.AppendLine("Output Files");
    builder.AppendLine("------------");
    builder.AppendLine($"Report: {TextReportPath}");
    builder.AppendLine($"CSV: {CsvOutputPath}");
    builder.AppendLine($"JSON: {JsonOutputPath}");
    builder.AppendLine();
    builder.AppendLine("Extraction Strategy");
    builder.AppendLine("-------------------");
    builder.AppendLine("- .pptx files are read directly as ZIP/Open XML packages. The tool reads slide XML only.");
    builder.AppendLine("- Legacy .ppt files use PowerPoint COM in read-only mode when PowerPoint is installed.");
    builder.AppendLine("- Office lock files beginning with ~$ are skipped as temporary artifacts.");
    builder.AppendLine("- The misspelled folder \"chruch service\" is ignored for this prototype.");
    builder.AppendLine();
    builder.AppendLine("Recommended Import Strategy");
    builder.AppendLine("---------------------------");
    builder.AppendLine("- Import only after operator review of this report and sample files.");
    builder.AppendLine("- Store source file path, detected title, slide order, section type, and lyric lines separately.");
    builder.AppendLine("- Prefer .pptx direct extraction for normal imports; use COM only as a manual fallback for legacy .ppt files.");
    builder.AppendLine("- Keep the first production Songs feature read-only until search and projection behavior are approved.");
    builder.AppendLine("- Preserve original wording; only normalize whitespace and obvious presentation artifacts.");
    builder.AppendLine();

    if (songs.Any(song => song.Status == "Failed"))
    {
        builder.AppendLine("Failed Files");
        builder.AppendLine("------------");
        foreach (var song in songs.Where(song => song.Status == "Failed").Take(50))
        {
            builder.AppendLine($"- {song.SourceFile}");
            builder.AppendLine($"  {song.Error}");
        }
        if (songs.Count(song => song.Status == "Failed") > 50)
        {
            builder.AppendLine("- Additional failed files omitted from report; see JSON for full details.");
        }
        builder.AppendLine();
    }

    var warningFiles = songs
        .Where(song => song.Warnings.Count > 0)
        .Take(80)
        .ToList();
    if (warningFiles.Count > 0)
    {
        builder.AppendLine("Files With Warnings");
        builder.AppendLine("-------------------");
        foreach (var song in warningFiles)
        {
            builder.AppendLine($"- {song.SourceFile}");
            foreach (var warning in song.Warnings)
            {
                builder.AppendLine($"  - {warning}");
            }
        }
        if (songs.Count(song => song.Warnings.Count > 0) > warningFiles.Count)
        {
            builder.AppendLine("- Additional warning files omitted from report; see JSON for full details.");
        }
        builder.AppendLine();
    }

    builder.AppendLine("Sample Extracted Lyrics");
    builder.AppendLine("-----------------------");
    foreach (var song in songs.Where(song => song.Status == "Read").Take(12))
    {
        builder.AppendLine($"[{song.DetectedTitle}] {song.SourceFile}");
        foreach (var slide in song.Slides.Take(2))
        {
            var lines = slide.Lines
                .Where(line => !string.IsNullOrWhiteSpace(line.LyricText))
                .Take(4)
                .Select(line => $"{line.SectionType}: {line.LyricText}");
            foreach (var line in lines)
            {
                builder.AppendLine($"  Slide {slide.SlideNumber}: {line}");
            }
        }
    }

    await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(false));
}

internal sealed record DiscoveryResult(IReadOnlyList<string> Files, int TemporaryFilesSkipped);

internal sealed record RawSlide(int SlideNumber, IReadOnlyList<string> Lines);

internal sealed record CleanResult(string Text, IReadOnlyList<string> Warnings);

internal sealed record SectionDetection(string NewCurrentSection, string LyricText, bool IsSectionLabelOnly);

internal sealed record ExtractionResult(
    bool Success,
    string Extractor,
    string Error,
    List<string> Warnings,
    IReadOnlyList<RawSlide> Slides)
{
    public static ExtractionResult Ok(string extractor, IReadOnlyList<RawSlide> slides) =>
        new(true, extractor, string.Empty, new List<string>(), slides);

    public static ExtractionResult Failure(string error, string extractor = "Unknown") =>
        new(false, extractor, error, new List<string> { error }, Array.Empty<RawSlide>());
}

internal sealed record SongInspection(
    string SourceFile,
    string FileName,
    string Extension,
    string DetectedTitle,
    string Status,
    string Extractor,
    string Error,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SlideInspection> Slides,
    IReadOnlyList<string> PossibleVerses,
    bool HasPossibleChorus);

internal sealed record SlideInspection(int SlideNumber, IReadOnlyList<LyricLine> Lines);

internal sealed record LyricLine(
    int LineNumber,
    string SectionType,
    string LyricText,
    bool IsSectionLabel,
    IReadOnlyList<string> Warnings);

internal sealed record JsonInspectionOutput(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<string> SourceRoots,
    IReadOnlyList<string> IgnoredFolders,
    string OutputDirectory,
    InspectionSummary Summary,
    IReadOnlyList<SongInspection> Songs);

internal sealed record InspectionSummary(
    int TotalPowerPointFiles,
    int TemporaryFilesSkipped,
    int PptxFilesFound,
    int PptFilesFound,
    int FilesSuccessfullyRead,
    int FilesFailed,
    int FilesWithNoText,
    int FilesWithSuspiciousCharacters,
    int FilesWithVeryBrokenText,
    int SlidesRead,
    int LyricLinesWritten)
{
    public static InspectionSummary From(
        IReadOnlyList<SongInspection> songs,
        int totalPowerPointFiles,
        int temporaryFilesSkipped)
    {
        return new InspectionSummary(
            TotalPowerPointFiles: totalPowerPointFiles,
            TemporaryFilesSkipped: temporaryFilesSkipped,
            PptxFilesFound: songs.Count(song => song.Extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)),
            PptFilesFound: songs.Count(song => song.Extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase)),
            FilesSuccessfullyRead: songs.Count(song => song.Status == "Read"),
            FilesFailed: songs.Count(song => song.Status == "Failed"),
            FilesWithNoText: songs.Count(song => song.Slides.Count == 0 || song.Slides.All(slide => slide.Lines.Count == 0)),
            FilesWithSuspiciousCharacters: songs.Count(song => song.Warnings.Any(SummarySuspiciousCharacterWarning)),
            FilesWithVeryBrokenText: songs.Count(song => song.Warnings.Any(SummaryBrokenTextWarning)),
            SlidesRead: songs.Sum(song => song.Slides.Count),
            LyricLinesWritten: songs.Sum(song => song.Slides.Sum(slide => slide.Lines.Count(line => !string.IsNullOrWhiteSpace(line.LyricText)))));
    }

    private static bool SummarySuspiciousCharacterWarning(string warning) =>
        warning.Contains("Suspicious", StringComparison.OrdinalIgnoreCase);

    private static bool SummaryBrokenTextWarning(string warning) =>
        warning.Contains("broken", StringComparison.OrdinalIgnoreCase);
}
