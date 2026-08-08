using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MessageFlow.Search;

namespace MessageFlow.App.Controls;

public sealed class HighlightedTextBlock : TextBlock
{
    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.Register(
            nameof(SourceText),
            typeof(string),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(string.Empty, OnHighlightPropertyChanged));

    public static readonly DependencyProperty HighlightQueryProperty =
        DependencyProperty.Register(
            nameof(HighlightQuery),
            typeof(string),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(string.Empty, OnHighlightPropertyChanged));

    public string SourceText
    {
        get => (string)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public string HighlightQuery
    {
        get => (string)GetValue(HighlightQueryProperty);
        set => SetValue(HighlightQueryProperty, value);
    }

    private static void OnHighlightPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        ((HighlightedTextBlock)dependencyObject).RefreshInlines();
    }

    private void RefreshInlines()
    {
        Inlines.Clear();

        var source = SourceText ?? string.Empty;
        if (source.Length == 0)
        {
            return;
        }

        var ranges = FindHighlightRanges(source, HighlightQuery).ToList();
        if (ranges.Count == 0)
        {
            Inlines.Add(new Run(source));
            return;
        }

        var position = 0;
        foreach (var range in ranges)
        {
            if (range.Start > position)
            {
                Inlines.Add(new Run(source[position..range.Start]));
            }

            var highlighted = new Run(source.Substring(range.Start, range.Length))
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 116, 144)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                TextDecorations = System.Windows.TextDecorations.Underline
            };
            highlighted.SetValue(Typography.CapitalsProperty, FontCapitals.Normal);
            Inlines.Add(highlighted);
            position = range.Start + range.Length;
        }

        if (position < source.Length)
        {
            Inlines.Add(new Run(source[position..]));
        }
    }

    private static IReadOnlyList<TextRange> FindHighlightRanges(string source, string? query)
    {
        var pattern = SermonTextSearchPattern.Create(query);
        if (pattern.IsEmpty)
        {
            return [];
        }

        var normalized = BuildNormalizedMap(source);
        if (normalized.Text.Length == 0)
        {
            return [];
        }

        if (pattern.IsExactPhrase)
        {
            return FindPhraseRanges(normalized, pattern.NormalizedQuery);
        }

        var phraseRanges = FindPhraseRanges(normalized, pattern.NormalizedQuery);
        if (phraseRanges.Count > 0)
        {
            return phraseRanges;
        }

        return FindTermRanges(normalized, pattern.Terms);
    }

    private static IReadOnlyList<TextRange> FindPhraseRanges(NormalizedTextMap normalized, string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return [];
        }

        var ranges = new List<TextRange>();
        var searchIndex = 0;
        while (searchIndex < normalized.Text.Length)
        {
            var matchIndex = normalized.Text.IndexOf(phrase, searchIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            AddMappedRange(ranges, normalized, matchIndex, phrase.Length);
            searchIndex = matchIndex + phrase.Length;
        }

        return MergeRanges(ranges);
    }

    private static IReadOnlyList<TextRange> FindTermRanges(
        NormalizedTextMap normalized,
        IReadOnlyCollection<string> terms)
    {
        if (terms.Count == 0)
        {
            return [];
        }

        var termSet = terms.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ranges = new List<TextRange>();
        var tokenStart = -1;
        for (var index = 0; index <= normalized.Text.Length; index++)
        {
            var isWord = index < normalized.Text.Length &&
                         (char.IsLetterOrDigit(normalized.Text[index]) || normalized.Text[index] == '\'');

            if (isWord && tokenStart < 0)
            {
                tokenStart = index;
            }
            else if (!isWord && tokenStart >= 0)
            {
                var token = normalized.Text[tokenStart..index];
                if (termSet.Contains(token))
                {
                    AddMappedRange(ranges, normalized, tokenStart, index - tokenStart);
                }

                tokenStart = -1;
            }
        }

        return MergeRanges(ranges);
    }

    private static void AddMappedRange(
        ICollection<TextRange> ranges,
        NormalizedTextMap normalized,
        int normalizedStart,
        int normalizedLength)
    {
        var normalizedEnd = normalizedStart + normalizedLength - 1;
        if (normalizedStart < 0 ||
            normalizedEnd < normalizedStart ||
            normalizedStart >= normalized.SourceIndexes.Count ||
            normalizedEnd >= normalized.SourceIndexes.Count)
        {
            return;
        }

        var sourceStart = normalized.SourceIndexes[normalizedStart];
        var sourceEnd = normalized.SourceIndexes[normalizedEnd] + 1;
        while (sourceStart < sourceEnd && char.IsWhiteSpace(normalized.Source[sourceStart]))
        {
            sourceStart++;
        }

        while (sourceEnd > sourceStart && char.IsWhiteSpace(normalized.Source[sourceEnd - 1]))
        {
            sourceEnd--;
        }

        if (sourceEnd > sourceStart)
        {
            ranges.Add(new TextRange(sourceStart, sourceEnd - sourceStart));
        }
    }

    private static IReadOnlyList<TextRange> MergeRanges(IEnumerable<TextRange> ranges)
    {
        var ordered = ranges
            .Where(range => range.Length > 0)
            .OrderBy(range => range.Start)
            .ToList();
        if (ordered.Count <= 1)
        {
            return ordered;
        }

        var merged = new List<TextRange> { ordered[0] };
        foreach (var range in ordered.Skip(1))
        {
            var previous = merged[^1];
            var previousEnd = previous.Start + previous.Length;
            if (range.Start <= previousEnd)
            {
                merged[^1] = previous with
                {
                    Length = Math.Max(previousEnd, range.Start + range.Length) - previous.Start
                };
                continue;
            }

            merged.Add(range);
        }

        return merged;
    }

    private static NormalizedTextMap BuildNormalizedMap(string source)
    {
        var text = new StringBuilder(source.Length);
        var sourceIndexes = new List<int>(source.Length);
        var pendingSpaceIndex = (int?)null;
        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            if (character is '\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\uFFFD')
            {
                continue;
            }

            var mapped = character switch
            {
                '\'' or '\u2018' or '\u2019' or '\u201A' or '\u201B' or '`' or '\u00B4' => '\'',
                '"' or '\u201C' or '\u201D' or '\u201E' or '\u201F' => ' ',
                '\r' or '\n' or '\t' => ' ',
                _ when char.IsWhiteSpace(character) => ' ',
                _ when char.IsControl(character) => ' ',
                _ when char.IsPunctuation(character) || char.IsSymbol(character) => ' ',
                _ => char.ToUpperInvariant(character)
            };

            if (mapped == ' ')
            {
                pendingSpaceIndex ??= index;
                continue;
            }

            if (pendingSpaceIndex is not null && text.Length > 0)
            {
                text.Append(' ');
                sourceIndexes.Add(pendingSpaceIndex.Value);
            }

            pendingSpaceIndex = null;
            text.Append(mapped);
            sourceIndexes.Add(index);
        }

        return new NormalizedTextMap(source, text.ToString(), sourceIndexes);
    }

    private sealed record NormalizedTextMap(
        string Source,
        string Text,
        IReadOnlyList<int> SourceIndexes);

    private readonly record struct TextRange(int Start, int Length);
}
