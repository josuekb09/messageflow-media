using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MessageFlow.Search;

namespace ImportSwahiliPptxSongs;

internal static partial class SwahiliPptxParser
{
    public const string LanguageCode = "sw";
    public const string SourceKeyPrefix = "swahili-pptx://";
    public const string SourceFolder = "Nyimbo za Kiswahili";

    public static IReadOnlyList<ParsedSwahiliSong> ParseDirectory(string directory)
    {
        var files = Directory.EnumerateFiles(directory, "*.pptx", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var songs = new List<ParsedSwahiliSong>();
        foreach (var file in files)
        {
            songs.Add(ParseFile(file));
        }

        return songs;
    }

    public static ParsedSwahiliSong ParseFile(string path)
    {
        var fileName = Path.GetFileName(path);
        var title = TitleFromFileName(fileName);
        var info = new FileInfo(path);
        if (info.Length == 0)
        {
            return new ParsedSwahiliSong(fileName, title, [], "Empty file; skipped.");
        }

        List<List<string>> slides;
        try
        {
            slides = ExtractSlides(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return new ParsedSwahiliSong(fileName, title, [], $"PPTX extraction failed: {ex.Message}");
        }

        var rawSections = new List<ParsedSection>();
        for (var i = 0; i < slides.Count; i++)
        {
            var lines = slides[i]
                .Select(CleanLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            var type = DetectSectionType(lines);
            var text = string.Join(Environment.NewLine, lines);
            rawSections.Add(new ParsedSection(type, CreateLabel(type, i + 1), text));
        }

        if (rawSections.Count == 0)
        {
            return new ParsedSwahiliSong(fileName, title, [], "No readable lyric text found.");
        }

        var sections = InterleaveChorus(rawSections);
        return new ParsedSwahiliSong(fileName, title, sections, string.Empty);
    }

    private static List<List<string>> ExtractSlides(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var slides = new List<List<string>>();
        foreach (var entryName in ResolveSlideEntries(archive))
        {
            var entry = archive.GetEntry(entryName);
            if (entry is null)
            {
                continue;
            }

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
            var lines = document
                .Descendants(a + "p")
                .Select(JoinRunText)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            slides.Add(lines);
        }

        return slides;
    }

    private static List<string> ResolveSlideEntries(ZipArchive archive)
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

                var relationshipTargets = relationships.Root?
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
                    .Select(id => relationshipTargets.TryGetValue(id!, out var target) ? target : string.Empty)
                    .Where(target => target.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (ordered.Count > 0)
                {
                    return ordered;
                }
            }
            catch (System.Xml.XmlException)
            {
                // Fall back to numeric slide order.
            }
        }

        return archive.Entries
            .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                            entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => ExtractSlideNumber(entry.FullName))
            .Select(entry => entry.FullName)
            .ToList();
    }

    private static string NormalizePartPath(string baseFolder, string target)
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

    private static int ExtractSlideNumber(string entryName)
    {
        var match = SlideNumberRegex().Match(entryName);
        return match.Success && int.TryParse(match.Groups["number"].Value, out var number) ? number : int.MaxValue;
    }

    private static string JoinRunText(XElement paragraph)
    {
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        var builder = new StringBuilder();
        foreach (var run in paragraph.Descendants(a + "t"))
        {
            var part = run.Value ?? string.Empty;
            if (part.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0 &&
                !char.IsWhiteSpace(builder[^1]) &&
                !char.IsWhiteSpace(part[0]) &&
                !char.IsPunctuation(part[0]))
            {
                builder.Append(' ');
            }

            builder.Append(part);
        }

        return builder.ToString().Trim();
    }

    private static string CleanLine(string value)
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
        cleaned = ControlCharactersRegex().Replace(cleaned, string.Empty);
        cleaned = WhitespaceRegex().Replace(cleaned, " ");
        return cleaned.Trim();
    }

    private static string DetectSectionType(IReadOnlyList<string> lines)
    {
        var first = lines[0];
        if (ChorusMarkerRegex().IsMatch(first))
        {
            return "Chorus";
        }

        if (VerseMarkerRegex().IsMatch(first))
        {
            return "Verse";
        }

        return "Verse";
    }

    private static string CreateLabel(string type, int slideNumber)
        => type.Equals("Chorus", StringComparison.OrdinalIgnoreCase)
            ? $"Kiitikio - Slide {slideNumber}"
            : $"Couplet - Slide {slideNumber}";

    private static List<ParsedSection> InterleaveChorus(IReadOnlyList<ParsedSection> sections)
    {
        var verses = sections.Where(section => section.Type != "Chorus").ToList();
        var choruses = sections.Where(section => section.Type == "Chorus").ToList();
        if (choruses.Count == 0 || verses.Count == 0)
        {
            return sections.ToList();
        }

        if (IsAlreadyInterleaved(sections))
        {
            return sections.ToList();
        }

        var uniqueChorusTexts = choruses
            .Select(section => SongTextNormalizer.Normalize(section.Text))
            .Where(text => text.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (uniqueChorusTexts.Count != 1)
        {
            return sections.ToList();
        }

        var chorus = choruses[0];
        var interleaved = new List<ParsedSection>(verses.Count * 2);
        foreach (var verse in verses)
        {
            interleaved.Add(verse);
            interleaved.Add(chorus);
        }

        return interleaved;
    }

    public static bool IsAlreadyInterleaved(IReadOnlyList<ParsedSection> sections)
    {
        var types = sections.Select(section => section.Type).ToList();
        if (!types.Contains("Chorus", StringComparer.Ordinal))
        {
            return false;
        }

        for (var i = 0; i < types.Count - 1; i++)
        {
            if (types[i] == "Verse" && types[i + 1] != "Chorus")
            {
                return false;
            }
        }

        return true;
    }

    public static string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        name = name.Replace('_', ' ');
        name = LeadingNumberRegex().Replace(name, string.Empty);
        name = TrailingCopyRegex().Replace(name, string.Empty);
        name = WhitespaceRegex().Replace(name, " ").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileNameWithoutExtension(fileName);
            name = WhitespaceRegex().Replace(name, " ").Trim();
        }

        return name;
    }

    public static string SourceKey(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName).Trim();
        slug = WhitespaceRegex().Replace(slug, " ");
        return SourceKeyPrefix + slug;
    }

    [GeneratedRegex(@"slide(?<number>\d+)\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SlideNumberRegex();

    [GeneratedRegex(@"(?<=\p{L})\u0000\s*(?=\p{Ll})")]
    private static partial Regex SoftHyphenJoinRegex();

    [GeneratedRegex(@"[\u0001-\u0008\u000B\u000C\u000E-\u001F]")]
    private static partial Regex ControlCharactersRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^\s*(?:choeur|chœur|chorus|refrain|kiitikio|kirudi|kwaya)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ChorusMarkerRegex();

    [GeneratedRegex(@"^\s*\d+\s*[.)]")]
    private static partial Regex VerseMarkerRegex();

    [GeneratedRegex(@"^\s*\d+\s+")]
    private static partial Regex LeadingNumberRegex();

    [GeneratedRegex(@"\s*\(\d+\)\s*$")]
    private static partial Regex TrailingCopyRegex();
}

internal sealed record ParsedSection(string Type, string Label, string Text);

internal sealed record ParsedSwahiliSong(
    string FileName,
    string Title,
    IReadOnlyList<ParsedSection> Sections,
    string Error)
{
    public bool Success => string.IsNullOrWhiteSpace(Error) && Sections.Count > 0;
}
