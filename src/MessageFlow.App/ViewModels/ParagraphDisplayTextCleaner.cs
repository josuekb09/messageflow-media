using System.Text.RegularExpressions;

namespace MessageFlow.App.ViewModels;

public static partial class ParagraphDisplayTextCleaner
{
    public static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var cleaned = text.Replace("\uFEFF", string.Empty, StringComparison.Ordinal).Trim();

        for (var index = 0; index < 3; index++)
        {
            var match = LeadingEditorNoteRegex().Match(cleaned);
            if (!match.Success)
            {
                break;
            }

            var remainder = cleaned[match.Length..].TrimStart();
            if (string.IsNullOrWhiteSpace(remainder))
            {
                break;
            }

            cleaned = remainder;
        }

        return cleaned;
    }

    public static string CreatePreview(string text, int maxLength = 160)
    {
        var displayText = Clean(text);
        var preview = WhiteSpaceRegex().Replace(displayText, " ").Trim();

        return preview.Length <= maxLength
            ? preview
            : $"{preview[..maxLength].TrimEnd()}...";
    }

    [GeneratedRegex("^\\[(?=[^\\]]{1,240}\\bEd\\.?\\])(?=[^\\]]{1,240}[-\\u2013\\u2014]\\s*Ed\\.?\\])[^\\]]{1,240}\\]\\s*", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingEditorNoteRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhiteSpaceRegex();
}
