using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MessageFlow.Importer;

internal static partial class EwaldFrankMetadataCatalog
{
    private const string AppliedCsvPath = @"D:\Ewald Frank\_Download Tracker\ewald_frank_pdf_metadata_applied_clean.csv";
    private static readonly Lazy<CatalogData> Data = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool TryFind(string filePath, out EwaldFrankCatalogMetadata metadata)
    {
        var fullPath = NormalizePath(filePath);
        if (Data.Value.ByAppliedFile.TryGetValue(fullPath, out metadata!))
        {
            return true;
        }

        var fileName = Path.GetFileName(filePath);
        return Data.Value.ByFileName.TryGetValue(fileName, out metadata!);
    }

    private static CatalogData Load()
    {
        if (!File.Exists(AppliedCsvPath))
        {
            return CatalogData.Empty;
        }

        var byAppliedFile = new Dictionary<string, EwaldFrankCatalogMetadata>(StringComparer.OrdinalIgnoreCase);
        var byFileName = new Dictionary<string, EwaldFrankCatalogMetadata>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(AppliedCsvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return CatalogData.Empty;
        }

        var headers = ParseCsvLine(headerLine);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            var row = CreateRow(headers, fields);
            var metadata = EwaldFrankCatalogMetadata.FromRow(row);
            if (metadata is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(metadata.AppliedFile))
            {
                byAppliedFile[NormalizePath(metadata.AppliedFile)] = metadata;
            }

            if (!string.IsNullOrWhiteSpace(metadata.SuggestedDestination) &&
                !string.IsNullOrWhiteSpace(metadata.SuggestedFileName))
            {
                var suggestedPath = Path.Combine(metadata.SuggestedDestination, metadata.SuggestedFileName);
                byAppliedFile[NormalizePath(suggestedPath)] = metadata;
            }

            AddFileNameLookup(byFileName, metadata.AppliedFile, metadata);
            AddFileNameLookup(byFileName, metadata.SuggestedFileName, metadata);
            AddFileNameLookup(byFileName, metadata.OriginalFile, metadata);
        }

        return new CatalogData(byAppliedFile, byFileName);
    }

    private static void AddFileNameLookup(
        IDictionary<string, EwaldFrankCatalogMetadata> byFileName,
        string? value,
        EwaldFrankCatalogMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        byFileName[Path.GetFileName(value)] = metadata;
    }

    private static Dictionary<string, string> CreateRow(
        IReadOnlyList<string> headers,
        IReadOnlyList<string> fields)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            row[headers[index]] = index < fields.Count ? fields[index] : string.Empty;
        }

        return row;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    builder.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(character);
        }

        fields.Add(builder.ToString());
        return fields;
    }

    private static string NormalizePath(string value)
    {
        return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed record CatalogData(
        Dictionary<string, EwaldFrankCatalogMetadata> ByAppliedFile,
        Dictionary<string, EwaldFrankCatalogMetadata> ByFileName)
    {
        public static CatalogData Empty { get; } = new(
            new Dictionary<string, EwaldFrankCatalogMetadata>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, EwaldFrankCatalogMetadata>(StringComparer.OrdinalIgnoreCase));
    }
}

internal sealed partial record EwaldFrankCatalogMetadata(
    string OriginalFile,
    string Category,
    string Language,
    string DateDisplay,
    string OfficialDisplayTitle,
    string SuggestedFileName,
    string SuggestedDestination,
    string AppliedFile)
{
    public bool IsCircularLetter =>
        string.Equals(Category, "Circular Letter", StringComparison.OrdinalIgnoreCase) ||
        OfficialDisplayTitle.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase);

    public static EwaldFrankCatalogMetadata? FromRow(IReadOnlyDictionary<string, string> row)
    {
        var title = Get(row, "official_display_title");
        var appliedFile = Get(row, "applied_file");
        var suggestedFileName = Get(row, "suggested_filename");

        if (string.IsNullOrWhiteSpace(title) ||
            (string.IsNullOrWhiteSpace(appliedFile) && string.IsNullOrWhiteSpace(suggestedFileName)))
        {
            return null;
        }

        return new EwaldFrankCatalogMetadata(
            Get(row, "original_file"),
            Get(row, "detected_category"),
            Get(row, "detected_language"),
            Get(row, "detected_date"),
            title,
            suggestedFileName,
            Get(row, "suggested_destination"),
            appliedFile);
    }

    public DateTime? TryCreateDate()
    {
        if (DateTime.TryParseExact(
                DateDisplay,
                "MMMM yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthDate))
        {
            return new DateTime(monthDate.Year, monthDate.Month, 1);
        }

        var monthRangeMatch = MonthRangeDateRegex().Match(DateDisplay);
        if (monthRangeMatch.Success &&
            TryParseMonth(monthRangeMatch.Groups["month"].Value, out var rangeMonth) &&
            int.TryParse(monthRangeMatch.Groups["year"].Value, CultureInfo.InvariantCulture, out var rangeYear))
        {
            return new DateTime(rangeYear, rangeMonth, 1);
        }

        var seasonalMatch = SeasonalDateRegex().Match(DateDisplay);
        if (seasonalMatch.Success &&
            int.TryParse(seasonalMatch.Groups["year"].Value, CultureInfo.InvariantCulture, out var seasonalYear))
        {
            return new DateTime(seasonalYear, GetSeasonalMonth(seasonalMatch.Groups["season"].Value), 1);
        }

        return null;
    }

    public int? TryFindYear()
    {
        var date = TryCreateDate();
        if (date is not null)
        {
            return date.Value.Year;
        }

        var match = YearRegex().Match($"{DateDisplay} {OfficialDisplayTitle} {SuggestedFileName} {AppliedFile}");
        return match.Success && int.TryParse(match.Value, CultureInfo.InvariantCulture, out var year)
            ? year
            : null;
    }

    private static bool TryParseMonth(string value, out int month)
    {
        for (var index = 1; index <= 12; index++)
        {
            var fullName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(index);
            if (string.Equals(value, fullName, StringComparison.OrdinalIgnoreCase))
            {
                month = index;
                return true;
            }
        }

        month = 0;
        return false;
    }

    private static int GetSeasonalMonth(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "SPRING" => 3,
            "SUMMER" => 6,
            "AUTUMN" or "FALL" => 9,
            "WINTER" or "YEAR END" or "YEAR-END" => 12,
            _ => 1
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
    }

    [GeneratedRegex(@"^(?<month>January|February|March|April|May|June|July|August|September|October|November|December)-[A-Za-z]+\s+(?<year>19\d{2}|20\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex MonthRangeDateRegex();

    [GeneratedRegex(@"^(?<season>Spring|Summer|Autumn|Fall|Winter|Year End|Year-End)\s+(?<year>19\d{2}|20\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonalDateRegex();

    [GeneratedRegex(@"(?<!\d)(?:19|20)\d{2}(?!\d)")]
    private static partial Regex YearRegex();
}
