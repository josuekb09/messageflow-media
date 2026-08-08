using System.Text;
using System.Text.RegularExpressions;

namespace MessageFlow.Search;

public sealed partial class SermonTextSearchPattern
{
    private readonly IReadOnlyList<string> terms;

    private SermonTextSearchPattern(
        string originalQuery,
        string normalizedQuery,
        IReadOnlyList<string> terms,
        bool isExactPhrase)
    {
        OriginalQuery = originalQuery;
        NormalizedQuery = normalizedQuery;
        this.terms = terms;
        IsExactPhrase = isExactPhrase;
    }

    public string OriginalQuery { get; }

    public string NormalizedQuery { get; }

    public bool IsExactPhrase { get; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(NormalizedQuery);

    public IReadOnlyList<string> Terms => terms;

    public static SermonTextSearchPattern Create(string? value)
    {
        var original = value?.Trim() ?? string.Empty;
        var isExactPhrase = TryExtractExactPhrase(original, out var exactPhrase);
        var normalized = NormalizeForSearch(isExactPhrase ? exactPhrase : original);
        var queryTerms = isExactPhrase || string.IsNullOrWhiteSpace(normalized)
            ? []
            : TokenizeNormalized(normalized)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        return new SermonTextSearchPattern(original, normalized, queryTerms, isExactPhrase);
    }

    public SermonTextSearchMatch? Match(string? text)
    {
        if (IsEmpty || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalizedText = NormalizeForSearch(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return null;
        }

        if (IsExactPhrase)
        {
            var firstPhraseIndex = normalizedText.IndexOf(NormalizedQuery, StringComparison.OrdinalIgnoreCase);
            return firstPhraseIndex < 0
                ? null
                : new SermonTextSearchMatch(firstPhraseIndex, CountPhraseOccurrences(normalizedText, NormalizedQuery));
        }

        if (terms.Count == 0)
        {
            return null;
        }

        var textTerms = WordRegex()
            .Matches(normalizedText)
            .Select(match => new IndexedTerm(match.Value, match.Index))
            .ToList();
        var groupedTerms = textTerms
            .GroupBy(term => term.Text, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        if (!terms.All(groupedTerms.ContainsKey))
        {
            return null;
        }

        var phraseIndex = normalizedText.IndexOf(NormalizedQuery, StringComparison.OrdinalIgnoreCase);
        if (phraseIndex >= 0)
        {
            return new SermonTextSearchMatch(phraseIndex, CountPhraseOccurrences(normalizedText, NormalizedQuery));
        }

        var orderedSpan = FindBestOrderedSpan(textTerms, terms);
        var firstTermIndexSum = terms.Sum(term => groupedTerms[term][0].Index);
        var occurrenceCount = terms.Min(term => groupedTerms[term].Count);
        var score = orderedSpan is null
            ? 200_000 + firstTermIndexSum
            : 100_000 + orderedSpan.Value;

        return new SermonTextSearchMatch(score, Math.Max(1, occurrenceCount));
    }

    public static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Trim().Length);
        foreach (var character in value.Trim().Normalize(NormalizationForm.FormKC))
        {
            if (character is '\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\uFFFD')
            {
                continue;
            }

            builder.Append(character switch
            {
                '\'' or '\u2018' or '\u2019' or '\u201A' or '\u201B' or '`' or '\u00B4' => '\'',
                '"' or '\u201C' or '\u201D' or '\u201E' or '\u201F' => ' ',
                '\r' or '\n' or '\t' => ' ',
                _ when char.IsWhiteSpace(character) => ' ',
                _ when char.IsControl(character) => ' ',
                _ when char.IsPunctuation(character) || char.IsSymbol(character) => ' ',
                _ => char.ToUpperInvariant(character)
            });
        }

        return SpaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    public static IReadOnlyList<string> TokenizeNormalized(string normalizedText)
    {
        return WordRegex()
            .Matches(normalizedText)
            .Select(match => match.Value)
            .Where(term => term.Length > 0)
            .ToList();
    }

    public static bool TryExtractExactPhrase(string? value, out string phrase)
    {
        phrase = string.Empty;
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return false;
        }

        if (!QuotesMatch(trimmed[0], trimmed[^1]))
        {
            return false;
        }

        phrase = trimmed[1..^1].Trim();
        return true;
    }

    private static int CountPhraseOccurrences(string normalizedText, string normalizedPhrase)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhrase))
        {
            return 0;
        }

        var count = 0;
        var startIndex = 0;
        while (startIndex < normalizedText.Length)
        {
            var matchIndex = normalizedText.IndexOf(normalizedPhrase, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            count++;
            startIndex = matchIndex + normalizedPhrase.Length;
        }

        return count;
    }

    private static int? FindBestOrderedSpan(
        IReadOnlyList<IndexedTerm> textTerms,
        IReadOnlyList<string> queryTerms)
    {
        var bestSpan = (int?)null;
        for (var startIndex = 0; startIndex < textTerms.Count; startIndex++)
        {
            if (!textTerms[startIndex].Text.Equals(queryTerms[0], StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var queryIndex = 1;
            var textIndex = startIndex + 1;
            while (queryIndex < queryTerms.Count && textIndex < textTerms.Count)
            {
                if (textTerms[textIndex].Text.Equals(queryTerms[queryIndex], StringComparison.OrdinalIgnoreCase))
                {
                    queryIndex++;
                }

                textIndex++;
            }

            if (queryIndex < queryTerms.Count)
            {
                continue;
            }

            var span = textTerms[textIndex - 1].Index - textTerms[startIndex].Index;
            bestSpan = bestSpan is null ? span : Math.Min(bestSpan.Value, span);
        }

        return bestSpan;
    }

    private static bool QuotesMatch(char opening, char closing)
    {
        return (opening == '"' && closing == '"') ||
               (opening == '\'' && closing == '\'') ||
               (opening == '\u201C' && closing == '\u201D') ||
               (opening == '\u2018' && closing == '\u2019');
    }

    private readonly record struct IndexedTerm(string Text, int Index);

    [GeneratedRegex(@"[\p{L}\p{Nd}]+(?:'[\p{L}\p{Nd}]+)*")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaceRegex();
}

public readonly record struct SermonTextSearchMatch(int Score, int OccurrenceCount);
