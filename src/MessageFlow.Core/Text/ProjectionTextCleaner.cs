using System.Text.RegularExpressions;

namespace MessageFlow.Core.Text;

public static partial class ProjectionTextCleaner
{
    private static readonly (Regex Pattern, string Replacement)[] KnownSpacedWords =
    [
        CreateSpacedWordReplacement("THE"),
        CreateSpacedWordReplacement("SPOKEN"),
        CreateSpacedWordReplacement("WORD"),
        CreateSpacedWordReplacement("GOD"),
        CreateSpacedWordReplacement("LORD"),
        CreateSpacedWordReplacement("JESUS"),
        CreateSpacedWordReplacement("CHRIST"),
        CreateSpacedWordReplacement("SPIRIT"),
        CreateSpacedWordReplacement("WHAT"),
        CreateSpacedWordReplacement("HEAREST"),
        CreateSpacedWordReplacement("THOU"),
        CreateSpacedWordReplacement("ELIJAH"),
        CreateSpacedWordReplacement("WEDDING"),
        CreateSpacedWordReplacement("SUPPER"),
        CreateSpacedWordReplacement("SEED"),
        CreateSpacedWordReplacement("HEIR"),
        CreateSpacedWordReplacement("WITH"),
        CreateSpacedWordReplacement("SHUCK"),
        CreateSpacedWordReplacement("NOT"),
        CreateSpacedWordReplacement("IS"),
        CreateSpacedWordReplacement("OF"),
        CreateSpacedWordReplacement("AND"),
        CreateSpacedWordReplacement("IN"),
        CreateSpacedWordReplacement("ON"),
        CreateSpacedWordReplacement("TO"),
        CreateSpacedWordReplacement("BE"),
        CreateSpacedWordReplacement("IT"),
        CreateSpacedWordReplacement("HE"),
        CreateSpacedWordReplacement("WE"),
        CreateSpacedWordReplacement("MY"),
        CreateSpacedWordReplacement("BY"),
        CreateSpacedWordReplacement("AS"),
        CreateSpacedWordReplacement("AT"),
        CreateSpacedWordReplacement("OR"),
        CreateSpacedWordReplacement("NO"),
        CreateSpacedWordReplacement("ME"),
        CreateSpacedWordReplacement("UP")
    ];
    private static readonly (Regex Pattern, string Replacement)[] KnownPrefixWords =
        KnownSpacedWords
            .Select(replacement => CreatePrefixWordReplacement(replacement.Replacement))
            .ToArray();
    private static readonly (Regex Pattern, string Replacement)[] KnownArticleMergedWords =
        KnownSpacedWords
            .Where(replacement => replacement.Replacement.Length > 2 &&
                                  !replacement.Replacement.StartsWith('A'))
            .Select(replacement => CreateArticleMergedWordReplacement(replacement.Replacement))
            .ToArray();

    public static string CleanSermonText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Replace("\uFEFF", string.Empty, StringComparison.Ordinal).Trim();

        if (HasSuspiciousLetterSpacing(cleaned))
        {
            cleaned = RepairLetterSpacedWords(cleaned);
        }

        cleaned = SpaceBeforePunctuationRegex().Replace(cleaned, "$1");
        cleaned = RepeatedHorizontalWhiteSpaceRegex().Replace(cleaned, " ");
        cleaned = WhiteSpaceAroundLineBreakRegex().Replace(cleaned, "\n");
        cleaned = ExcessiveBlankLineRegex().Replace(cleaned, "\n\n");

        return cleaned.Trim();
    }

    public static bool HasSuspiciousLetterSpacing(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return KnownSpacedWords.Any(replacement => replacement.Pattern.IsMatch(value)) ||
               KnownPrefixWords.Any(replacement => replacement.Pattern.IsMatch(value)) ||
               KnownArticleMergedWords.Any(replacement => replacement.Pattern.IsMatch(value)) ||
               SingleLetterPrefixArtifactRegex().IsMatch(value);
    }

    private static string RepairLetterSpacedWords(string value)
    {
        var repaired = value;

        foreach (var (pattern, replacement) in KnownSpacedWords)
        {
            repaired = pattern.Replace(repaired, replacement);
        }

        foreach (var (pattern, replacement) in KnownArticleMergedWords)
        {
            repaired = pattern.Replace(repaired, replacement);
        }

        foreach (var (pattern, replacement) in KnownPrefixWords)
        {
            repaired = pattern.Replace(repaired, replacement);
        }

        for (var index = 0; index < 6; index++)
        {
            var next = SingleLetterPrefixArtifactRegex().Replace(repaired, "$1$2");
            if (string.Equals(next, repaired, StringComparison.Ordinal))
            {
                break;
            }

            repaired = next;
        }

        return repaired;
    }

    private static (Regex Pattern, string Replacement) CreateSpacedWordReplacement(string word)
    {
        var spacedLetters = string.Join(@"\s+", word.Select(character => Regex.Escape(character.ToString())));
        return (
            new Regex(
                $@"(?<![A-Za-z'’]){spacedLetters}(?![A-Za-z'’])",
                RegexOptions.CultureInvariant | RegexOptions.Compiled),
            word);
    }

    private static (Regex Pattern, string Replacement) CreatePrefixWordReplacement(string word)
    {
        var escapedFirst = Regex.Escape(word[0].ToString());
        var escapedRest = Regex.Escape(word[1..]);
        return (
            new Regex(
                $@"(?<![A-Za-z'’]){escapedFirst}\s+{escapedRest}(?![A-Za-z'’])",
                RegexOptions.CultureInvariant | RegexOptions.Compiled),
            word);
    }

    private static (Regex Pattern, string Replacement) CreateArticleMergedWordReplacement(string word)
    {
        var escapedFirst = Regex.Escape(word[0].ToString());
        var escapedRest = Regex.Escape(word[1..]);
        return (
            new Regex(
                $@"(?<![A-Za-z'’])A{escapedFirst}\s+{escapedRest}(?![A-Za-z'’])",
                RegexOptions.CultureInvariant | RegexOptions.Compiled),
            $"A {word}");
    }

    [GeneratedRegex(@"(?<![A-Za-z'’])([B-HJ-NP-Z])\s+([A-Z]{2,})(?![A-Za-z'’])", RegexOptions.CultureInvariant)]
    private static partial Regex SingleLetterPrefixArtifactRegex();

    [GeneratedRegex(@"\s+([,.;:!?%\]\)])", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceBeforePunctuationRegex();

    [GeneratedRegex(@"[ \t\f\v]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHorizontalWhiteSpaceRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t]*", RegexOptions.CultureInvariant)]
    private static partial Regex WhiteSpaceAroundLineBreakRegex();

    [GeneratedRegex(@"(?:\n\s*){3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExcessiveBlankLineRegex();
}
