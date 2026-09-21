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
        var language = DetectLanguage(fileName, sourceRoot);
        var fileNameWithoutLanguage = StripLanguagePrefix(fileName);
        var dateCode = TryFindSermonCode(fileNameWithoutLanguage) ?? TrimTo(fileNameWithoutLanguage, 80);
        var sermonCode = PrefixSermonCode(dateCode, language, fileName);
        var date = TryParseDateFromCode(dateCode);
        var year = date?.Year ?? TryFindYearFromPath(filePath, sourceRoot) ?? DateTime.UtcNow.Year;
        var title = StripPublisherSuffix(BuildTitle(fileNameWithoutLanguage));

        return new SermonMetadata(
            TrimTo(title, 300),
            TrimTo(sermonCode, 80),
            year,
            date,
            Location: null,
            Language: language);
    }

    public static SermonMetadata Parse(
        string filePath,
        string sourceRoot,
        SourceMetadataContext? sourceContext)
    {
        if (IsEwaldFrankSource(sourceContext) || IsEwaldFrankFilePath(filePath))
        {
            return ParseEwaldFrank(filePath, sourceRoot, sourceContext);
        }

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
                ContainsIgnoreCase(sourceContext.DisplayName, EwaldFrankDisplayName) ||
                ContainsIgnoreCase(sourceContext.Name, "brother_frank") ||
                ContainsIgnoreCase(sourceContext.Name, "frank") ||
                ContainsIgnoreCase(sourceContext.Name, "ewald_frank") ||
                ContainsIgnoreCase(sourceContext.Name, "ewald"));
    }

    public static bool IsEwaldFrankFilePath(string filePath)
    {
        var normalized = filePath.Replace('/', '\\');
        return normalized.Contains("Bro Frank", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("Ewald Frank", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("brother_frank", StringComparison.OrdinalIgnoreCase);
    }

    public static ImportAuthorMetadata GetAuthorMetadata(SourceMetadataContext? sourceContext, string? filePath = null)
    {
        if (IsEwaldFrankSource(sourceContext) || (filePath is not null && IsEwaldFrankFilePath(filePath)))
        {
            return new ImportAuthorMetadata(
                EwaldFrankFullName,
                EwaldFrankDisplayName,
                "Imported from the Brother Ewald Frank local publication library.");
        }

        if (IsBrotherBranhamSource(sourceContext))
        {
            return new ImportAuthorMetadata(
                "William Marrion Branham",
                "Brother Branham",
                "Primary sermon author for the local MessageFlow sermon library.");
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

    public static SermonMetadata ParseEwaldFrank(
        string filePath,
        string sourceRoot,
        SourceMetadataContext? sourceContext)
    {
        if (EwaldFrankMetadataCatalog.TryFind(filePath, out var catalogMetadata))
        {
            return BuildEwaldFrankCatalogMetadata(filePath, sourceRoot, catalogMetadata);
        }

        var rawFileName = Path.GetFileNameWithoutExtension(filePath);
        var fileName = Regex.Replace(rawFileName, @"\s*\(\d+\)$", string.Empty).Trim();

        // 1. Meeting transcripts with date, time, and location:
        // e.g. 1985-10-27-1400-Zurich-english.pdf, 1975-11-05-1930-Krefeld-english.pdf
        var meetingMatch = EwaldFrankMeetingRegex().Match(fileName);
        if (meetingMatch.Success)
        {
            var year = int.Parse(meetingMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(meetingMatch.Groups["month"].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(meetingMatch.Groups["day"].Value, CultureInfo.InvariantCulture);
            var location = meetingMatch.Groups["loc"].Value;
            var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
            var date = new DateTime(year, month, day);
            var title = $"Meeting in {location} - {monthName} {day}, {year}";
            var code = $"EF-{year:D4}-{month:D2}-{day:D2}-{location.ToUpperInvariant()}";

            return new SermonMetadata(
                TrimTo(title, 300),
                TrimTo(code, 80),
                year,
                date,
                Location: location,
                Language: "en");
        }

        // 2. Known books and brochures
        if (fileName.Contains("Marriage_the_ancient_problem", StringComparison.OrdinalIgnoreCase))
        {
            return new SermonMetadata(
                "Marriage — The Ancient Problem",
                "EF-MARRIAGE-THE-ANCIENT-PROBLEM",
                0,
                Date: null,
                Location: null,
                Language: "en");
        }

        if (fileName.Contains("Time_Is_At_Hand", StringComparison.OrdinalIgnoreCase))
        {
            return new SermonMetadata(
                "The Time Is At Hand",
                "EF-TIME-IS-AT-HAND",
                0,
                Date: null,
                Location: null,
                Language: "en");
        }

        if (fileName.Contains("A_prophet_sent_from_God", StringComparison.OrdinalIgnoreCase))
        {
            return new SermonMetadata(
                "William Branham — A Prophet Sent From God",
                "EF-WILLIAM-BRANHAM-A-PROPHET-SENT-FROM-GOD",
                0,
                Date: null,
                Location: null,
                Language: "en");
        }

        if (fileName.Contains("Revelation", StringComparison.OrdinalIgnoreCase))
        {
            return new SermonMetadata(
                "The Revelation — A Book With 7 Seals?",
                "EF-REVELATION-BOOK-7-SEALS",
                0,
                Date: null,
                Location: null,
                Language: "en");
        }

        // 3. Seasonal dates, e.g. Spring2005.pdf
        var seasonMatch = EwaldFrankSeasonalRegex().Match(fileName);
        if (seasonMatch.Success)
        {
            var seasonName = seasonMatch.Groups["season"].Value;
            var year = int.Parse(seasonMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
            var month = seasonName.Equals("Spring", StringComparison.OrdinalIgnoreCase) ? 3 :
                        seasonName.Equals("Summer", StringComparison.OrdinalIgnoreCase) ? 6 :
                        seasonName.Equals("Autumn", StringComparison.OrdinalIgnoreCase) || seasonName.Equals("Fall", StringComparison.OrdinalIgnoreCase) ? 9 : 12;
            var date = new DateTime(year, month, 1);
            var capitalizedSeason = char.ToUpperInvariant(seasonName[0]) + seasonName[1..].ToLowerInvariant();
            return new SermonMetadata(
                $"Circular Letter - {capitalizedSeason} {year}",
                $"CL-{year}-{capitalizedSeason.ToUpperInvariant()}",
                year,
                date,
                Location: null,
                Language: "en");
        }

        // 4. Compact month ranges: yyyyMMMM e.g. 19940304, 20060304, 20190405, 20041112
        var monthRangeMatch = EwaldFrankCompactMonthRangeRegex().Match(fileName);
        if (monthRangeMatch.Success)
        {
            var year = int.Parse(monthRangeMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
            var m1 = int.Parse(monthRangeMatch.Groups["m1"].Value, CultureInfo.InvariantCulture);
            var m2 = int.Parse(monthRangeMatch.Groups["m2"].Value, CultureInfo.InvariantCulture);
            if (m1 is >= 1 and <= 12 && m2 is >= 1 and <= 12)
            {
                var name1 = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m1);
                var name2 = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m2);
                var subtitle = ExtractEwaldFrankSubtitle(fileName);
                var title = string.IsNullOrWhiteSpace(subtitle)
                    ? $"Circular Letter - {name1}-{name2} {year}"
                    : $"Circular Letter - {name1}-{name2} {year} - {subtitle}";
                var code = $"CL-{year:D4}-{m1:D2}-{m2:D2}";
                var date = new DateTime(year, m1, 1);

                return new SermonMetadata(
                    TrimTo(title, 300),
                    TrimTo(code, 80),
                    year,
                    date,
                    Location: null,
                    Language: "en");
            }
        }

        // 5. Delimited or compact year-month: 1971-06, 197209, 1980-05, 200509, 200512, 201112, etc.
        var yearMonthMatch = EwaldFrankYearMonthRegex().Match(fileName);
        if (yearMonthMatch.Success)
        {
            var year = int.Parse(yearMonthMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(yearMonthMatch.Groups["month"].Value, CultureInfo.InvariantCulture);
            if (month is >= 1 and <= 12)
            {
                var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
                var subtitle = ExtractEwaldFrankSubtitle(fileName);
                var title = string.IsNullOrWhiteSpace(subtitle)
                    ? $"Circular Letter - {monthName} {year}"
                    : $"Circular Letter - {monthName} {year} - {subtitle}";
                var code = $"CL-{year:D4}-{month:D2}";
                var date = new DateTime(year, month, 1);

                return new SermonMetadata(
                    TrimTo(title, 300),
                    TrimTo(code, 80),
                    year,
                    date,
                    Location: null,
                    Language: "en");
            }
        }

        // 6. Year only, e.g. 2000-Circular-english.pdf
        var yearOnlyMatch = FourDigitYearRegex().Match(fileName);
        if (yearOnlyMatch.Success && int.TryParse(yearOnlyMatch.Value, CultureInfo.InvariantCulture, out var yearOnly))
        {
            var subtitle = ExtractEwaldFrankSubtitle(fileName);
            var title = string.IsNullOrWhiteSpace(subtitle)
                ? $"Circular Letter - {yearOnly}"
                : $"Circular Letter - {yearOnly} - {subtitle}";
            var code = $"CL-{yearOnly:D4}";
            var date = new DateTime(yearOnly, 1, 1);

            return new SermonMetadata(
                TrimTo(title, 300),
                TrimTo(code, 80),
                yearOnly,
                date,
                Location: null,
                Language: "en");
        }

        // Fallback for any generic Ewald Frank document
        var fallbackTitle = CleanGenericTitle(fileName);
        return new SermonMetadata(
            TrimTo(string.IsNullOrWhiteSpace(fallbackTitle) ? "Brother Frank Publication" : fallbackTitle, 300),
            SafeCodeFromFileName(fileName),
            0,
            Date: null,
            Location: null,
            Language: "en");
    }

    private static string ExtractEwaldFrankSubtitle(string fileName)
    {
        if (fileName.Contains("70_Weeks_of_Daniel", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("70 Weeks of Daniel", StringComparison.OrdinalIgnoreCase))
        {
            return "70 Weeks of Daniel";
        }

        if (fileName.Contains("Wakeupcall", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Wake up call", StringComparison.OrdinalIgnoreCase))
        {
            return "Wake-up Call";
        }

        return string.Empty;
    }

    private static SermonMetadata ParseGeneric(
        string filePath,
        string sourceRoot,
        SourceMetadataContext? sourceContext)
    {
        if (IsEwaldFrankSource(sourceContext) &&
            EwaldFrankMetadataCatalog.TryFind(filePath, out var catalogMetadata))
        {
            return BuildEwaldFrankCatalogMetadata(filePath, sourceRoot, catalogMetadata);
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (IsEwaldFrankSource(sourceContext) &&
            fileName.Contains("Revelation", StringComparison.OrdinalIgnoreCase))
        {
            return new SermonMetadata(
                "The Revelation \u2014 A Book With 7 Seals?",
                SafeCodeFromFileName(fileName),
                0,
                Date: null,
                Location: null,
                Language: "en");
        }

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

    private static SermonMetadata BuildEwaldFrankCatalogMetadata(
        string filePath,
        string sourceRoot,
        EwaldFrankCatalogMetadata catalogMetadata)
    {
        var date = catalogMetadata.TryCreateDate();
        var year = date?.Year ??
                   catalogMetadata.TryFindYear() ??
                   TryFindYearFromPath(filePath, sourceRoot) ??
                   0;
        var title = catalogMetadata.OfficialDisplayTitle;
        var code = catalogMetadata.IsCircularLetter
            ? BuildCircularLetterCode(year, date)
            : SafeCodeFromFileName(Path.GetFileNameWithoutExtension(filePath));

        return new SermonMetadata(
            TrimTo(title, 300),
            TrimTo(code, 80),
            year,
            date,
            Location: null,
            Language: NormalizeCatalogLanguage(catalogMetadata.Language));
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

    private static string BuildCircularLetterCode(int year, DateTime? date)
    {
        if (year <= 0)
        {
            return "CL-UNDATED";
        }

        return date is null
            ? $"CL-{year:D4}"
            : $"CL-{year:D4}-{date.Value.Month:D2}";
    }

    private static string NormalizeCatalogLanguage(string language)
    {
        if (language.Equals("English", StringComparison.OrdinalIgnoreCase) ||
            language.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return string.IsNullOrWhiteSpace(language) ? "en" : TrimTo(language, 20);
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
               value.Equals("swa", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("frn", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("vgr", StringComparison.OrdinalIgnoreCase) ||
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

    private static string DetectLanguage(string fileName, string sourceRoot)
    {
        var prefix = LanguagePrefixRegex().Match(fileName);
        if (prefix.Success)
        {
            return MapLanguagePrefix(prefix.Groups["lang"].Value);
        }

        var haystack = $"{fileName} {sourceRoot}";
        if (haystack.Contains("kiswahili", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("swahili", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("swh", StringComparison.OrdinalIgnoreCase))
        {
            return "sw";
        }

        if (haystack.Contains("francais", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("français", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("french", StringComparison.OrdinalIgnoreCase))
        {
            return "fr";
        }

        return "en";
    }

    private static string MapLanguagePrefix(string prefix)
    {
        return prefix.ToUpperInvariant() switch
        {
            "SWA" => "sw",
            "FRA" or "FRE" or "FRN" or "FR" => "fr",
            _ => "en"
        };
    }

    private static string StripLanguagePrefix(string fileName)
    {
        var stripped = LanguagePrefixRegex().Replace(fileName, string.Empty, 1);
        return string.IsNullOrWhiteSpace(stripped) ? fileName : stripped.TrimStart(' ', '-', '_');
    }

    private static string PrefixSermonCode(string dateCode, string language, string originalFileName)
    {
        var prefixMatch = LanguagePrefixRegex().Match(originalFileName);
        if (prefixMatch.Success)
        {
            var prefix = prefixMatch.Groups["lang"].Value.ToUpperInvariant();
            return dateCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? dateCode
                : $"{prefix}{dateCode}";
        }

        return language switch
        {
            "sw" => dateCode.StartsWith("SWA", StringComparison.OrdinalIgnoreCase) ? dateCode : "SWA" + dateCode,
            "fr" => dateCode.StartsWith("FR", StringComparison.OrdinalIgnoreCase) ? dateCode : "FRN" + dateCode,
            _ => dateCode
        };
    }

    private static string StripPublisherSuffix(string title)
    {
        var cleaned = VgrSuffixRegex().Replace(title, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? title : WhiteSpaceRegex().Replace(cleaned, " ").Trim();
    }

    [GeneratedRegex(@"^(?<lang>SWA|FRA|FRE|FRN|FR|ENG|EN)(?=[\d\-_ ])", RegexOptions.IgnoreCase)]
    private static partial Regex LanguagePrefixRegex();

    [GeneratedRegex(@"\bVGR\b\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex VgrSuffixRegex();

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

    [GeneratedRegex(@"\b(?:en|de|fr|swa|frn|vgr|rb|pdf)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FileNoiseTokenRegex();

    [GeneratedRegex(@"\s*-\s*")]
    private static partial Regex HyphenSpacingRegex();

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"^(?<year>19\d{2}|20\d{2})-(?<month>0?[1-9]|1[0-2])-(?<day>0?[1-9]|[12]\d|3[01])-\d{4}-(?<loc>[A-Za-z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EwaldFrankMeetingRegex();

    [GeneratedRegex(@"^(?<season>Spring|Summer|Autumn|Fall|Winter)(?<year>19\d{2}|20\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex EwaldFrankSeasonalRegex();

    [GeneratedRegex(@"^(?<year>19\d{2}|20\d{2})(?<m1>0[1-9]|1[0-2])(?<m2>0[1-9]|1[0-2])(?!\d)")]
    private static partial Regex EwaldFrankCompactMonthRangeRegex();

    [GeneratedRegex(@"^(?<year>19\d{2}|20\d{2})[-_]?(?<month>0[1-9]|1[0-2])(?!\d)")]
    private static partial Regex EwaldFrankYearMonthRegex();
}
