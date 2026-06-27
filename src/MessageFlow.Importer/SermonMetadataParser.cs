using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class SermonMetadataParser
{
    public static SermonMetadata Parse(string filePath, string sourceRoot)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var sermonCode = TryFindSermonCode(fileName) ?? TrimTo(fileName, 80);
        var date = TryParseDateFromCode(sermonCode);
        var year = date?.Year ?? TryFindYearFromPath(filePath, sourceRoot) ?? DateTime.UtcNow.Year;
        var title = BuildTitle(fileName);

        return new SermonMetadata(
            TrimTo(title, 300),
            TrimTo(sermonCode, 80),
            year,
            date,
            Location: null,
            Language: "en");
    }

    private static string BuildTitle(string fileName)
    {
        var title = SermonCodeRegex().Replace(fileName, " ", 1);
        title = CleanTitle(title);
        return string.IsNullOrWhiteSpace(title) ? CleanTitle(fileName) : title;
    }

    private static string? TryFindSermonCode(string fileName)
    {
        var match = SermonCodeRegex().Match(fileName);
        return match.Success ? match.Value.Replace("_", "-", StringComparison.Ordinal) : null;
    }

    private static DateTime? TryParseDateFromCode(string sermonCode)
    {
        var match = SermonDateRegex().Match(sermonCode);
        if (!match.Success)
        {
            return null;
        }

        var yearText = match.Groups["year"].Value;
        var year = int.Parse(yearText, CultureInfo.InvariantCulture);
        if (yearText.Length == 2)
        {
            year += year >= 30 ? 1900 : 2000;
        }

        var month = int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);

        return DateTime.TryParse(
            $"{year:D4}-{month:D2}-{day:D2}",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static int? TryFindYearFromPath(string filePath, string sourceRoot)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, filePath);
        foreach (var part in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (int.TryParse(part, CultureInfo.InvariantCulture, out var year) &&
                year is >= 1800 and <= 2200)
            {
                return year;
            }
        }

        return null;
    }

    private static string CleanTitle(string value)
    {
        var title = value
            .Replace('_', ' ')
            .Replace('-', ' ');

        return WhiteSpaceRegex().Replace(title, " ").Trim();
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    [GeneratedRegex(@"(?<!\d)(?:\d{2}|\d{4})[-_. ]?\d{2}[-_. ]?\d{2}[A-Za-z]?(?!\d)")]
    private static partial Regex SermonCodeRegex();

    [GeneratedRegex(@"(?<year>\d{2}|\d{4})[-_. ]?(?<month>\d{2})[-_. ]?(?<day>\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex SermonDateRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();
}
