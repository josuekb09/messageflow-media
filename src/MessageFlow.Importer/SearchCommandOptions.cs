using MessageFlow.Search;

namespace MessageFlow.Importer;

public sealed record SearchCommandOptions(
    string SearchText,
    SermonSearchQuery Query,
    bool IsStructured,
    bool ShowHelp = false,
    bool IsValid = true,
    string ErrorMessage = "")
{
    public static string HelpText =>
        """
        Usage:
          dotnet run --project src\MessageFlow.Importer -- search "faith"
          dotnet run --project src\MessageFlow.Importer -- search --title "Seed"
          dotnet run --project src\MessageFlow.Importer -- search --code 65-0429
          dotnet run --project src\MessageFlow.Importer -- search --year 1965 --keyword "rapture"
          dotnet run --project src\MessageFlow.Importer -- search --paragraph 12 --limit 10

        Options:
          --title <text>       Search sermon titles.
          --code <text>        Search sermon codes.
          --year <number>      Search sermon year.
          --paragraph <number> Search paragraph number.
          --keyword <text>     Search inside paragraph text.
          --limit <number>     Maximum results to show. Default: 20.
          --help               Show this help text.
        """;

    public static SearchCommandOptions Parse(string[] args)
    {
        var textParts = new List<string>();
        string? title = null;
        string? sermonCode = null;
        string? keyword = null;
        int? year = null;
        int? paragraphNumber = null;
        var limit = 20;
        var isStructured = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                return Empty with { ShowHelp = true };
            }

            if (string.Equals(arg, "--title", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref i, "--title", out var value, out var error))
                {
                    return Invalid(error);
                }

                title = value;
                isStructured = true;
                continue;
            }

            if (string.Equals(arg, "--code", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref i, "--code", out var value, out var error))
                {
                    return Invalid(error);
                }

                sermonCode = value;
                isStructured = true;
                continue;
            }

            if (string.Equals(arg, "--keyword", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref i, "--keyword", out var value, out var error))
                {
                    return Invalid(error);
                }

                keyword = value;
                isStructured = true;
                continue;
            }

            if (string.Equals(arg, "--year", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadInt(args, ref i, "--year", out var value, out var error))
                {
                    return Invalid(error);
                }

                year = value;
                isStructured = true;
                continue;
            }

            if (string.Equals(arg, "--paragraph", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadInt(args, ref i, "--paragraph", out var value, out var error))
                {
                    return Invalid(error);
                }

                paragraphNumber = value;
                isStructured = true;
                continue;
            }

            if (string.Equals(arg, "--limit", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadInt(args, ref i, "--limit", out var value, out var error))
                {
                    return Invalid(error);
                }

                limit = value;
                continue;
            }

            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                return Invalid($"Unknown search option: {arg}");
            }

            textParts.Add(arg);
        }

        var searchText = string.Join(' ', textParts).Trim();
        if (!isStructured && string.IsNullOrWhiteSpace(searchText))
        {
            return Invalid("Enter search text or at least one search option.");
        }

        if (isStructured && string.IsNullOrWhiteSpace(keyword) && !string.IsNullOrWhiteSpace(searchText))
        {
            keyword = searchText;
        }

        return new SearchCommandOptions(
            searchText,
            new SermonSearchQuery(
                Title: title,
                SermonCode: sermonCode,
                Year: year,
                ParagraphNumber: paragraphNumber,
                Keyword: keyword,
                MaxResults: limit),
            isStructured);
    }

    private static SearchCommandOptions Empty => new(
        string.Empty,
        new SermonSearchQuery(),
        IsStructured: false);

    private static SearchCommandOptions Invalid(string error)
    {
        return Empty with { IsValid = false, ErrorMessage = error };
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string option,
        out string value,
        out string error)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            value = string.Empty;
            error = $"Missing value for {option}.";
            return false;
        }

        index++;
        value = args[index];
        error = string.Empty;
        return true;
    }

    private static bool TryReadInt(
        string[] args,
        ref int index,
        string option,
        out int value,
        out string error)
    {
        if (!TryReadValue(args, ref index, option, out var text, out error))
        {
            value = 0;
            return false;
        }

        if (!int.TryParse(text, out value))
        {
            error = $"{option} must be a whole number.";
            return false;
        }

        return true;
    }
}
