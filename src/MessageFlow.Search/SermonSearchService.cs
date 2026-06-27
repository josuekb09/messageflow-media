using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Search;

public sealed partial class SermonSearchService(MessageFlowDbContext dbContext) : ISermonSearchService
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var normalized = searchText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var limit = ClampLimit(maxResults);
        if (TryParseParagraphLookup(normalized, out var paragraphLookup))
        {
            return await SearchAsync(
                new SermonSearchQuery(
                    SearchText: paragraphLookup.SearchText,
                    ParagraphNumber: paragraphLookup.ParagraphNumber,
                    MaxResults: limit),
                cancellationToken);
        }

        var like = BuildContainsLike(normalized);
        var searchLike = BuildContainsLike(normalized.ToUpperInvariant());
        var ftsQuery = BuildFtsPrefixQuery(normalized);
        var parameters = new List<SearchParameter>
        {
            new("$like", like),
            new("$searchLike", searchLike),
            new("$limit", limit)
        };

        var numeric = int.TryParse(normalized, out var number);
        if (numeric)
        {
            parameters.Add(new("$number", number));
        }

        if (!string.IsNullOrWhiteSpace(ftsQuery))
        {
            parameters.Add(new("$fts", ftsQuery));

            try
            {
                return await ExecuteQueryAsync(
                    BuildSimpleSearchSql(useFts: true, hasNumber: numeric),
                    parameters,
                    cancellationToken);
            }
            catch (Exception ex) when (IsFtsFailure(ex))
            {
                // Fall back to LIKE if the SQLite build or database has no FTS5 table yet.
            }
        }

        return await ExecuteQueryAsync(
            BuildSimpleSearchSql(useFts: false, hasNumber: numeric),
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SermonSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var limit = ClampLimit(query.MaxResults);
        var parameters = new List<SearchParameter>
        {
            new("$limit", limit)
        };

        var clauses = new List<string>();

        if (query.AuthorId is not null)
        {
            clauses.Add("s.AuthorId = $authorId");
            parameters.Add(new("$authorId", query.AuthorId.Value));
        }

        var generalText = query.SearchText?.Trim();
        var paragraphNumber = query.ParagraphNumber;

        if (!string.IsNullOrWhiteSpace(generalText) &&
            paragraphNumber is null &&
            TryParseParagraphLookup(generalText, out var paragraphLookup))
        {
            generalText = paragraphLookup.SearchText;
            paragraphNumber = paragraphLookup.ParagraphNumber;
        }

        if (!string.IsNullOrWhiteSpace(generalText))
        {
            var generalClauses = new List<string>
            {
                "s.Title LIKE $generalLike ESCAPE '\\'",
                "s.SermonCode LIKE $generalLike ESCAPE '\\'",
                "p.SearchText LIKE $generalSearchLike ESCAPE '\\'"
            };

            parameters.Add(new("$generalLike", BuildContainsLike(generalText)));
            parameters.Add(new("$generalSearchLike", BuildContainsLike(generalText.ToUpperInvariant())));

            if (int.TryParse(generalText, out var generalNumber))
            {
                generalClauses.Add("s.Year = $generalNumber");
                generalClauses.Add("p.ParagraphNumber = $generalNumber");
                parameters.Add(new("$generalNumber", generalNumber));
            }

            clauses.Add($"({string.Join(" OR ", generalClauses)})");
        }

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            clauses.Add("s.Title LIKE $title ESCAPE '\\'");
            parameters.Add(new("$title", BuildContainsLike(query.Title)));
        }

        if (!string.IsNullOrWhiteSpace(query.SermonCode))
        {
            clauses.Add("s.SermonCode LIKE $sermonCode ESCAPE '\\'");
            parameters.Add(new("$sermonCode", BuildContainsLike(query.SermonCode)));
        }

        if (query.Year is not null)
        {
            clauses.Add("s.Year = $year");
            parameters.Add(new("$year", query.Year.Value));
        }

        if (paragraphNumber is not null)
        {
            clauses.Add("p.ParagraphNumber = $paragraphNumber");
            parameters.Add(new("$paragraphNumber", paragraphNumber.Value));
        }

        var keyword = query.Keyword?.Trim();
        var ftsQuery = string.IsNullOrWhiteSpace(keyword) ? null : BuildFtsPrefixQuery(keyword);

        if (!string.IsNullOrWhiteSpace(ftsQuery))
        {
            parameters.Add(new("$fts", ftsQuery));

            try
            {
                return await ExecuteQueryAsync(
                    BuildFilteredSearchSql(clauses, useFts: true),
                    parameters,
                    cancellationToken);
            }
            catch (Exception ex) when (IsFtsFailure(ex))
            {
                clauses.Add("p.SearchText LIKE $keywordLike ESCAPE '\\'");
                parameters.RemoveAll(parameter => parameter.Name == "$fts");
                parameters.Add(new("$keywordLike", BuildContainsLike(keyword!.ToUpperInvariant())));
            }
        }
        else if (!string.IsNullOrWhiteSpace(keyword))
        {
            clauses.Add("p.SearchText LIKE $keywordLike ESCAPE '\\'");
            parameters.Add(new("$keywordLike", BuildContainsLike(keyword.ToUpperInvariant())));
        }

        if (clauses.Count == 0)
        {
            return [];
        }

        return await ExecuteQueryAsync(
            BuildFilteredSearchSql(clauses, useFts: false),
            parameters,
            cancellationToken);
    }

    private static string BuildSimpleSearchSql(bool useFts, bool hasNumber)
    {
        var clauses = new List<string>
        {
            "s.Title LIKE $like ESCAPE '\\'",
            "s.SermonCode LIKE $like ESCAPE '\\'",
            "p.SearchText LIKE $searchLike ESCAPE '\\'"
        };

        if (hasNumber)
        {
            clauses.Add("s.Year = $number");
            clauses.Add("p.ParagraphNumber = $number");
        }

        if (useFts)
        {
            clauses.Add(
                """
                p.Id IN (
                    SELECT rowid
                    FROM SermonParagraphSearch
                    WHERE SermonParagraphSearch MATCH $fts
                )
                """);
        }

        return BuildBaseSelect(
            "SermonParagraphs p",
            string.Join($"{Environment.NewLine}        OR ", clauses),
            orderByFtsRank: false);
    }

    private static string BuildFilteredSearchSql(IReadOnlyCollection<string> clauses, bool useFts)
    {
        var from = useFts
            ? "SermonParagraphSearch JOIN SermonParagraphs p ON p.Id = SermonParagraphSearch.rowid"
            : "SermonParagraphs p";

        var ftsClause = useFts
            ? "SermonParagraphSearch MATCH $fts"
            : string.Empty;

        var allClauses = clauses.ToList();
        if (!string.IsNullOrWhiteSpace(ftsClause))
        {
            allClauses.Insert(0, ftsClause);
        }

        return BuildBaseSelect(
            from,
            string.Join($"{Environment.NewLine}        AND ", allClauses),
            orderByFtsRank: useFts);
    }

    private static string BuildBaseSelect(string from, string whereClause, bool orderByFtsRank)
    {
        var rankOrder = orderByFtsRank ? "bm25(SermonParagraphSearch)," : string.Empty;

        return $$"""
            SELECT
                s.Id AS SermonId,
                p.Id AS ParagraphId,
                s.Title AS SermonTitle,
                s.SermonCode,
                s.Year,
                p.ParagraphNumber,
                CASE
                    WHEN length(p.Text) <= 240 THEN p.Text
                    ELSE substr(p.Text, 1, 240) || '...'
                END AS ParagraphTextPreview,
                p.Text AS FullParagraphText,
                s.SourceFilePath,
                p.PageNumber
            FROM {{from}}
            JOIN Sermons s ON s.Id = p.SermonId
            WHERE {{whereClause}}
            ORDER BY {{rankOrder}} s.Year, s.Date, p.ParagraphNumber
            LIMIT $limit;
            """;
    }

    private async Task<IReadOnlyList<SearchResult>> ExecuteQueryAsync(
        string sql,
        IReadOnlyCollection<SearchParameter> parameters,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;

        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var parameter in parameters)
            {
                AddParameter(command, parameter.Name, parameter.Value);
            }

            var results = new List<SearchResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new SearchResult(
                    reader.GetInt32(reader.GetOrdinal("SermonId")),
                    reader.GetInt32(reader.GetOrdinal("ParagraphId")),
                    reader.GetString(reader.GetOrdinal("SermonTitle")),
                    reader.GetString(reader.GetOrdinal("SermonCode")),
                    reader.GetInt32(reader.GetOrdinal("Year")),
                    reader.GetInt32(reader.GetOrdinal("ParagraphNumber")),
                    reader.GetString(reader.GetOrdinal("ParagraphTextPreview")),
                    reader.GetString(reader.GetOrdinal("FullParagraphText")),
                    reader.GetString(reader.GetOrdinal("SourceFilePath")),
                    reader.IsDBNull(reader.GetOrdinal("PageNumber"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("PageNumber"))));
            }

            return results;
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static int ClampLimit(int maxResults)
    {
        return Math.Clamp(maxResults, 1, 250);
    }

    private static string BuildContainsLike(string value)
    {
        return $"%{EscapeLike(value.Trim())}%";
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    private static string? BuildFtsPrefixQuery(string value)
    {
        var tokens = FtsTokenRegex()
            .Matches(value)
            .Select(match => match.Value)
            .Where(token => token.Length > 0)
            .Take(12)
            .Select(token => $"{token}*")
            .ToList();

        return tokens.Count == 0 ? null : string.Join(' ', tokens);
    }

    private static bool IsFtsFailure(Exception ex)
    {
        return ex.Message.Contains("SermonParagraphSearch", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("fts5", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("MATCH", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex FtsTokenRegex();

    private static bool TryParseParagraphLookup(string value, out ParagraphLookup lookup)
    {
        lookup = default;

        var match = ParagraphLookupRegex().Match(value.Trim());
        if (!match.Success ||
            !int.TryParse(match.Groups["paragraphNumber"].Value, out var paragraphNumber) ||
            paragraphNumber <= 0)
        {
            return false;
        }

        var searchText = match.Groups["searchText"].Value.Trim();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return false;
        }

        lookup = new ParagraphLookup(searchText, paragraphNumber);
        return true;
    }

    [GeneratedRegex(@"^(?<searchText>.+?)\s+(?<paragraphNumber>\d{1,4})$")]
    private static partial Regex ParagraphLookupRegex();

    private sealed record SearchParameter(string Name, object Value);

    private readonly record struct ParagraphLookup(string SearchText, int ParagraphNumber);
}
