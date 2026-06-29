using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageFlow.Importer;

public static partial class SermonMetadataParser
{
    private const string BrotherBranhamSourceName = "brother_branham";
    private const string CircularLetterSourceType = "CircularLetter";
    private const string EwaldFrankFullName = "Ewald Frank";
    private const string EwaldFrankDisplayName = "Brother Frank";

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

    public static SermonMetadata Parse(
        string filePath,
        string sourceRoot,
        SourceMetadataContext? sourceContext)
    {
        return IsBrotherBranhamSource(sourceContext)
            ? Parse(filePath, sourceRoot)
            : ParseGeneric(filePath, sourceRoot, sourceContext);
    }

    public static bool IsBrotherBranhamSource(SourceMetadataContext? sourceContext)
    {
        if (sourceContext is null)
        {
            return true;
        }

        return string.Equals(sourceContext.Name, BrotherBranhamSourceName, StringComparison.OrdinalIgnoreCase) ||
               ContainsIgnoreCase(sourceContext.DisplayName, "Brother Branham") ||
               ContainsIgnoreCase(sourceContext.DisplayName, "William Marrion Branham");
    }

    public static bool IsEwaldFrankSource(SourceMetadataContext? sourceContext)
    {
        return sourceContext is not null &&
               (ContainsIgnoreCase(sourceContext.DisplayName, EwaldFrankFullName) ||
                ContainsIgnoreCase(sourceContext.Name, "ewald_frank") ||
                ContainsIgnoreCase(sourceContext.Name, "ewald"));
    }

    public static ImportAuthorMetadata GetAuthorMetadata(SourceMetadataContext? sourceContext)
    {
        if (IsBrotherBranhamSource(sourceContext))
        {
            return new ImportAuthorMetadata(
                "William Marrion Branham",
                "Brother Branham",
                "Primary sermon author for the local MessageFlow sermon library.");
        }

        if (IsEwaldFrankSource(sourceContext))
        {
            return new ImportAuthorMetadata(
                EwaldFrankFullName,
                EwaldFrankDisplayName,
                "Imported from the Ewald Frank local PDF source.");
        }

        var displayName = sourceContext?.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Imported Source";
        }

