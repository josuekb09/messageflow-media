using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class CircularLetterParagraphQualityFilter
{
    public static FilteredParagraphs Apply(IReadOnlyList<ParagraphDraft> paragraphs)
    {
        var repeatedHeaderFooterCandidates = FindRepeatedHeaderFooterCandidates(paragraphs);
        var accepted = new List<ParagraphDraft>();
        var rejectedPageNumbers = 0;
        var rejectedCorruptedText = 0;
        var rejectedHeadersFooters = 0;
        var rejectedTooShort = 0;

        foreach (var paragraph in paragraphs)
        {
            var reason = Classify(paragraph.Text, repeatedHeaderFooterCandidates);
            switch (reason)
            {
                case ParagraphRejectionReason.None:
                    accepted.Add(paragraph);
                    break;
                case ParagraphRejectionReason.PageNumber:
                    rejectedPageNumbers++;
                    break;
                case ParagraphRejectionReason.CorruptedText:
                    rejectedCorruptedText++;
                    break;
                case ParagraphRejectionReason.HeaderFooter:
                    rejectedHeadersFooters++;
                    break;
                case ParagraphRejectionReason.TooShort:
                    rejectedTooShort++;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported paragraph rejection reason: {reason}");
            }
        }

        var renumberedParagraphs = accepted
            .Select((paragraph, index) => new ParagraphDraft(
                index + 1,
                paragraph.Text,
                TextCleaner.NormalizeSearchText(paragraph.Text),
                paragraph.PageNumber,
                HasDetectedParagraphNumber: false))
            .ToList();

        var summary = new ParagraphQualitySummary(
            paragraphs.Count,
            renumberedParagraphs.Count,
            rejectedPageNumbers,
            rejectedCorruptedText,
            rejectedHeadersFooters,
            rejectedTooShort);

        return new FilteredParagraphs(renumberedParagraphs, summary);
    }

    private static ParagraphRejectionReason Classify(
        string value,
        ISet<string> repeatedHeaderFooterCandidates)
    {
        var text = TextCleaner.CleanExtractedText(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return ParagraphRejectionReason.TooShort;
        }

        if (PageNumberRegex().IsMatch(text))
        {
            return ParagraphRejectionReason.PageNumber;
        }

        if (LooksCorrupted(text))
        {
            return ParagraphRejectionReason.CorruptedText;
        }

        var headerKey = NormalizeHeaderFooterCandidate(text);
        if (!string.IsNullOrWhiteSpace(headerKey) &&
            repeatedHeaderFooterCandidates.Contains(headerKey))
        {
            return ParagraphRejectionReason.HeaderFooter;
        }

        return LooksTooShort(text)
            ? ParagraphRejectionReason.TooShort
            : ParagraphRejectionReason.None;
    }

    private static HashSet<string> FindRepeatedHeaderFooterCandidates(IReadOnlyList<ParagraphDraft> paragraphs)
    {
        var pageNumbersByText = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        var paragraphCountsByText = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var paragraph in paragraphs)
        {
            var text = TextCleaner.CleanExtractedText(paragraph.Text);
            var key = NormalizeHeaderFooterCandidate(text);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            paragraphCountsByText[key] = paragraphCountsByText.TryGetValue(key, out var count) ? count + 1 : 1;
            if (paragraph.PageNumber is not null)
            {
                if (!pageNumbersByText.TryGetValue(key, out var pageNumbers))
                {
                    pageNumbers = [];
                    pageNumbersByText[key] = pageNumbers;
                }

                pageNumbers.Add(paragraph.PageNumber.Value);
            }
        }

        return paragraphCountsByText
            .Where(item =>
                item.Value >= 2 &&
                IsLikelyHeaderFooter(item.Key) &&
                (!pageNumbersByText.TryGetValue(item.Key, out var pages) || pages.Count >= 2))
            .Select(item => item.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool LooksCorrupted(string text)
    {
        var visibleCount = 0;
        var letterOrDigitCount = 0;
        var symbolCount = 0;

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            visibleCount++;
            if (char.IsLetterOrDigit(character))
            {
                letterOrDigitCount++;
                continue;
            }

            if (!IsReadablePunctuation(character))
            {
                symbolCount++;
            }
        }

        if (visibleCount == 0)
        {
            return false;
        }

        var symbolRatio = symbolCount / (double)visibleCount;
        var readableRatio = letterOrDigitCount / (double)visibleCount;

        return CorruptionMarkerRegex().IsMatch(text) ||
               (visibleCount >= 5 && readableRatio < 0.45 && symbolRatio > 0.25) ||
               (visibleCount >= 12 && symbolRatio > 0.45);
    }

    private static bool LooksTooShort(string text)
    {
        var words = WordRegex().Matches(text);
        var letterCount = text.Count(char.IsLetter);
        var digitCount = text.Count(char.IsDigit);

        if (letterCount + digitCount < 8)
        {
            return true;
        }

        return words.Count < 2 && text.Length < 24;
    }

    private static string NormalizeHeaderFooterCandidate(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 140)
        {
            return string.Empty;
        }

        var normalized = TextCleaner.CleanExtractedText(text).ToUpperInvariant();
        normalized = HeaderFooterWhitespaceRegex().Replace(normalized, " ").Trim();

        return normalized.Length < 3 ? string.Empty : normalized;
    }

    private static bool IsLikelyHeaderFooter(string normalizedText)
    {
        if (HeaderFooterTokenRegex().IsMatch(normalizedText))
        {
            return true;
        }

        var words = WordRegex().Matches(normalizedText);
        return normalizedText.Length <= 32 && words.Count <= 4;
    }

    private static bool IsReadablePunctuation(char character)
    {
        return character is '.' or ',' or ';' or ':' or '!' or '?' or '\'' or '"' or '-' or '(' or ')' or '[' or ']' or '/'
            or '&';
    }

    [GeneratedRegex(@"^(?:page\s*)?\d{1,3}$", RegexOptions.IgnoreCase)]
    private static partial Regex PageNumberRegex();

    [GeneratedRegex(@"[#@$%^*_+=~`|\\<>]{3,}|[""']?#[$%][""']?")]
    private static partial Regex CorruptionMarkerRegex();

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex HeaderFooterWhitespaceRegex();

    [GeneratedRegex(@"\b(?:CIRCULAR\s+LETTER|EWALD\s+FRANK|BROTHER\s+FRANK|MISSIONS?[-\s]?CENTER|VOLKSMISSION|KREFELD|POSTFACH|WWW\.|HTTP|E-?MAIL|FAX|TELEPHONE|PHONE|PAGE)\b", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderFooterTokenRegex();

    private enum ParagraphRejectionReason
    {
        None,
        PageNumber,
        CorruptedText,
        HeaderFooter,
        TooShort
    }
}

public sealed record FilteredParagraphs(
    IReadOnlyList<ParagraphDraft> Paragraphs,
    ParagraphQualitySummary Summary);
