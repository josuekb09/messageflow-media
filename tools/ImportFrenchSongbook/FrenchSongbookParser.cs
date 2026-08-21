using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ImportFrenchSongbook;

internal static partial class FrenchSongbookParser
{
    public const string SourceKeyPrefix = "french-songbook://dinanga/";
    public const string SourceFolder = "Recueil de cantiques français";
    public const string LanguageCode = "fr";

    public static IReadOnlyList<ParsedFrenchSong> Parse(string pdfPath)
    {
        var lines = TwoColumnPdfExtractor.ExtractPages(pdfPath)
            .SelectMany(page => SplitPageLines(page.Text))
            .Where(line => !IsNoiseLine(line))
            .TakeWhile(line => !IsIndexLine(line))
            .ToList();

        var songs = SplitSongs(lines);
        return songs
            .Select(BuildSong)
            .Where(song => song.Sections.Count > 0 && !string.Equals(song.Number, "0", StringComparison.Ordinal))
            .ToList();
    }

    public static string DumpRawText(string pdfPath)
    {
        var builder = new StringBuilder();
        foreach (var page in TwoColumnPdfExtractor.ExtractPages(pdfPath))
        {
            builder.AppendLine($"===== PAGE {page.PageNumber} =====");
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitPageLines(string pageText)
    {
        var normalized = pageText
            .Replace('\u00A0', ' ')
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'');

        foreach (var raw in normalized.Split(['\r', '\n'], StringSplitOptions.None))
        {
            var text = CollapseSpaces(raw);
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static bool IsNoiseLine(string text)
    {
        return PageBannerRegex().IsMatch(text);
    }

    private static bool IsIndexLine(string text)
    {
        return IndexEntryRegex().IsMatch(text);
    }

    private static List<SongDraft> SplitSongs(IReadOnlyList<string> lines)
    {
        var songs = new List<SongDraft>();
        SongDraft? current = null;
        var seenHighNumber = false;
        var appendix = false;

        foreach (var line in lines)
        {
            if (TryReadSongNumber(line, out var number, out var rest))
            {
                if (int.TryParse(number.TrimEnd('A', 'B', 'C', 'a', 'b', 'c'), out var numeric) && numeric >= 50)
                {
                    seenHighNumber = true;
                }

                if (appendix is false &&
                    seenHighNumber &&
                    string.Equals(number, "1", StringComparison.OrdinalIgnoreCase))
                {
                    appendix = true;
                }

                var storedNumber = appendix ? "A" + number : number;
                if (current is not null)
                {
                    songs.Add(current);
                }

                current = new SongDraft(storedNumber);
                if (!string.IsNullOrWhiteSpace(rest))
                {
                    current.Lines.Add(rest);
                }

                continue;
            }

            current ??= new SongDraft("0");
            current.Lines.Add(line);
        }

        if (current is not null)
        {
            songs.Add(current);
        }

        return songs;
    }

    private static bool TryReadSongNumber(string text, out string number, out string rest)
    {
        number = string.Empty;
        rest = string.Empty;

        var match = SongNumberLineRegex().Match(text);
        if (!match.Success)
        {
            return false;
        }

        var digits = match.Groups["num"].Value;
        var letter = match.Groups["letter"].Value.ToUpperInvariant();
        rest = CollapseSpaces(match.Groups["rest"].Value);
        if (rest.Length > 0 && !LooksLikeTitle(rest))
        {
            return false;
        }

        if (!int.TryParse(digits, out var value) || value is < 1 or > 396)
        {
            return false;
        }

        number = digits + letter;
        return true;
    }

    private static bool LooksLikeTitle(string text)
    {
        if (RefrainMarkerRegex().IsMatch(text) || VerseMarkerRegex().IsMatch(text))
        {
            return false;
        }

        var letters = text.Count(char.IsLetter);
        var upper = text.Count(char.IsUpper);
        return letters >= 3 && upper >= letters * 0.45 && text.Length <= 90;
    }

    private static ParsedFrenchSong BuildSong(SongDraft draft)
    {
        var lines = UnwrapLines(draft.Lines);
        var title = string.Empty;
        var bodyStart = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (IsTranslationCue(lines[i]))
            {
                continue;
            }

            title = StripMusicalKey(lines[i]);
            bodyStart = i + 1;
            break;
        }

        while (bodyStart < lines.Count && IsTranslationCue(lines[bodyStart]))
        {
            bodyStart++;
        }

        title = string.IsNullOrWhiteSpace(title) ? $"Cantique {draft.Number}" : title;
        var body = lines.Skip(bodyStart).Where(line => !IsTranslationCue(line)).ToList();
        var sections = BuildSections(body);
        return new ParsedFrenchSong(draft.Number, $"{draft.Number}. {title}", sections);
    }

    private static List<ParsedSongSection> BuildSections(IReadOnlyList<string> body)
    {
        if (body.Count == 0)
        {
            return [];
        }

        var marked = ParseMarkedBlocks(body);
        if (marked.Any(block => block.Type == "Chorus"))
        {
            var refrainBlock = marked.First(block => block.Type == "Chorus");
            var verses = ExpandVerseChunks(
                marked.Where(block => block.Type != "Chorus").Select(block => block.Lines).ToList());
            var verseBlocks = verses
                .Select((verse, index) => new LyricBlock("Verse", $"Couplet {index + 1}", verse))
                .ToList();
            return InterleaveRefrain([.. verseBlocks, refrainBlock]);
        }

        var refrain = DetectRepeatedRefrain(body);
        if (refrain is not null)
        {
            var verses = ExpandVerseChunks(SplitAroundRefrain(body, refrain));
            var verseBlocks = verses
                .Select((verse, index) => new LyricBlock("Verse", $"Couplet {index + 1}", verse))
                .Where(block => block.Lines.Count > 0)
                .ToList();
            var refrainBlock = new LyricBlock("Chorus", "Refrain", refrain);
            return InterleaveRefrain([.. verseBlocks, refrainBlock]);
        }

        var stanzas = SplitStanzas(body);
        var chorus = FindChorusStanza(stanzas);
        if (chorus is not null && stanzas.Count > 1)
        {
            var refrainNorm = NormalizeStanza(chorus);
            var verses = stanzas
                .Where(stanza => NormalizeStanza(stanza) != refrainNorm)
                .Select((stanza, index) => new LyricBlock("Verse", $"Couplet {index + 1}", stanza))
                .ToList();
            if (verses.Count > 0)
            {
                return InterleaveRefrain([.. verses, new LyricBlock("Chorus", "Refrain", chorus)]);
            }
        }

        return stanzas
            .Select((stanza, index) => new ParsedSongSection("Verse", $"Couplet {index + 1}", JoinLines(stanza)))
            .ToList();
    }

    private static List<LyricBlock> ParseMarkedBlocks(IReadOnlyList<string> lines)
    {
        var blocks = new List<LyricBlock>();
        LyricBlock? current = null;
        var verseNumber = 0;

        foreach (var line in lines)
        {
            var refrainMatch = RefrainMarkerRegex().Match(line);
            if (refrainMatch.Success)
            {
                Flush(ref current, blocks);
                current = new LyricBlock("Chorus", "Refrain", []);
                var remainder = refrainMatch.Groups["rest"].Value.Trim();
                if (remainder.Length > 0)
                {
                    current.Lines.Add(remainder);
                }

                continue;
            }

            var verseMatch = VerseMarkerRegex().Match(line);
            if (verseMatch.Success)
            {
                Flush(ref current, blocks);
                verseNumber++;
                var label = verseMatch.Groups["num"].Success && verseMatch.Groups["num"].Length > 0
                    ? verseMatch.Groups["num"].Value
                    : verseNumber.ToString(CultureInfo.InvariantCulture);
                current = new LyricBlock("Verse", $"Couplet {label}", []);
                var remainder = verseMatch.Groups["rest"].Value.Trim();
                if (remainder.Length > 0)
                {
                    current.Lines.Add(remainder);
                }

                continue;
            }

            current ??= new LyricBlock("Verse", $"Couplet {++verseNumber}", []);
            current.Lines.Add(line);
        }

        Flush(ref current, blocks);
        return blocks.Where(block => block.Lines.Count > 0).ToList();
    }

    private static List<string>? DetectRepeatedRefrain(IReadOnlyList<string> lines)
    {
        List<string>? ababWithInternalRepeat = null;
        List<string>? ababFallback = null;
        for (var start = 0; start + 4 <= lines.Count; start++)
        {
            var a = NormalizeLine(lines[start]);
            var b = NormalizeLine(lines[start + 1]);
            var c = NormalizeLine(lines[start + 2]);
            var d = NormalizeLine(lines[start + 3]);
            if (a.Length > 10 && a == c && b == d && a != b)
            {
                var candidate = new List<string> { lines[start], lines[start + 1], lines[start + 2], lines[start + 3] };
                if (HasInternalRepeat(lines[start]))
                {
                    ababWithInternalRepeat ??= candidate;
                }
                else
                {
                    ababFallback ??= candidate;
                }
            }
        }

        if (ababWithInternalRepeat is not null)
        {
            return ababWithInternalRepeat;
        }

        if (ababFallback is not null)
        {
            return ababFallback;
        }

        for (var length = 4; length >= 2; length--)
        {
            for (var start = 0; start + length <= lines.Count; start++)
            {
                var window = lines.Skip(start).Take(length).ToList();
                var normalized = NormalizeStanza(window);
                if (normalized.Length < 24)
                {
                    continue;
                }

                var repeats = 0;
                for (var compare = start + length; compare + length <= lines.Count; compare++)
                {
                    if (NormalizeStanza(lines.Skip(compare).Take(length)) == normalized)
                    {
                        repeats++;
                    }
                }

                var required = length == 2 ? 2 : 1;
                if (repeats >= required)
                {
                    return window;
                }
            }
        }

        return null;
    }

    private static List<List<string>> SplitAroundRefrain(IReadOnlyList<string> lines, IReadOnlyList<string> refrain)
    {
        var verses = new List<List<string>>();
        var current = new List<string>();
        var refrainNorm = NormalizeStanza(refrain);
        var i = 0;
        while (i < lines.Count)
        {
            if (i + refrain.Count <= lines.Count &&
                NormalizeStanza(lines.Skip(i).Take(refrain.Count)) == refrainNorm)
            {
                if (current.Count > 0)
                {
                    verses.Add(current);
                    current = [];
                }

                i += refrain.Count;
                continue;
            }

            current.Add(lines[i]);
            i++;
        }

        if (current.Count > 0)
        {
            verses.Add(current);
        }

        return verses;
    }

    private static List<List<string>> ExpandVerseChunks(IReadOnlyList<List<string>> chunks)
    {
        var firstCount = chunks.FirstOrDefault()?.Count ?? 4;
        var stanzaLength = firstCount is >= 3 and <= 5 ? firstCount : 4;

        var stanzas = new List<List<string>>();
        foreach (var chunk in chunks)
        {
            if (chunk.Count <= stanzaLength + 1)
            {
                stanzas.Add(chunk);
                continue;
            }

            for (var i = 0; i < chunk.Count; i += stanzaLength)
            {
                stanzas.Add(chunk.Skip(i).Take(stanzaLength).ToList());
            }
        }

        if (stanzas.Count >= 2 && stanzas[^1].Count <= 2)
        {
            stanzas[^2].AddRange(stanzas[^1]);
            stanzas.RemoveAt(stanzas.Count - 1);
        }

        return stanzas.Where(stanza => stanza.Count > 0).ToList();
    }

    private static List<string>? FindChorusStanza(IReadOnlyList<List<string>> stanzas)
    {
        if (stanzas.Count < 2)
        {
            return null;
        }

        var duplicate = stanzas
            .GroupBy(NormalizeStanza)
            .Where(group => group.Key.Length >= 24 && group.Count() >= 2)
            .Select(group => group.First())
            .FirstOrDefault();
        if (duplicate is not null)
        {
            return duplicate;
        }

        var marked = stanzas.FirstOrDefault(LooksLikeChorusStanza);
        if (marked is not null)
        {
            return marked;
        }

        if (stanzas.Count >= 2 && StanzaLooksLikeWrittenOnceChorus(stanzas[0], stanzas[1]))
        {
            return stanzas[1];
        }

        return null;
    }

    private static bool StanzaLooksLikeWrittenOnceChorus(List<string> firstVerse, List<string> candidate)
    {
        if (candidate.Any(HasInternalRepeat) || candidate.Any(line => RepeatMarkerRegex().IsMatch(line)))
        {
            return true;
        }

        var hook = NormalizeLine(firstVerse[^1]);
        foreach (var line in candidate)
        {
            var normalized = NormalizeLine(line);
            if (hook.Length >= 16 && normalized.Length >= 16 &&
                (normalized == hook ||
                 normalized.Contains(hook) ||
                 hook.Contains(normalized) ||
                 CommonSuffixLength(hook, normalized) >= 16))
            {
                return true;
            }
        }

        return false;
    }

    private static int CommonSuffixLength(string left, string right)
    {
        var count = 0;
        var i = left.Length - 1;
        var j = right.Length - 1;
        while (i >= 0 && j >= 0 && left[i] == right[j])
        {
            count++;
            i--;
            j--;
        }

        return count;
    }

    private static List<List<string>> SplitStanzas(IReadOnlyList<string> lines)
    {
        var stanzas = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += 4)
        {
            stanzas.Add(lines.Skip(i).Take(4).ToList());
        }

        return stanzas.Where(stanza => stanza.Count > 0).ToList();
    }

    private static bool LooksLikeChorusStanza(List<string> stanza)
    {
        if (stanza.Count < 2)
        {
            return false;
        }

        if (stanza.Any(line => RepeatMarkerRegex().IsMatch(line)))
        {
            return true;
        }

        return stanza.Count >= 4 &&
               NormalizeLine(stanza[0]) == NormalizeLine(stanza[2]) &&
               NormalizeLine(stanza[1]) == NormalizeLine(stanza[3]);
    }

    private static List<ParsedSongSection> InterleaveRefrain(IReadOnlyList<LyricBlock> blocks)
    {
        var refrains = blocks.Where(block => block.Type == "Chorus").ToList();
        var verses = blocks.Where(block => block.Type != "Chorus").ToList();
        if (refrains.Count == 0 || verses.Count == 0)
        {
            return blocks.Select(ToSection).ToList();
        }

        var alreadyInterleaved = true;
        for (var i = 0; i < blocks.Count - 1; i++)
        {
            if (blocks[i].Type == "Verse" && blocks[i + 1].Type != "Chorus")
            {
                alreadyInterleaved = false;
                break;
            }
        }

        if (alreadyInterleaved && refrains.Count >= Math.Max(1, verses.Count - 1))
        {
            return blocks.Select(ToSection).ToList();
        }

        var refrain = refrains[0];
        var sections = new List<ParsedSongSection>();
        foreach (var verse in verses)
        {
            sections.Add(ToSection(verse));
            sections.Add(ToSection(refrain));
        }

        return sections;
    }

    private static ParsedSongSection ToSection(LyricBlock block)
        => new(block.Type, block.Label, JoinLines(block.Lines));

    private static List<string> UnwrapLines(IReadOnlyList<string> lines)
    {
        var unwrapped = new List<string>();
        foreach (var line in lines)
        {
            if (unwrapped.Count > 0 && ShouldJoin(unwrapped[^1], line))
            {
                unwrapped[^1] = CollapseSpaces(unwrapped[^1] + " " + line);
                continue;
            }

            unwrapped.Add(line);
        }

        return unwrapped;
    }

    private static bool ShouldJoin(string previous, string next)
    {
        if (SongNumberLineRegex().IsMatch(next) || RefrainMarkerRegex().IsMatch(next) || VerseMarkerRegex().IsMatch(next))
        {
            return false;
        }

        if (previous.EndsWith('-'))
        {
            return true;
        }

        var last = previous[^1];
        if (last is '.' or '!' or '?' or ':' or ';' or '»' or '"' )
        {
            return false;
        }

        return next.Length > 0 && char.IsLower(next[0]);
    }

    private static bool IsTranslationCue(string text)
        => text.StartsWith('(') && text.EndsWith(')') && text.Length <= 80 && !text.Contains("2x", StringComparison.OrdinalIgnoreCase);

    private static string StripMusicalKey(string title)
    {
        title = MusicalKeyRegex().Replace(title, string.Empty);
        title = CollapseSpaces(title.Trim(' ', '.', '-', '–', '—'));
        return title;
    }

    private static string JoinLines(IReadOnlyList<string> lines)
        => string.Join(Environment.NewLine, lines.Select(line => line.Trim()).Where(line => line.Length > 0));

    private static void Flush(ref LyricBlock? current, ICollection<LyricBlock> blocks)
    {
        if (current is { Lines.Count: > 0 })
        {
            blocks.Add(current);
        }

        current = null;
    }

    private static string NormalizeStanza(IEnumerable<string> lines)
        => string.Join('\n', lines.Select(NormalizeLine));

    private static bool HasInternalRepeat(string line)
    {
        var parts = line.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        var first = NormalizeLine(parts[0]);
        return first.Length > 8 && parts.Skip(1).Any(part => NormalizeLine(part) == first);
    }

    private static string NormalizeLine(string line)
    {
        var stripped = RepeatMarkerRegex().Replace(line, string.Empty);
        var builder = new StringBuilder(stripped.Length);
        foreach (var character in stripped)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string CollapseSpaces(string value)
        => WhitespaceRegex().Replace(value.Trim(), " ");

    [GeneratedRegex(@"^(?:page(?:\s+\d+\s+sur\s+\d+)?|\d{1,3}\s+sur\s+\d{1,3})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PageBannerRegex();

    [GeneratedRegex(@"\.{5,}\s*\d{1,3}\s*[A-Za-z]?\s*$")]
    private static partial Regex IndexEntryRegex();

    [GeneratedRegex(@"^(?<num>\d{1,3})\s*(?<letter>[A-Za-z])?(?:\s+(?<rest>.+))?$")]
    private static partial Regex SongNumberLineRegex();

    [GeneratedRegex(@"\s+(?:[A-G](?:\s*[b#♭♯])?)$|\.{2,}[A-G](?:\s*[b#♭♯])?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MusicalKeyRegex();

    [GeneratedRegex(@"^(?:refrain|choeur|chœur|chorus)\b(?:\s*[:.\-–—)]\s*(?<rest>.*))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RefrainMarkerRegex();

    [GeneratedRegex(@"^(?:couplet|strophe|verse|vs\.?)\s*(?<num>\d+)?\s*(?:[.\-–—:)]\s*(?<rest>.*)?)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VerseMarkerRegex();

    [GeneratedRegex(@"\s*\((?:bis|\d+\s*[x×]|2x)\)\s*\.?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RepeatMarkerRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private sealed class SongDraft(string number)
    {
        public string Number { get; } = number;

        public List<string> Lines { get; } = [];
    }

    private sealed class LyricBlock(string type, string label, List<string> lines)
    {
        public string Type { get; } = type;

        public string Label { get; } = label;

        public List<string> Lines { get; } = lines;
    }
}

internal sealed record ParsedFrenchSong(
    string Number,
    string Title,
    IReadOnlyList<ParsedSongSection> Sections);

internal sealed record ParsedSongSection(
    string Type,
    string Label,
    string Text);
