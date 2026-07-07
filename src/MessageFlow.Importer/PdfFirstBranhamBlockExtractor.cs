using System.Text;
using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class PdfFirstBranhamBlockExtractor
{
    private const int MaxDetectedParagraphNumber = 999;
    private const string SpokenWordHeader = "THE SPOKEN WORD";

    public static IReadOnlyList<ParagraphDraft> Split(
        IReadOnlyList<ExtractedPage> pages,
        SermonMetadata metadata)
    {
        var candidates = new List<BlockCandidate>();
        BlockBuilder? current = null;
        var bodyStarted = false;

        foreach (var page in pages.OrderBy(page => page.PageNumber))
        {
            var lines = ExtractCleanLines(page.Text);
            var nonEmptyLines = lines
                .Where(line => !line.IsBlank)
                .Select(line => line.Text)
                .ToList();

            if (bodyStarted && IsTrailingPublicationPage(nonEmptyLines, metadata))
            {
                continue;
            }

            foreach (var lineItem in lines)
            {
                if (lineItem.IsBlank)
                {
                    if (current is not null && current.IsUnnumbered)
                    {
                        AddCurrent(candidates, current);
                        current = null;
                    }

                    continue;
                }

                var line = RemovePdfFurniture(lineItem.Text, metadata, bodyStarted);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!bodyStarted && IsPreBodyMetadataLine(line, metadata))
                {
                    continue;
                }

                bodyStarted = true;

                if (TryExtractLeadingParagraphNumber(line, out var detectedNumber, out var paragraphText))
                {
                    AddCurrent(candidates, current);
                    current = new BlockBuilder(paragraphText, page.PageNumber, detectedNumber);
                    continue;
                }

                if (current is null)
                {
                    current = new BlockBuilder(line, page.PageNumber, detectedParagraphNumber: null);
                }
                else
                {
                    current.Append(line);
                }
            }
        }

        AddCurrent(candidates, current);

        return BuildParagraphDrafts(candidates);
    }

    private static IReadOnlyList<LineItem> ExtractCleanLines(string pageText)
    {
        var normalized = pageText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return normalized
            .Split('\n')
            .Select(line =>
            {
                var cleaned = TextCleaner.CleanExtractedText(line);
                return string.IsNullOrWhiteSpace(cleaned)
                    ? new LineItem(string.Empty, IsBlank: true)
                    : new LineItem(cleaned, IsBlank: false);
            })
            .ToList();
    }

    private static string RemovePdfFurniture(string line, SermonMetadata metadata, bool bodyStarted)
    {
        line = RemoveInlineHeaderPrefix(line);
        line = RemoveInlineHeaderFragments(line);

        if (string.IsNullOrWhiteSpace(line) ||
            IsHeaderFooterLine(line, metadata, bodyStarted))
        {
            return string.Empty;
        }

        return line;
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

        return line.StartsWith(SpokenWordHeader + " ", StringComparison.Ordinal)
            ? line[SpokenWordHeader.Length..].TrimStart(' ', '-', ':')
            : line;
    }

    private static string RemoveInlineHeaderFragments(string line)
    {
        var cleaned = InlinePageNumberWithSpokenWordHeaderRegex().Replace(line, " ");
        return string.Equals(cleaned, line, StringComparison.Ordinal)
            ? line
            : TextCleaner.CleanExtractedText(cleaned);
    }

    private static bool IsHeaderFooterLine(string line, SermonMetadata metadata, bool bodyStarted)
    {
        if (PageNumberLineRegex().IsMatch(line) ||
            SpokenWordHeaderWithPageNumberLineRegex().IsMatch(line) ||
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

        if (IsTitleKey(normalized, metadata) && (!bodyStarted || TitleHeaderWithPageNumberRegex().IsMatch(line)))
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

        return normalized == "VOICEOFGODRECORDINGS" ||
               normalized == "ALLRIGHTSRESERVED" ||
               normalized == "COPYRIGHTNOTICE" ||
               normalized == "WWWBRANHAMORG" ||
               normalized.StartsWith("VGR", StringComparison.Ordinal);
    }

    private static bool IsPreBodyMetadataLine(string line, SermonMetadata metadata)
    {
        if (TryExtractLeadingParagraphNumber(line, out _, out _))
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

    private static bool TryExtractLeadingParagraphNumber(
        string text,
        out int paragraphNumber,
        out string paragraphText)
    {
        paragraphNumber = 0;
        paragraphText = text;

        var match = LeadingParagraphNumberRegex().Match(text);
        if (!match.Success ||
            !int.TryParse(match.Groups["number"].Value, out var detectedNumber) ||
            detectedNumber <= 0 ||
            detectedNumber > MaxDetectedParagraphNumber)
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

    private static IReadOnlyList<ParagraphDraft> BuildParagraphDrafts(IReadOnlyList<BlockCandidate> candidates)
    {
        var paragraphs = new List<ParagraphDraft>();
        var nextNumber = 1;

        foreach (var candidate in candidates)
        {
            var paragraphNumber = candidate.DetectedParagraphNumber is { } detectedNumber &&
                                  detectedNumber >= nextNumber
                ? detectedNumber
                : nextNumber;

            nextNumber = paragraphNumber + 1;
            paragraphs.Add(new ParagraphDraft(
                paragraphNumber,
                candidate.Text,
                TextCleaner.NormalizeSearchText(candidate.Text),
                candidate.PageNumber,
                candidate.DetectedParagraphNumber is not null));
        }

        return paragraphs;
    }

    private static void AddCurrent(ICollection<BlockCandidate> candidates, BlockBuilder? current)
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

        candidates.Add(new BlockCandidate(text, current.PageNumber, current.DetectedParagraphNumber));
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

    [GeneratedRegex(@"^\s*THE\s+SPOKEN\s+WORD\s+\d{1,4}\s*$")]
    private static partial Regex SpokenWordHeaderWithPageNumberLineRegex();

    [GeneratedRegex(@"^\s*\d{1,4}\s+THE\s+SPOKEN\s+WORD\s*$")]
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

    private sealed record LineItem(string Text, bool IsBlank);

    private sealed record BlockCandidate(string Text, int? PageNumber, int? DetectedParagraphNumber);

    private sealed class BlockBuilder
    {
        private readonly StringBuilder builder;

        public BlockBuilder(string text, int pageNumber, int? detectedParagraphNumber)
        {
            builder = new StringBuilder(text);
            PageNumber = pageNumber;
            DetectedParagraphNumber = detectedParagraphNumber;
        }

        public int PageNumber { get; }

        public int? DetectedParagraphNumber { get; }

        public bool IsUnnumbered => DetectedParagraphNumber is null;

        public string Text => builder.ToString();

        public void Append(string text)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(text);
        }
    }
}
