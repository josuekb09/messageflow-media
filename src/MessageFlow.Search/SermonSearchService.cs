using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Search;

public sealed partial class SermonSearchService(MessageFlowDbContext dbContext) : ISermonSearchService
{
    private const string FtsTableName = "SermonParagraphsFts";

    /// <summary>
    /// Timings from the most recently completed search.  This is diagnostic
    /// information only; it does not cache content or affect search results.
    /// </summary>
    public SermonSearchDiagnostics? LastDiagnostics { get; private set; }

    private string? queryLanguage;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        string? language = null)
    {
        queryLanguage = language;
        try
        {
        var diagnostics = new SermonSearchDiagnostics(searchText);
        var totalTimer = Stopwatch.StartNew();
        try
        {
        var rawSearchText = searchText.Trim();
        var normalized = SermonTextSearchPattern.NormalizeForSearch(rawSearchText);
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
                    MaxResults: limit,
                    Language: language),
                cancellationToken);
        }

        var like = BuildContainsLike(normalized);
        var searchLike = BuildContainsLike(normalized.ToUpperInvariant());
        var ftsQuery = BuildFtsPrefixQuery(rawSearchText);
        var dateIntent = SearchDateIntent.TryCreate(normalized);
        var parameters = new List<SearchParameter>
        {
            new("$like", like),
            new("$searchLike", searchLike),
            new("$exact", normalized),
            new("$prefix", BuildStartsWithLike(normalized)),
            new("$searchExact", normalized.ToUpperInvariant()),
            new("$searchPrefix", BuildStartsWithLike(normalized.ToUpperInvariant())),
            new("$limit", limit)
        };
        AddDateIntentParameters(parameters, dateIntent);

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
                var pattern = SermonTextSearchPattern.Create(rawSearchText);
                if (!pattern.IsExactPhrase && pattern.Terms.Count > 1)
                {
                    parameters.Add(new("$ftsPhrase", BuildFtsPhraseQuery(rawSearchText)!));
                    var phraseResults = await ExecuteQueryAsync(
                        BuildSimplePhraseFtsSearchSql(),
                        parameters,
                        cancellationToken,
                        diagnostics,
                        "phrase FTS");

                    // A full page of contiguous phrase matches is both more
                    // relevant than scattered-term matches and avoids ranking
                    // every broad FTS hit before the first page can be shown.
                    if (phraseResults.Count >= limit)
                    {
                        return phraseResults;
                    }

                    var allTermResults = await ExecuteQueryAsync(
                        BuildSimpleSearchSql(useFts: true, hasNumber: numeric),
                        parameters,
                        cancellationToken,
                        diagnostics,
                        "all-terms FTS");
                    return MergeRankedResults(phraseResults, allTermResults, limit, diagnostics);
                }

                var allTermParagraphResults = await ExecuteQueryAsync(
                    BuildSimpleSearchSql(useFts: true, hasNumber: numeric, rankFtsCandidates: !pattern.IsExactPhrase),
                    parameters,
                    cancellationToken,
                    diagnostics,
                    "all-terms FTS");

                // FTS indexes every searchable metadata field as well as the
                // paragraph text.  Avoid a second LIKE scan when FTS already
                // supplied the requested page; that scan was dominating common
                // broad searches such as "and so we".
                if (allTermParagraphResults.Count >= limit)
                {
                    return allTermParagraphResults;
                }

                var fullPhraseAndMetadataResults = await ExecuteQueryAsync(
                    BuildSimpleMetadataSearchSql(hasNumber: numeric),
                    parameters,
                    cancellationToken,
                    diagnostics,
                    "metadata fallback");
                return MergeRankedResults(fullPhraseAndMetadataResults, allTermParagraphResults, limit, diagnostics);
            }
            catch (Exception ex) when (IsFtsFailure(ex))
            {
                // Fall back to LIKE if the SQLite build or database has no FTS5 table yet.
            }
        }

        return await ExecuteQueryAsync(
            BuildSimpleSearchSql(useFts: false, hasNumber: numeric),
            parameters,
            cancellationToken,
            diagnostics,
            "LIKE fallback");
        }
        finally
        {
            totalTimer.Stop();
            diagnostics.SetTotal(totalTimer.Elapsed);
            LastDiagnostics = diagnostics;
        }
        }
        finally
        {
            queryLanguage = null;
        }
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SermonSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var previousLanguage = queryLanguage;
        queryLanguage = query.Language ?? queryLanguage;
        try
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

        if (query.ContentSourceId is not null)
        {
            clauses.Add("s.ContentSourceId = $contentSourceId");
            parameters.Add(new("$contentSourceId", query.ContentSourceId.Value));
        }

        var rawGeneralText = query.SearchText?.Trim();
        var generalText = SermonTextSearchPattern.NormalizeForSearch(rawGeneralText);
        var paragraphNumber = query.ParagraphNumber;
        var hasGeneralText = false;
        var hasTitle = false;
        var hasSermonCode = false;

        if (!string.IsNullOrWhiteSpace(generalText) &&
            paragraphNumber is null &&
            TryParseParagraphLookup(generalText, out var paragraphLookup))
        {
            generalText = paragraphLookup.SearchText;
            rawGeneralText = paragraphLookup.SearchText;
            paragraphNumber = paragraphLookup.ParagraphNumber;
        }

        List<string> generalClauses = [];
        if (!string.IsNullOrWhiteSpace(generalText))
        {
            hasGeneralText = true;
            var dateIntent = SearchDateIntent.TryCreate(generalText);

            generalClauses = BuildGeneralLikeClauses();
            parameters.Add(new("$generalLike", BuildContainsLike(generalText)));
            parameters.Add(new("$generalSearchLike", BuildContainsLike(generalText.ToUpperInvariant())));
            parameters.Add(new("$generalExact", generalText));
            parameters.Add(new("$generalPrefix", BuildStartsWithLike(generalText)));
            parameters.Add(new("$generalSearchExact", generalText.ToUpperInvariant()));
            parameters.Add(new("$generalSearchPrefix", BuildStartsWithLike(generalText.ToUpperInvariant())));
            AddDateIntentParameters(parameters, dateIntent);

            if (int.TryParse(generalText, out var generalNumber))
            {
                generalClauses.Add("s.Year = $generalNumber");
                generalClauses.Add("p.ParagraphNumber = $generalNumber");
                parameters.Add(new("$generalNumber", generalNumber));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            hasTitle = true;
            clauses.Add("s.Title LIKE $title ESCAPE '\\'");
            parameters.Add(new("$title", BuildContainsLike(query.Title)));
            parameters.Add(new("$titleExact", query.Title.Trim()));
            parameters.Add(new("$titlePrefix", BuildStartsWithLike(query.Title)));
        }

        if (!string.IsNullOrWhiteSpace(query.SermonCode))
        {
            hasSermonCode = true;
            clauses.Add("s.SermonCode LIKE $sermonCode ESCAPE '\\'");
            parameters.Add(new("$sermonCode", BuildContainsLike(query.SermonCode)));
            parameters.Add(new("$sermonCodeExact", query.SermonCode.Trim()));
            parameters.Add(new("$sermonCodePrefix", BuildStartsWithLike(query.SermonCode)));
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

        var rawKeyword = query.Keyword?.Trim();
        var keyword = SermonTextSearchPattern.NormalizeForSearch(rawKeyword);
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        var ftsQuery = BuildCombinedFtsQuery(rawGeneralText, rawKeyword);

        if (hasKeyword)
        {
            parameters.Add(new("$keywordExact", keyword!.ToUpperInvariant()));
            parameters.Add(new("$keywordPrefix", BuildStartsWithLike(keyword.ToUpperInvariant())));
            parameters.Add(new("$keywordLike", BuildContainsLike(keyword.ToUpperInvariant())));
        }

        if (!string.IsNullOrWhiteSpace(ftsQuery))
        {
            parameters.Add(new("$fts", ftsQuery));

            try
            {
                var phraseAndMetadataClauses = clauses.ToList();
                if (hasGeneralText)
                {
                    phraseAndMetadataClauses.Add($"({string.Join(" OR ", generalClauses)})");
                }

                if (hasKeyword)
                {
                    phraseAndMetadataClauses.Add("p.SearchText LIKE $keywordLike ESCAPE '\\'");
                }

                var phraseAndMetadataResults = await ExecuteQueryAsync(
                    BuildFilteredSearchSql(
                        phraseAndMetadataClauses,
                        useFts: false,
                        BuildFilteredRankingOrder(
                            hasGeneralText,
                            hasTitle,
                            hasSermonCode,
                            paragraphNumber is not null,
                            hasKeyword)),
                    parameters,
                    cancellationToken);
                var allTermResults = await ExecuteQueryAsync(
                    BuildFilteredSearchSql(
                        clauses,
                        useFts: true,
                        BuildFilteredRankingOrder(
                            hasGeneralText,
                            hasTitle,
                            hasSermonCode,
                            paragraphNumber is not null,
                            hasKeyword)),
                    parameters,
                    cancellationToken);
                return MergeRankedResults(phraseAndMetadataResults, allTermResults, limit);
            }
            catch (Exception ex) when (IsFtsFailure(ex))
            {
                if (hasGeneralText)
                {
                    clauses.Add($"({string.Join(" OR ", generalClauses)})");
                }

                parameters.RemoveAll(parameter => parameter.Name == "$fts");
                if (hasKeyword)
                {
                    clauses.Add("p.SearchText LIKE $keywordLike ESCAPE '\\'");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(ftsQuery) && hasGeneralText)
        {
            clauses.Add($"({string.Join(" OR ", generalClauses)})");
        }

        if (string.IsNullOrWhiteSpace(ftsQuery) && hasKeyword)
        {
            clauses.Add("p.SearchText LIKE $keywordLike ESCAPE '\\'");
        }

        if (clauses.Count == 0)
        {
            return [];
        }

        return await ExecuteQueryAsync(
            BuildFilteredSearchSql(
                clauses,
                useFts: false,
                BuildFilteredRankingOrder(
                    hasGeneralText,
                    hasTitle,
                    hasSermonCode,
                    paragraphNumber is not null,
                    hasKeyword)),
            parameters,
            cancellationToken);
        }
        finally
        {
            queryLanguage = previousLanguage;
        }
    }

    public async Task<IReadOnlyList<SearchResult>> BrowseSermonsAsync(
        int? authorId = null,
        int? contentSourceId = null,
        int? year = null,
        int maxResults = 2000,
        CancellationToken cancellationToken = default,
        string? language = null)
    {
        var previousLanguage = queryLanguage;
        queryLanguage = language ?? queryLanguage;
        try
        {
        var parameters = new List<SearchParameter>
        {
            new("$limit", ClampBrowseLimit(maxResults))
        };
        var clauses = new List<string>
        {
            """
            p.Id = (
                SELECT p2.Id
                FROM SermonParagraphs p2
                WHERE p2.SermonId = s.Id
                ORDER BY p2.ParagraphNumber, p2.Id
                LIMIT 1
            )
            """
        };

        if (authorId is not null)
        {
            clauses.Add("s.AuthorId = $authorId");
            parameters.Add(new("$authorId", authorId.Value));
        }

        if (contentSourceId is not null)
        {
            clauses.Add("s.ContentSourceId = $contentSourceId");
            parameters.Add(new("$contentSourceId", contentSourceId.Value));
        }

        if (year is not null)
        {
            clauses.Add("s.Year = $year");
            parameters.Add(new("$year", year.Value));
        }

        return await ExecuteQueryAsync(
            BuildBaseSelect(
                "SermonParagraphs p",
                string.Join($"{Environment.NewLine}        AND ", clauses),
                orderByFtsRank: false),
            parameters,
            cancellationToken);
        }
        finally
        {
            queryLanguage = previousLanguage;
        }
    }

    private string BuildSimpleSearchSql(bool useFts, bool hasNumber, bool rankFtsCandidates = true)
    {
        if (useFts)
        {
            return BuildSimpleFtsSearchSql(rankFtsCandidates);
        }

        var clauses = new List<string>();
        clauses.Add("s.Title LIKE $like ESCAPE '\\'");
        clauses.Add("s.SermonCode LIKE $like ESCAPE '\\'");
        clauses.Add("a.FullName LIKE $like ESCAPE '\\'");
        clauses.Add("a.DisplayName LIKE $like ESCAPE '\\'");
        clauses.Add("cs.DisplayName LIKE $like ESCAPE '\\'");
        clauses.Add("cs.SourceType LIKE $like ESCAPE '\\'");
        clauses.Add("p.SearchText LIKE $searchLike ESCAPE '\\'");

        if (hasNumber)
        {
            clauses.Add("s.Year = $number");
            clauses.Add("p.ParagraphNumber = $number");
        }

        return BuildBaseSelect(
            "SermonParagraphs p",
            string.Join($"{Environment.NewLine}        OR ", clauses),
            orderByFtsRank: false,
            rankingOrder: SimpleRankingOrder);
    }

    private string BuildSimpleFtsSearchSql(bool rankCandidates)
    {
        return BuildBaseSelect(
            BuildRankedFtsCandidateJoin("$fts", rankCandidates),
            "1 = 1",
            orderByFtsRank: rankCandidates,
            ftsRankExpression: rankCandidates ? "ftsMatches.FtsRank" : null);
    }

    private string BuildSimplePhraseFtsSearchSql()
    {
        return BuildBaseSelect(
            BuildRankedFtsCandidateJoin("$ftsPhrase", rankCandidates: false),
            "1 = 1",
            orderByFtsRank: false);
    }

    private string BuildRankedFtsCandidateJoin(string ftsParameter, bool rankCandidates = true)
    {
        // FTS5 can stop at LIMIT only while it owns the rank ordering.  Joining
        // and adding Sermon columns to that ordering forces SQLite to rank every
        // hit first (millions for common terms).  Materialize only the ranked
        // rowids needed for this page, then perform ordinary indexed joins.
        // Language must be applied inside this subquery.  Filtering after LIMIT
        // dropped French/Swahili hits whenever English filled the first page.
        var rankColumns = rankCandidates ? $", {FtsTableName}.rank AS FtsRank" : string.Empty;
        var rankOrder = rankCandidates ? $"ORDER BY {FtsTableName}.rank" : string.Empty;
        var languageJoin = string.Empty;
        var languagePredicate = string.Empty;
        if (!string.IsNullOrWhiteSpace(queryLanguage))
        {
            languageJoin =
                $"""

                        JOIN SermonParagraphs languageParagraphs ON languageParagraphs.Id = {FtsTableName}.rowid
                        JOIN Sermons languageSermons ON languageSermons.Id = languageParagraphs.SermonId
                """;
            languagePredicate = " AND languageSermons.Language = $language";
        }

        return
            $"""
            (
                SELECT {FtsTableName}.rowid{rankColumns}
                FROM {FtsTableName}{languageJoin}
                WHERE {FtsTableName} MATCH {ftsParameter}{languagePredicate}
                {rankOrder}
                LIMIT $limit
            ) ftsMatches
            JOIN SermonParagraphs p ON p.Id = ftsMatches.rowid
            """;
    }

    private static string BuildSimpleMetadataSearchSql(bool hasNumber)
    {
        var metadataClauses = new List<string>
        {
            "s.Title LIKE $like ESCAPE '\\'",
            "s.SermonCode LIKE $like ESCAPE '\\'",
            "a.FullName LIKE $like ESCAPE '\\'",
            "a.DisplayName LIKE $like ESCAPE '\\'",
            "cs.DisplayName LIKE $like ESCAPE '\\'",
            "cs.SourceType LIKE $like ESCAPE '\\'"
        };

        if (hasNumber)
        {
            metadataClauses.Add("s.Year = $number");
        }

        return $$"""
            SELECT
                s.Id AS SermonId,
                p.Id AS ParagraphId,
                s.Title AS SermonTitle,
                s.SermonCode,
                s.Year,
                COALESCE(a.DisplayName, a.FullName, '') AS AuthorDisplayName,
                COALESCE(cs.DisplayName, '') AS SourceDisplayName,
                COALESCE(cs.SourceType, '') AS SourceType,
                p.ParagraphNumber,
                CASE
                    WHEN length(p.Text) <= 240 THEN p.Text
                    ELSE substr(p.Text, 1, 240) || '...'
                END AS ParagraphTextPreview,
                p.Text AS FullParagraphText,
                s.SourceFilePath,
                p.PageNumber
            FROM Sermons s
            JOIN SermonParagraphs p ON p.Id = (
                SELECT p2.Id
                FROM SermonParagraphs p2
                WHERE p2.SermonId = s.Id
                ORDER BY p2.ParagraphNumber, p2.Id
                LIMIT 1
            )
            LEFT JOIN Authors a ON a.Id = s.AuthorId
            LEFT JOIN ContentSources cs ON cs.Id = s.ContentSourceId
            WHERE
                {{string.Join($"{Environment.NewLine}                OR ", metadataClauses)}}
            ORDER BY {{SimpleRankingOrder}}, s.Year, s.Date, p.ParagraphNumber
            LIMIT $limit;
            """;
    }

    private static string BuildFilteredSearchSql(
        IReadOnlyCollection<string> clauses,
        bool useFts,
        string rankingOrder)
    {
        var from = useFts
            ? $"{FtsTableName} JOIN SermonParagraphs p ON p.Id = {FtsTableName}.rowid"
            : "SermonParagraphs p";

        var ftsClause = useFts
            ? $"{FtsTableName} MATCH $fts"
            : string.Empty;

        var allClauses = clauses.ToList();
        if (!string.IsNullOrWhiteSpace(ftsClause))
        {
            allClauses.Insert(0, ftsClause);
        }

        return BuildBaseSelect(
            from,
            string.Join($"{Environment.NewLine}        AND ", allClauses),
            orderByFtsRank: useFts,
            rankingOrder: rankingOrder);
    }

    private static string BuildBaseSelect(
        string from,
        string whereClause,
        bool orderByFtsRank,
        string rankingOrder = "",
        string? ftsRankExpression = null)
    {
        var orderParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(rankingOrder))
        {
            orderParts.Add(rankingOrder);
        }

        if (orderByFtsRank)
        {
            // FTS5's hidden rank column lets SQLite stop early for LIMIT and is
            // materially faster than evaluating bm25() through an outer CASE sort.
            orderParts.Add(ftsRankExpression ?? $"{FtsTableName}.rank");
        }

        orderParts.Add("s.Year");
        orderParts.Add("s.Date");
        orderParts.Add("p.ParagraphNumber");
        var orderBy = string.Join(", ", orderParts);

        return $$"""
            SELECT
                s.Id AS SermonId,
                p.Id AS ParagraphId,
                s.Title AS SermonTitle,
                s.SermonCode,
                s.Year,
                COALESCE(a.DisplayName, a.FullName, '') AS AuthorDisplayName,
                COALESCE(cs.DisplayName, '') AS SourceDisplayName,
                COALESCE(cs.SourceType, '') AS SourceType,
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
            LEFT JOIN Authors a ON a.Id = s.AuthorId
            LEFT JOIN ContentSources cs ON cs.Id = s.ContentSourceId
            WHERE {{whereClause}}
            ORDER BY {{orderBy}}
            LIMIT $limit;
            """;
    }

    private async Task<IReadOnlyList<SearchResult>> ExecuteQueryAsync(
        string sql,
        IReadOnlyCollection<SearchParameter> parameters,
        CancellationToken cancellationToken,
        SermonSearchDiagnostics? diagnostics = null,
        string? diagnosticPhase = null)
    {
        var sqlToRun = sql;
        var parametersToUse = parameters;
        if (!string.IsNullOrWhiteSpace(queryLanguage))
        {
            sqlToRun = InsertLanguageFilter(sql);
            parametersToUse = parameters
                .Append(new SearchParameter("$language", queryLanguage))
                .ToList();
        }

        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;

        if (closeConnection)
        {
            var connectionTimer = Stopwatch.StartNew();
            await connection.OpenAsync(cancellationToken);
            connectionTimer.Stop();
            diagnostics?.RecordConnection(connectionTimer.Elapsed);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sqlToRun;

            var prepareTimer = Stopwatch.StartNew();
            foreach (var parameter in parametersToUse)
            {
                AddParameter(command, parameter.Name, parameter.Value);
            }
            prepareTimer.Stop();
            diagnostics?.RecordPreparation(diagnosticPhase, prepareTimer.Elapsed);

            var results = new List<SearchResult>();
            var executionTimer = Stopwatch.StartNew();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            executionTimer.Stop();
            diagnostics?.RecordExecution(diagnosticPhase, executionTimer.Elapsed);

            var materializationTimer = Stopwatch.StartNew();
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
                        : reader.GetInt32(reader.GetOrdinal("PageNumber")),
                    reader.GetString(reader.GetOrdinal("AuthorDisplayName")),
                    reader.GetString(reader.GetOrdinal("SourceDisplayName")),
                    reader.GetString(reader.GetOrdinal("SourceType"))));
            }
            materializationTimer.Stop();
            diagnostics?.RecordMaterialization(diagnosticPhase, materializationTimer.Elapsed);

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

    private static IReadOnlyList<SearchResult> MergeRankedResults(
        IEnumerable<SearchResult> phraseAndMetadataResults,
        IEnumerable<SearchResult> allTermResults,
        int limit,
        SermonSearchDiagnostics? diagnostics = null)
    {
        var timer = Stopwatch.StartNew();
        var results = phraseAndMetadataResults
            .Concat(allTermResults)
            .DistinctBy(result => result.ParagraphId)
            .Take(limit)
            .ToList();
        timer.Stop();
        diagnostics?.RecordManagedMerge(timer.Elapsed);
        return results;
    }

    private static void AddDateIntentParameters(
        ICollection<SearchParameter> parameters,
        SearchDateIntent? dateIntent)
    {
        if (dateIntent is null)
        {
            parameters.Add(new("$hasDateIntent", 0));
            parameters.Add(new("$dateExactTitle", string.Empty));
            parameters.Add(new("$dateTitleLike", string.Empty));
            parameters.Add(new("$dateMonthLike", string.Empty));
            parameters.Add(new("$dateYearMin", 0));
            parameters.Add(new("$dateYearMax", 0));
            return;
        }

        parameters.Add(new("$hasDateIntent", 1));
        parameters.Add(new("$dateExactTitle", dateIntent.ExactCircularLetterTitle));
        parameters.Add(new("$dateTitleLike", BuildContainsLike(dateIntent.DateText)));
        parameters.Add(new("$dateMonthLike", BuildContainsLike(dateIntent.MonthName)));
        parameters.Add(new("$dateYearMin", dateIntent.YearMin));
        parameters.Add(new("$dateYearMax", dateIntent.YearMax));
    }

    private static int ClampLimit(int maxResults)
    {
        return Math.Clamp(maxResults, 1, 250);
    }

    private static int ClampBrowseLimit(int maxResults)
    {
        return Math.Clamp(maxResults, 1, 2000);
    }

    private static string BuildContainsLike(string value)
    {
        return $"%{EscapeLike(value.Trim())}%";
    }

    private static string BuildStartsWithLike(string value)
    {
        return $"{EscapeLike(value.Trim())}%";
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    private static List<string> BuildGeneralLikeClauses()
    {
        return
        [
            "s.Title LIKE $generalLike ESCAPE '\\'",
            "s.SermonCode LIKE $generalLike ESCAPE '\\'",
            "a.FullName LIKE $generalLike ESCAPE '\\'",
            "a.DisplayName LIKE $generalLike ESCAPE '\\'",
            "cs.DisplayName LIKE $generalLike ESCAPE '\\'",
            "cs.SourceType LIKE $generalLike ESCAPE '\\'",
            "p.SearchText LIKE $generalSearchLike ESCAPE '\\'"
        ];
    }

    private static string? BuildFtsPrefixQuery(string value)
    {
        var exactPhrase = SermonTextSearchPattern.TryExtractExactPhrase(value, out var exactPhraseText);
        var normalized = SermonTextSearchPattern.NormalizeForSearch(exactPhrase ? exactPhraseText : value);
        var tokens = SermonTextSearchPattern.TokenizeNormalized(normalized)
            .ToList();

        if (tokens.Count == 0)
        {
            return null;
        }

        return exactPhrase
            ? $"\"{string.Join(' ', tokens).Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : string.Join(' ', tokens.Select(QuoteFtsToken));
    }

    private static string QuoteFtsToken(string token)
    {
        return $"\"{token.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string? BuildFtsPhraseQuery(string value)
    {
        var normalized = SermonTextSearchPattern.NormalizeForSearch(value);
        var tokens = SermonTextSearchPattern.TokenizeNormalized(normalized);
        return tokens.Count < 2
            ? null
            : $"\"{string.Join(' ', tokens).Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string? BuildCombinedFtsQuery(params string?[] values)
    {
        var parts = values
            .Select(value => string.IsNullOrWhiteSpace(value) ? null : BuildFtsPrefixQuery(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return parts.Count == 0 ? null : string.Join(' ', parts);
    }

    private static bool IsFtsFailure(Exception ex)
    {
        return ex.Message.Contains("SermonParagraphSearch", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("SermonParagraphsFts", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("fts5", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("MATCH", StringComparison.OrdinalIgnoreCase);
    }

    private const string SimpleRankingOrder =
        """
        CASE
            WHEN s.SermonCode COLLATE NOCASE = $exact THEN 0
            WHEN s.Title COLLATE NOCASE = $exact THEN 1
            WHEN $hasDateIntent = 1 AND s.Title COLLATE NOCASE = $dateExactTitle THEN 2
            WHEN $hasDateIntent = 1 AND s.Title LIKE $dateTitleLike ESCAPE '\' AND s.Year BETWEEN $dateYearMin AND $dateYearMax THEN 3
            WHEN $hasDateIntent = 1 AND s.Title LIKE $dateMonthLike ESCAPE '\' AND s.Year BETWEEN $dateYearMin AND $dateYearMax THEN 4
            WHEN s.SermonCode LIKE $prefix ESCAPE '\' THEN 5
            WHEN s.Title LIKE $prefix ESCAPE '\' THEN 6
            WHEN s.SermonCode LIKE $like ESCAPE '\' THEN 7
            WHEN s.Title LIKE $like ESCAPE '\' THEN 8
            WHEN a.DisplayName LIKE $prefix ESCAPE '\' THEN 9
            WHEN a.FullName LIKE $prefix ESCAPE '\' THEN 10
            WHEN cs.DisplayName LIKE $prefix ESCAPE '\' THEN 11
            WHEN p.SearchText = $searchExact THEN 12
            WHEN p.SearchText LIKE $searchPrefix ESCAPE '\' THEN 13
            ELSE 14
        END
        """;

    private static string BuildFilteredRankingOrder(
        bool hasGeneralText,
        bool hasTitle,
        bool hasSermonCode,
        bool hasParagraphNumber,
        bool hasKeyword)
    {
        var cases = new List<string>();
        var rank = 0;

        if (hasParagraphNumber)
        {
            if (hasSermonCode)
            {
                cases.Add($"WHEN s.SermonCode COLLATE NOCASE = $sermonCodeExact AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
            }

            if (hasGeneralText)
            {
                cases.Add($"WHEN s.SermonCode COLLATE NOCASE = $generalExact AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
                cases.Add($"WHEN s.Title COLLATE NOCASE = $generalExact AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
                cases.Add($"WHEN s.Title LIKE $generalPrefix ESCAPE '\\' AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
                cases.Add($"WHEN s.Title LIKE $generalLike ESCAPE '\\' AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
            }

            if (hasTitle)
            {
                cases.Add($"WHEN s.Title COLLATE NOCASE = $titleExact AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
                cases.Add($"WHEN s.Title LIKE $titlePrefix ESCAPE '\\' AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
                cases.Add($"WHEN s.Title LIKE $title ESCAPE '\\' AND p.ParagraphNumber = $paragraphNumber THEN {rank++}");
            }
        }

        if (hasSermonCode)
        {
            cases.Add($"WHEN s.SermonCode COLLATE NOCASE = $sermonCodeExact THEN {rank++}");
            cases.Add($"WHEN s.SermonCode LIKE $sermonCodePrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN s.SermonCode LIKE $sermonCode ESCAPE '\\' THEN {rank++}");
        }

        if (hasGeneralText)
        {
            cases.Add($"WHEN $hasDateIntent = 1 AND s.Title COLLATE NOCASE = $dateExactTitle THEN {rank++}");
            cases.Add($"WHEN $hasDateIntent = 1 AND s.Title LIKE $dateTitleLike ESCAPE '\\' AND s.Year BETWEEN $dateYearMin AND $dateYearMax THEN {rank++}");
            cases.Add($"WHEN $hasDateIntent = 1 AND s.Title LIKE $dateMonthLike ESCAPE '\\' AND s.Year BETWEEN $dateYearMin AND $dateYearMax THEN {rank++}");
            cases.Add($"WHEN s.SermonCode COLLATE NOCASE = $generalExact THEN {rank++}");
            cases.Add($"WHEN s.SermonCode LIKE $generalPrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN s.SermonCode LIKE $generalLike ESCAPE '\\' THEN {rank++}");
        }

        if (hasTitle)
        {
            cases.Add($"WHEN s.Title COLLATE NOCASE = $titleExact THEN {rank++}");
            cases.Add($"WHEN s.Title LIKE $titlePrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN s.Title LIKE $title ESCAPE '\\' THEN {rank++}");
        }

        if (hasGeneralText)
        {
            cases.Add($"WHEN s.Title COLLATE NOCASE = $generalExact THEN {rank++}");
            cases.Add($"WHEN s.Title LIKE $generalPrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN s.Title LIKE $generalLike ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN a.DisplayName LIKE $generalPrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN a.FullName LIKE $generalPrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN cs.DisplayName LIKE $generalPrefix ESCAPE '\\' THEN {rank++}");
            cases.Add($"WHEN p.SearchText = $generalSearchExact THEN {rank++}");
            cases.Add($"WHEN p.SearchText LIKE $generalSearchPrefix ESCAPE '\\' THEN {rank++}");
        }

        if (hasKeyword)
        {
            cases.Add($"WHEN p.SearchText = $keywordExact THEN {rank++}");
            cases.Add($"WHEN p.SearchText LIKE $keywordPrefix ESCAPE '\\' THEN {rank++}");
        }

        if (cases.Count == 0)
        {
            return string.Empty;
        }

        return $"""
            CASE
                {string.Join($"{Environment.NewLine}                ", cases)}
                ELSE {rank}
            END
            """;
    }

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

    [GeneratedRegex(@"\b(?<month>January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)\s+(?<year>\d{2,4})\b", RegexOptions.IgnoreCase)]
    private static partial Regex MonthYearPrefixRegex();

    private static string InsertLanguageFilter(string sql)
    {
        // Wrap only the outermost WHERE. Nested subquery WHERE/ORDER BY must
        // stay untouched or SQLite reports: near "ORDER": syntax error.
        var whereIndex = IndexOfKeywordAtDepthZero(sql, "WHERE");
        var orderIndex = whereIndex < 0
            ? -1
            : IndexOfKeywordAtDepthZero(sql, "ORDER BY", whereIndex + "WHERE".Length);
        if (whereIndex < 0 || orderIndex < 0)
        {
            throw new InvalidOperationException("Sermon search SQL could not receive a language filter.");
        }

        var bodyStart = whereIndex + "WHERE".Length;
        var whereBody = sql[bodyStart..orderIndex];
        return sql[..bodyStart] +
               $" s.Language = $language AND ({whereBody.Trim()}) " +
               sql[orderIndex..];
    }

    private static int IndexOfKeywordAtDepthZero(string sql, string keyword, int start = 0)
    {
        var depth = 0;
        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            if (c == '(')
            {
                depth++;
                continue;
            }

            if (c == ')')
            {
                if (depth > 0)
                {
                    depth--;
                }

                continue;
            }

            if (depth != 0 || i < start || i + keyword.Length > sql.Length)
            {
                continue;
            }

            if (!sql.AsSpan(i, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var beforeOk = i == 0 || !char.IsLetterOrDigit(sql[i - 1]);
            var afterIndex = i + keyword.Length;
            var afterOk = afterIndex >= sql.Length || !char.IsLetterOrDigit(sql[afterIndex]);
            if (beforeOk && afterOk)
            {
                return i;
            }
        }

        return -1;
    }

    private sealed record SearchParameter(string Name, object Value);

    private readonly record struct ParagraphLookup(string SearchText, int ParagraphNumber);

    private sealed record SearchDateIntent(
        string MonthName,
        string YearPrefix,
        int YearMin,
        int YearMax)
    {
        public string DateText => $"{MonthName} {YearPrefix}";

        public string ExactCircularLetterTitle =>
            YearPrefix.Length == 4
                ? $"Circular Letter - {MonthName} {YearPrefix}"
                : string.Empty;

        public static SearchDateIntent? TryCreate(string value)
        {
            var match = SermonSearchService.MonthYearPrefixRegex().Match(value);
            if (!match.Success)
            {
                return null;
            }

            var yearPrefix = match.Groups["year"].Value;
            if (!TryNormalizeMonth(match.Groups["month"].Value, out var monthName) ||
                !TryCreateYearRange(yearPrefix, out var yearMin, out var yearMax))
            {
                return null;
            }

            return new SearchDateIntent(monthName, yearPrefix, yearMin, yearMax);
        }

        private static bool TryNormalizeMonth(string value, out string monthName)
        {
            for (var index = 1; index <= 12; index++)
            {
                var fullName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(index);
                var abbreviatedName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(index);
                if (value.Equals(fullName, StringComparison.OrdinalIgnoreCase) ||
                    value.Equals(abbreviatedName, StringComparison.OrdinalIgnoreCase) ||
                    (index == 9 && value.Equals("Sept", StringComparison.OrdinalIgnoreCase)))
                {
                    monthName = fullName;
                    return true;
                }
            }

            monthName = string.Empty;
            return false;
        }

        private static bool TryCreateYearRange(
            string yearPrefix,
            out int yearMin,
            out int yearMax)
        {
            yearMin = 0;
            yearMax = 0;

            if (!int.TryParse(yearPrefix, out var prefix))
            {
                return false;
            }

            switch (yearPrefix.Length)
            {
                case 4:
                    yearMin = prefix;
                    yearMax = prefix;
                    return true;
                case 3:
                    yearMin = prefix * 10;
                    yearMax = yearMin + 9;
                    return true;
                case 2 when yearPrefix == "20":
                    yearMin = 2000;
                    yearMax = 2029;
                    return true;
                case 2 when yearPrefix == "19":
                    yearMin = 1900;
                    yearMax = 1999;
                    return true;
                default:
                    return false;
            }
        }
    }
}

public sealed class SermonSearchDiagnostics(string searchText)
{
    private readonly List<string> phases = [];
    private TimeSpan connection;
    private TimeSpan managedMerge;
    private TimeSpan total;

    public string SearchText { get; } = searchText;

    public void RecordConnection(TimeSpan elapsed) => connection += elapsed;
    public void RecordPreparation(string? phase, TimeSpan elapsed) => Record(phase, "prepare", elapsed);
    public void RecordExecution(string? phase, TimeSpan elapsed) => Record(phase, "execute", elapsed);
    public void RecordMaterialization(string? phase, TimeSpan elapsed) => Record(phase, "materialize", elapsed);

    public void RecordManagedMerge(TimeSpan elapsed) => managedMerge += elapsed;

    public void SetTotal(TimeSpan elapsed) => total = elapsed;

    public override string ToString()
    {
        var timing = string.Join(", ", phases);
        return $"connection={connection.TotalMilliseconds:0} ms; {timing}; managed merge={managedMerge.TotalMilliseconds:0} ms; total={total.TotalMilliseconds:0} ms";
    }

    private void Record(string? phase, string operation, TimeSpan elapsed)
    {
        phases.Add($"{phase ?? "query"} {operation}={elapsed.TotalMilliseconds:0} ms");
    }
}