        return new ImportAuthorMetadata(
            TrimTo(displayName, 200),
            TrimTo(displayName, 120),
            $"Imported from the {displayName} local PDF source.");
    }

    private static SermonMetadata ParseGeneric(
        string filePath,
        string sourceRoot,
        SourceMetadataContext? sourceContext)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var monthYear = TryFindMonthYear(fileName);
        var yearFromFile = monthYear?.Year ?? TryFindYearFromFileName(fileName);
        var treatAsCircularLetter = ShouldTreatAsCircularLetter(sourceContext, fileName, monthYear);
        var year = yearFromFile ??
                   (treatAsCircularLetter ? null : TryFindYearFromPath(filePath, sourceRoot)) ??
                   0;

        var title = BuildGenericTitle(
            fileName,
            sourceContext,
            monthYear,
            yearFromFile,
            treatAsCircularLetter);
        var code = BuildGenericCode(fileName, monthYear, yearFromFile, treatAsCircularLetter);
        DateTime? date = monthYear is null
            ? null
            : new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);

        return new SermonMetadata(
            TrimTo(title, 300),
            TrimTo(code, 80),
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

    private static int? TryFindYearFromFileName(string fileName)
    {
        var match = FourDigitYearRegex().Match(fileName);
        return match.Success && int.TryParse(match.Groups["year"].Value, CultureInfo.InvariantCulture, out var year)
            ? year
            : null;
    }

    private static MonthYear? TryFindMonthYear(string fileName)
    {
        var readableTitle = CleanGenericTitle(fileName);
        var monthNameMatch = MonthNameYearRegex().Match(readableTitle);
        if (monthNameMatch.Success &&
            TryParseMonthName(monthNameMatch.Groups["month"].Value, out var namedMonth) &&
            int.TryParse(monthNameMatch.Groups["year"].Value, CultureInfo.InvariantCulture, out var namedYear))
        {
            return new MonthYear(namedYear, namedMonth);
        }

        var numericMatch = NumericYearMonthRegex().Match(fileName);
        if (numericMatch.Success &&
            int.TryParse(numericMatch.Groups["year"].Value, CultureInfo.InvariantCulture, out var numericYear) &&
            int.TryParse(numericMatch.Groups["month"].Value, CultureInfo.InvariantCulture, out var numericMonth) &&
            numericMonth is >= 1 and <= 12)
        {
            return new MonthYear(numericYear, numericMonth);
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

    private static string BuildGenericTitle(
        string fileName,
        SourceMetadataContext? sourceContext,
        MonthYear? monthYear,
        int? yearFromFile,
        bool treatAsCircularLetter)
    {
        if (treatAsCircularLetter && yearFromFile is > 0)
        {
            return monthYear is null
                ? $"Circular Letter - {yearFromFile.Value:D4}"
                : $"Circular Letter - {CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthYear.Value.Month)} {monthYear.Value.Year:D4}";
        }

        if (IsEwaldFrankSermonSource(sourceContext))
        {
            var cleanFilenameTitle = CleanGenericTitle(fileName);
            if (IsMeaningfulGenericTitle(cleanFilenameTitle))
            {
                return cleanFilenameTitle;
            }
        }

        var titleWithoutMetadata = RemoveGenericMetadataTokens(fileName);
        if (IsMeaningfulGenericTitle(titleWithoutMetadata))
        {
            return titleWithoutMetadata;
        }

        var cleanTitle = CleanGenericTitle(fileName);
        if (IsMeaningfulGenericTitle(cleanTitle))
        {
            return cleanTitle;
        }

        if (!string.IsNullOrWhiteSpace(sourceContext?.DisplayName))
        {
            return yearFromFile is > 0
                ? $"{sourceContext.DisplayName.Trim()} - {yearFromFile.Value:D4}"
                : sourceContext.DisplayName.Trim();
        }

        return string.IsNullOrWhiteSpace(cleanTitle) ? "Untitled Document" : cleanTitle;
    }

    private static string BuildGenericCode(
        string fileName,
        MonthYear? monthYear,
        int? yearFromFile,
        bool treatAsCircularLetter)
    {
        if (treatAsCircularLetter && yearFromFile is > 0)
        {
            return monthYear is null
                ? $"CL-{yearFromFile.Value:D4}"
                : $"CL-{monthYear.Value.Year:D4}-{monthYear.Value.Month:D2}";
        }

        return SafeCodeFromFileName(fileName);
    }

    private static string RemoveGenericMetadataTokens(string fileName)
    {
        var value = NumericYearMonthRegex().Replace(fileName, " ");
        value = MonthNameYearRegex().Replace(value, " ");
        value = FourDigitYearRegex().Replace(value, " ");
        value = FileNoiseTokenRegex().Replace(value, " ");

        return CleanGenericTitle(value);
    }

    private static string CleanGenericTitle(string value)
    {
        var title = value
            .Replace('_', ' ')
            .Replace('.', ' ');

        title = HyphenSpacingRegex().Replace(title, " - ");
        title = WhiteSpaceRegex().Replace(title, " ").Trim();

        return title.Trim(' ', '-', '_', '.');
    }

    private static string SafeCodeFromFileName(string fileName)
    {
        var code = NonAlphaNumericRegex()
            .Replace(fileName.Trim(), "-")
            .Trim('-');

        return TrimTo(code.ToUpperInvariant(), 80);
    }

    private static bool ShouldTreatAsCircularLetter(
        SourceMetadataContext? sourceContext,
        string fileName,
        MonthYear? monthYear)
    {
        if (sourceContext is not null &&
            string.Equals(sourceContext.SourceType, CircularLetterSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsEwaldFrankSermonSource(sourceContext))
        {
            return ContainsIgnoreCase(fileName, "circular");
        }

        return ContainsIgnoreCase(fileName, "circular") ||
               ContainsIgnoreCase(sourceContext?.DisplayName, "circular") ||
               ContainsIgnoreCase(sourceContext?.Name, "circular") ||
               (IsEwaldFrankSource(sourceContext) && monthYear is not null);
    }

    private static bool IsEwaldFrankSermonSource(SourceMetadataContext? sourceContext)
    {
        return sourceContext is not null &&
               IsEwaldFrankSource(sourceContext) &&
               string.Equals(sourceContext.SourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase) &&
               (ContainsIgnoreCase(sourceContext.DisplayName, "sermon") ||
                ContainsIgnoreCase(sourceContext.DisplayName, "preaching") ||
                ContainsIgnoreCase(sourceContext.DisplayName, "broadcast") ||
                ContainsIgnoreCase(sourceContext.DisplayName, "service") ||
                ContainsIgnoreCase(sourceContext.Name, "sermon") ||
                ContainsIgnoreCase(sourceContext.Name, "preaching") ||
                ContainsIgnoreCase(sourceContext.Name, "broadcast") ||
                ContainsIgnoreCase(sourceContext.Name, "service"));
    }

    private static bool IsMeaningfulGenericTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var words = WordRegex()
            .Matches(value)
            .Select(match => match.Value)
            .ToList();

        if (words.Count == 0 || words.Sum(word => word.Length) < 4)
        {
            return false;
        }

        return words.Any(word => !IsNoiseWord(word));
    }

    private static bool IsNoiseWord(string value)
    {
        return value.Equals("en", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("de", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("fr", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("rb", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseMonthName(string value, out int month)
    {
        for (var index = 1; index <= 12; index++)
        {
            var fullName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(index);
            var abbreviatedName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(index);
            if (value.Equals(fullName, StringComparison.OrdinalIgnoreCase) ||
                value.Equals(abbreviatedName, StringComparison.OrdinalIgnoreCase) ||
                (index == 9 && value.Equals("Sept", StringComparison.OrdinalIgnoreCase)))
            {
                month = index;
                return true;
            }
        }

        month = 0;
        return false;
    }

    private static bool ContainsIgnoreCase(string? value, string expected)
    {
        return value?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private readonly record struct MonthYear(int Year, int Month);

    [GeneratedRegex(@"(?<!\d)(?:\d{2}|\d{4})[-_. ]?\d{2}[-_. ]?\d{2}[A-Za-z]?(?!\d)")]
    private static partial Regex SermonCodeRegex();

    [GeneratedRegex(@"(?<year>\d{2}|\d{4})[-_. ]?(?<month>\d{2})[-_. ]?(?<day>\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex SermonDateRegex();

    [GeneratedRegex(@"\b(?<month>January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)\s+(?<year>19\d{2}|20\d{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex MonthNameYearRegex();

    [GeneratedRegex(@"(?<!\d)(?<year>19\d{2}|20\d{2})[-_. ](?<month>0?[1-9]|1[0-2])(?!\d)")]
    private static partial Regex NumericYearMonthRegex();

    [GeneratedRegex(@"(?<!\d)(?<year>19\d{2}|20\d{2})(?!\d)")]
    private static partial Regex FourDigitYearRegex();

    [GeneratedRegex(@"\b(?:en|de|fr|rb|pdf)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FileNoiseTokenRegex();

    [GeneratedRegex(@"\s*-\s*")]
    private static partial Regex HyphenSpacingRegex();

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();
}
