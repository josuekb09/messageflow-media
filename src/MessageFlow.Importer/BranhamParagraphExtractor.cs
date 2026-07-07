using System.Text;
using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class BranhamParagraphExtractor
{
    private const int MaxDetectedParagraphNumber = 999;
    private const int MaximumDetectedNumberJump = 5;
    private const string SpokenWordHeader = "THE SPOKEN WORD";

    public static IReadOnlyList<ParagraphDraft> Split(
        IReadOnlyList<ExtractedPage> pages,
        SermonMetadata metadata)
    {
        var candidates = new List<ParagraphCandidate>();
        ParagraphBuilder? current = null;
        var usedDetectedNumbers = new HashSet<int>();
        int? lastDetectedNumber = null;
        foreach (var line in ExtractBodyLines(pages, metadata))
        {
            if (TryExtractLeadingParagraphNumber(
                    line.Text,
                    lastDetectedNumber,
                    usedDetectedNumbers,
                    out var paragraphNumber,
                    out var paragraphText))
            {
                AddCurrentCandidate(candidates, current);
                current = new ParagraphBuilder(paragraphText, line.PageNumber, paragraphNumber);
                usedDetectedNumbers.Add(paragraphNumber);
                lastDetectedNumber = paragraphNumber;
                continue;
            }

            if (current is null)
            {
                current = new ParagraphBuilder(line.Text, line.PageNumber, detectedParagraphNumber: null);
                continue;
            }

            current.Append(line.Text);
        }

        AddCurrentCandidate(candidates, current);

        return BuildParagraphDrafts(candidates);
    }

    private static IEnumerable<BodyLine> ExtractBodyLines(
        IReadOnlyList<ExtractedPage> pages,
        SermonMetadata metadata)
    {
        var bodyStarted = false;

        foreach (var page in pages)
        {
            var lines = page.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => TextCleaner.CleanExtractedText(line))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (bodyStarted && IsTrailingPublicationPage(lines, metadata))
            {
                continue;
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                line = RemoveInlineHeaderPrefix(line);
                line = RemoveInlineHeaderFragments(line);
                if (string.IsNullOrWhiteSpace(line) ||
                    IsHeaderFooterLine(line, metadata) ||
                    (!bodyStarted && IsPreBodyMetadataLine(line, metadata)))
                {
                    continue;
                }

                bodyStarted = true;
                yield return new BodyLine(page.PageNumber, line);
            }
        }
    }

    private static bool IsTrailingPublicationPage(IReadOnlyList<string> lines, SermonMetadata metadata)
    {
        if (lines.Count == 0)
        {
            return false;
        }

        if (lines[0].Equals("Copyright notice", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalizedPage = NormalizeKey(string.Join(' ', lines));
        var publicationSignals = 0;
        if (normalizedPage.Contains("VOICEOFGODRECORDINGS", StringComparison.Ordinal))
        {
            publicationSignals++;
        }

        if (normalizedPage.Contains("ALLRIGHTSRESERVED", StringComparison.Ordinal))
        {
            publicationSignals++;
        }

        if (normalizedPage.Contains("WWWBRANHAMORG", StringComparison.Ordinal))
        {
            publicationSignals++;
        }

        if (normalizedPage.Contains("PRINTEDHEREINUNABRIDGED", StringComparison.Ordinal) ||
            normalizedPage.Contains("PRINTEDONAHOMEPRINTER", StringComparison.Ordinal))
        {
            publicationSignals++;
        }

        if (publicationSignals >= 2)
        {
            return true;
        }

        return IsTitleKey(NormalizeKey(lines[0]), metadata) &&
               normalizedPage.Contains("VOICEOFGOD", StringComparison.Ordinal);
    }

    private static string RemoveInlineHeaderPrefix(string line)
    {
        if (line.Equals(SpokenWordHeader, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var spokenWordHeaderWithPageNumber = SpokenWordHeaderWithPageNumberPrefixRegex().Match(line);
        if (spokenWordHeaderWithPageNumber.Success)
        {
            return spokenWordHeaderWithPageNumber.Groups["text"].Value.Trim();
        }

        var pageNumberWithSpokenWordHeader = PageNumberWithSpokenWordHeaderPrefixRegex().Match(line);
        if (pageNumberWithSpokenWordHeader.Success)
        {
            return pageNumberWithSpokenWordHeader.Groups["text"].Value.Trim();
        }

        if (!line.StartsWith(SpokenWordHeader + " ", StringComparison.OrdinalIgnoreCase))
        {
            return line;
        }

        return line[SpokenWordHeader.Length..].TrimStart(' ', '-', ':');
    }

    private static string RemoveInlineHeaderFragments(string line)
    {
        var cleaned = InlinePageNumberWithSpokenWordHeaderRegex().Replace(line, " ");
        return string.Equals(cleaned, line, StringComparison.Ordinal)
            ? line
            : TextCleaner.CleanExtractedText(cleaned);
    }

    private static bool IsHeaderFooterLine(string line, SermonMetadata metadata)
    {
        if (PageNumberLineRegex().IsMatch(line))
        {
            return true;
        }

        if (SpokenWordHeaderWithPageNumberLineRegex().IsMatch(line) ||
            PageNumberWithSpokenWordHeaderLineRegex().IsMatch(line))
        {
            return true;
        }

        if (line.StartsWith(metadata.SermonCode + " ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = NormalizeKey(line);
        if (normalized.Length == 0)
        {
            return true;
        }

        if (IsTitleKey(normalized, metadata))
        {
            return true;
        }

        if (TitleHeaderWithPageNumberRegex().IsMatch(line))
        {
            var withoutTrailingPageNumber = NormalizeKey(TitleHeaderWithPageNumberRegex().Replace(line, string.Empty));
            if (IsTitleKey(withoutTrailingPageNumber, metadata))
            {
                return true;
            }
        }

        return normalized.Contains("VOICEOFGODRECORDINGS", StringComparison.Ordinal) ||
               normalized.Contains("ALLRIGHTSRESERVED", StringComparison.Ordinal) ||
               normalized.Contains("WWWBRANHAMORG", StringComparison.Ordinal) ||
               normalized.Contains("COPYRIGHT", StringComparison.Ordinal) ||
               normalized.StartsWith("VGR", StringComparison.Ordinal);
    }

    private static bool IsPreBodyMetadataLine(string line, SermonMetadata metadata)
    {
        if (TryExtractLeadingParagraphNumber(
                line,
                lastDetectedNumber: null,
                usedDetectedNumbers: new HashSet<int>(),
                out _,
                out _))
        {
            return false;
        }

        var normalized = NormalizeKey(line);
        if (IsTitleKey(normalized, metadata))
        {
            return true;
        }

        if (normalized.Contains("ENGLISH", StringComparison.Ordinal) ||
            normalized.Contains("USA", StringComparison.Ordinal))
        {
            return true;
        }

        var letters = line.Count(char.IsLetter);
        if (letters == 0)
        {
            return true;
        }

        var uppercaseLetters = line.Count(character => char.IsLetter(character) && char.IsUpper(character));
        return uppercaseLetters >= Math.Max(4, letters * 0.85);
    }

    private static bool IsTitleKey(string normalizedLine, SermonMetadata metadata)
    {
        var title = NormalizeKey(metadata.Title);
        var titleWithoutVgr = NormalizeKey(RemoveVgrSuffix(metadata.Title));
        var fileTitle = NormalizeKey(SermonCodePrefixRegex().Replace(RemoveVgrSuffix(metadata.Title), string.Empty));

        return normalizedLine == title ||
               normalizedLine == titleWithoutVgr ||
               normalizedLine == fileTitle ||
               normalizedLine == NormalizeKey(metadata.SermonCode + metadata.Title) ||
               normalizedLine.StartsWith(NormalizeKey(metadata.SermonCode + titleWithoutVgr), StringComparison.Ordinal);
    }

    private static string RemoveVgrSuffix(string value)
    {
        return VgrSuffixRegex().Replace(value, string.Empty).Trim();
    }

    private static bool TryExtractLeadingParagraphNumber(
        string text,
        int? lastDetectedNumber,
        ISet<int> usedDetectedNumbers,
        out int paragraphNumber,
        out string paragraphText)
    {
        paragraphNumber = 0;
        paragraphText = text;

        var match = LeadingParagraphNumberRegex().Match(text);
        if (!match.Success ||
            !int.TryParse(match.Groups["number"].Value, out var detectedNumber) ||
            detectedNumber <= 0 ||
            detectedNumber > MaxDetectedParagraphNumber ||
            usedDetectedNumbers.Contains(detectedNumber) ||
            !IsPlausibleNextNumber(detectedNumber, lastDetectedNumber))
        {
            return false;
        }

        var cleanedText = TextCleaner.CleanExtractedText(match.Groups["text"].Value);
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            return false;
        }

        paragraphNumber = detectedNumber;
        paragraphText = cleanedText;
        return true;
    }

    private static bool IsPlausibleNextNumber(int detectedNumber, int? lastDetectedNumber)
    {
        if (lastDetectedNumber is null)
        {
            return detectedNumber <= MaximumDetectedNumberJump;
        }

        return detectedNumber > lastDetectedNumber.Value &&
               detectedNumber <= lastDetectedNumber.Value + MaximumDetectedNumberJump;
    }

    private static IReadOnlyList<ParagraphDraft> BuildParagraphDrafts(IReadOnlyList<ParagraphCandidate> candidates)
    {
        var reservedDetectedNumbers = candidates
            .Where(candidate => candidate.DetectedParagraphNumber is not null)
            .Select(candidate => candidate.DetectedParagraphNumber!.Value)
            .ToHashSet();

        var usedNumbers = new HashSet<int>();
        var nextFallbackNumber = 1;
        var paragraphs = new List<ParagraphDraft>();

        foreach (var candidate in candidates)
        {
            if (candidate.DetectedParagraphNumber is { } detectedNumber &&
                !usedNumbers.Contains(detectedNumber))
            {
                usedNumbers.Add(detectedNumber);
                paragraphs.Add(CreateDraft(candidate, detectedNumber, hasDetectedParagraphNumber: true));
                continue;
            }

            var fallbackNumber = GetNextFallbackParagraphNumber(
                usedNumbers,
                reservedDetectedNumbers,
                ref nextFallbackNumber);

            usedNumbers.Add(fallbackNumber);
            paragraphs.Add(CreateDraft(candidate, fallbackNumber, hasDetectedParagraphNumber: false));
        }

        return paragraphs;
    }

    private static ParagraphDraft CreateDraft(
        ParagraphCandidate candidate,
        int paragraphNumber,
        bool hasDetectedParagraphNumber)
    {
        return new ParagraphDraft(
            paragraphNumber,
            candidate.Text,
            TextCleaner.NormalizeSearchText(candidate.Text),
            candidate.PageNumber,
            hasDetectedParagraphNumber);
    }

    private static int GetNextFallbackParagraphNumber(
        ISet<int> usedNumbers,
        ISet<int> reservedDetectedNumbers,
        ref int nextFallbackNumber)
    {
        while (usedNumbers.Contains(nextFallbackNumber) ||
               reservedDetectedNumbers.Contains(nextFallbackNumber))
        {
            nextFallbackNumber++;
        }

        return nextFallbackNumber++;
    }

    private static void AddCurrentCandidate(ICollection<ParagraphCandidate> candidates, ParagraphBuilder? current)
    {
        if (current is null)
        {
            return;
        }

        var text = TextCleaner.CleanExtractedText(current.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        candidates.Add(new ParagraphCandidate(text, current.PageNumber, current.DetectedParagraphNumber));
    }

    private static string NormalizeKey(string value)
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

    [GeneratedRegex(@"^\s*\d{1,4}\s*$")]
    private static partial Regex PageNumberLineRegex();

    [GeneratedRegex(@"^\s*THE\s+SPOKEN\s+WORD\s+\d{1,4}\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex SpokenWordHeaderWithPageNumberLineRegex();

    [GeneratedRegex(@"^\s*\d{1,4}\s+THE\s+SPOKEN\s+WORD\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PageNumberWithSpokenWordHeaderLineRegex();

    [GeneratedRegex(@"^\s*THE\s+SPOKEN\s+WORD\s+\d{1,4}(?:\.{3})?\s*(?<text>.+)$")]
    private static partial Regex SpokenWordHeaderWithPageNumberPrefixRegex();

    [GeneratedRegex(@"^\s*\d{1,4}\s+THE\s+SPOKEN\s+WORD(?:\.{3})?\s*(?<text>.+)$")]
    private static partial Regex PageNumberWithSpokenWordHeaderPrefixRegex();

    [GeneratedRegex(@"\s+\d{1,4}\s+THE\s+SPOKEN\s+WORD(?:\.{3})?\s*")]
    private static partial Regex InlinePageNumberWithSpokenWordHeaderRegex();

    [GeneratedRegex(@"\s+\d{1,4}\s*$")]
    private static partial Regex TitleHeaderWithPageNumberRegex();

    [GeneratedRegex(@"\bVGR\b\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex VgrSuffixRegex();

    [GeneratedRegex(@"^\s*\d{2}-\d{4}[A-Z]?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex SermonCodePrefixRegex();

    [GeneratedRegex(@"^\s*(?<number>\d{1,3})(?:[.)])?\s+(?<text>(?:[""'\(\[]\s*)?[\p{L}][\s\S]*)$")]
    private static partial Regex LeadingParagraphNumberRegex();

    private sealed record BodyLine(int PageNumber, string Text);

    private sealed record ParagraphCandidate(string Text, int? PageNumber, int? DetectedParagraphNumber);

    private sealed class ParagraphBuilder
    {
        private readonly StringBuilder builder;

        public ParagraphBuilder(string text, int pageNumber, int? detectedParagraphNumber)
        {
            builder = new StringBuilder(text);
            PageNumber = pageNumber;
            DetectedParagraphNumber = detectedParagraphNumber;
        }

        public int PageNumber { get; }

        public int? DetectedParagraphNumber { get; }

        public string Text => builder.ToString();

        public void Append(string text)
        {
            if (builder.Length == 0)
            {
                builder.Append(text);
                return;
            }

            builder.Append(' ');
            builder.Append(text);
        }
    }
}
