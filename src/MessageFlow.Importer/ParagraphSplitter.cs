using System.Text;
using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class ParagraphSplitter
{
    private const int PreferredChunkLength = 900;
    private const int HardChunkLength = 1400;
    private const int MaxDetectedParagraphNumber = 999;

    public static IReadOnlyList<ParagraphDraft> Split(IReadOnlyList<ExtractedPage> pages)
    {
        var candidates = new List<ParagraphCandidate>();

        foreach (var page in pages)
        {
            foreach (var block in SplitPageText(page.Text))
            {
                var text = TextCleaner.CleanExtractedText(block);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (TryExtractLeadingParagraphNumber(text, out var paragraphNumber, out var paragraphText))
                {
                    candidates.Add(new ParagraphCandidate(paragraphText, page.PageNumber, paragraphNumber));
                    continue;
                }

                foreach (var chunk in SplitLongBlock(text))
                {
                    var chunkText = TextCleaner.CleanExtractedText(chunk);
                    if (!string.IsNullOrWhiteSpace(chunkText))
                    {
                        candidates.Add(new ParagraphCandidate(chunkText, page.PageNumber, null));
                    }
                }
            }
        }

        return BuildParagraphDrafts(candidates);
    }

    private static IEnumerable<string> SplitPageText(string text)
    {
        var normalized = TextCleaner.CleanExtractedText(text, preserveLineBreaks: true);

        var blocks = BlankLineRegex().Split(normalized)
            .Select(block => block.Trim())
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        if (blocks.Count > 1)
        {
            return blocks.SelectMany(SplitNumberedParagraphs);
        }

        var numberedBlocks = SplitNumberedParagraphs(normalized)
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        if (numberedBlocks.Count > 1)
        {
            return numberedBlocks;
        }

        var singleBlock = TextCleaner.CleanExtractedText(normalized);
        if (string.IsNullOrWhiteSpace(singleBlock))
        {
            return [];
        }

        return [singleBlock];
    }

    private static IEnumerable<string> SplitLongBlock(string block)
    {
        var normalized = TextCleaner.CleanExtractedText(block);
        if (normalized.Length <= HardChunkLength)
        {
            yield return normalized;
            yield break;
        }

        var sentences = SentenceBoundaryRegex().Split(normalized);
        var builder = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (builder.Length > 0 && builder.Length + trimmed.Length + 1 > PreferredChunkLength)
            {
                yield return builder.ToString();
                builder.Clear();
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(trimmed);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static IEnumerable<string> SplitNumberedParagraphs(string block)
    {
        var normalized = TextCleaner.CleanExtractedText(block, preserveLineBreaks: true);
        var matches = SermonParagraphNumberRegex().Matches(normalized);

        if (matches.Count <= 1)
        {
            yield return TextCleaner.CleanExtractedText(normalized);
            yield break;
        }

        for (var index = 0; index < matches.Count; index++)
        {
            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : normalized.Length;
            var paragraph = TextCleaner.CleanExtractedText(normalized[start..end]);

            if (!string.IsNullOrWhiteSpace(paragraph))
            {
                yield return paragraph;
            }
        }
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

    [GeneratedRegex(@"(?:\n\s*){2,}")]
    private static partial Regex BlankLineRegex();

    [GeneratedRegex(@"(?m)(?=^\s*\d{1,4}\s+[A-Z][a-z])")]
    private static partial Regex SermonParagraphNumberRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundaryRegex();

    [GeneratedRegex(@"^\s*(?<number>\d{1,3})(?:[.)])?\s+(?<text>(?:[""'\(\[]\s*)?[\p{L}][\s\S]*)$")]
    private static partial Regex LeadingParagraphNumberRegex();

    private sealed record ParagraphCandidate(string Text, int? PageNumber, int? DetectedParagraphNumber);
}
