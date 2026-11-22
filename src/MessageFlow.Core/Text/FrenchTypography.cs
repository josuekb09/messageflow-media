using System.Text.RegularExpressions;

namespace MessageFlow.Core.Text;

/// <summary>
/// Display-time French typography. The stored text is never changed: these helpers only choose
/// which spaces may break a line when the text is laid out. A space before : ; ! ? or » and a
/// space after « must stay with the neighbouring word, so each one is shown as a no-break space
/// (U+00A0). A space that is not in the source is never added.
/// </summary>
public static partial class FrenchTypography
{
    public const char NoBreakSpace = '\u00A0';
    public const char NarrowNoBreakSpace = '\u202F';

    // Private-use markers that carry a protected space through ProjectionTextCleaner, which would
    // otherwise strip whitespace before punctuation. They never appear in stored text.
    private const char ProtectedSpace = '\uE000';
    private const char ProtectedNarrowSpace = '\uE001';

    /// <summary>
    /// Characters that may separate words and break a line: every whitespace character except
    /// the two no-break spaces.
    /// </summary>
    public const string BreakingWhiteSpacePattern = @"[^\S\u00A0\u202F]";

    /// <summary>
    /// Returns <paramref name="text"/> with the space before : ; ! ? » and after « made
    /// non-breaking. Only spaces change; every other character stays as it is.
    /// </summary>
    public static string KeepWithNeighbour(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return Restore(Protect(text));
    }

    /// <summary>
    /// First half of <see cref="KeepWithNeighbour"/>: swaps those spaces for markers a cleaner
    /// will not touch. Pair it with <see cref="Restore"/>.
    /// </summary>
    public static string Protect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var protectedText = SpaceBeforeClosingMarkRegex().Replace(text, ReplaceRun);
        return SpaceAfterOpeningGuillemetRegex().Replace(protectedText, ReplaceRun);
    }

    /// <summary>
    /// Second half of <see cref="KeepWithNeighbour"/>: turns the markers into no-break spaces.
    /// </summary>
    public static string Restore(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return text
            .Replace(ProtectedSpace, NoBreakSpace)
            .Replace(ProtectedNarrowSpace, NarrowNoBreakSpace);
    }

    private static string ReplaceRun(Match match)
    {
        // A narrow no-break space in the source keeps its width; any other space run becomes
        // one regular-width no-break space.
        return match.Value.Contains(NarrowNoBreakSpace)
            ? ProtectedNarrowSpace.ToString()
            : ProtectedSpace.ToString();
    }

    [GeneratedRegex(@"[^\S\r\n]+(?=[:;!?»])", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceBeforeClosingMarkRegex();

    [GeneratedRegex(@"(?<=«)[^\S\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceAfterOpeningGuillemetRegex();
}
