using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Core.Text;

namespace MessageFlow.Importer;

public static partial class TextCleaner
{
    public static string CleanExtractedText(string value, bool preserveLineBreaks = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = NormalizeCharacters(value);
        normalized = IsPastArtifactRegex().Replace(
            normalized,
            match => char.IsUpper(match.Value[0]) ? "Is past" : "is past");
        normalized = HyphenLineBreakRegex().Replace(normalized, string.Empty);
        normalized = MissingSpaceAfterPunctuationRegex().Replace(normalized, "$1 ");
        normalized = MissingSpaceAfterPeriodRegex().Replace(normalized, "$1. $2");
        normalized = SpaceBeforePunctuationRegex().Replace(normalized, "$1");

        if (!preserveLineBreaks)
        {
            return ProjectionTextCleaner.CleanSermonText(WhiteSpaceRegex().Replace(normalized, " ").Trim());
        }

        normalized = normalized
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        normalized = HorizontalWhiteSpaceRegex().Replace(normalized, " ");
        normalized = WhiteSpaceAroundLineBreakRegex().Replace(normalized, "\n");
        normalized = ExcessiveBlankLineRegex().Replace(normalized, "\n\n");

        return ProjectionTextCleaner.CleanSermonText(normalized.Trim());
    }

    public static string CleanToken(string value)
    {
        return CleanExtractedText(value).Trim();
    }

    public static string NormalizeSearchText(string value)
    {
        return CleanExtractedText(value).ToUpperInvariant();
    }

    public static string BuildPreview(string value, int maxLength)
    {
        var preview = CleanExtractedText(value);
        if (preview.Length <= maxLength)
        {
            return preview;
        }

        return string.Concat(preview.AsSpan(0, maxLength).TrimEnd(), "...");
    }

    private static string NormalizeCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Normalize(NormalizationForm.FormKC))
        {
            if (character is '\u200B' or '\u200C' or '\u200D' or '\uFEFF' or '\uFFFD')
            {
                continue;
            }

            if (character == '\u2026')
            {
                builder.Append("...");
                continue;
            }

            var replacement = character switch
            {
                '\u00A0' or '\u1680' or '\u2000' or '\u2001' or '\u2002' or '\u2003' or '\u2004'
                    or '\u2005' or '\u2006' or '\u2007' or '\u2008' or '\u2009' or '\u200A' or '\u202F'
                    or '\u205F' or '\u3000' => ' ',
                '\u2018' or '\u2019' or '\u201A' or '\u201B' => '\'',
                '\u201C' or '\u201D' or '\u201E' or '\u201F' => '"',
                '\u2010' or '\u2011' or '\u2012' or '\u2013' or '\u2014' or '\u2212' => '-',
                _ => character
            };

            var category = CharUnicodeInfo.GetUnicodeCategory(replacement);
            if (category is UnicodeCategory.PrivateUse or UnicodeCategory.Surrogate or UnicodeCategory.OtherNotAssigned)
            {
                continue;
            }

            if (char.IsControl(replacement) &&
                replacement is not '\n' and not '\r' and not '\t')
            {
                continue;
            }

            builder.Append(replacement);
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"-\s*(?:\r\n|\r|\n)\s*")]
    private static partial Regex HyphenLineBreakRegex();

    [GeneratedRegex(@"\bispast\b", RegexOptions.IgnoreCase)]
    private static partial Regex IsPastArtifactRegex();

    [GeneratedRegex(@"([,;:!?])(?=\p{L}|\p{N})")]
    private static partial Regex MissingSpaceAfterPunctuationRegex();

    [GeneratedRegex(@"([a-z])\.([A-Z])")]
    private static partial Regex MissingSpaceAfterPeriodRegex();

    [GeneratedRegex(@"\s+([,.;:!?%\]\)])")]
    private static partial Regex SpaceBeforePunctuationRegex();

    [GeneratedRegex(@"[ \t\f\v]+")]
    private static partial Regex HorizontalWhiteSpaceRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t]*")]
    private static partial Regex WhiteSpaceAroundLineBreakRegex();

    [GeneratedRegex(@"(?:\n\s*){3,}")]
    private static partial Regex ExcessiveBlankLineRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();
}
