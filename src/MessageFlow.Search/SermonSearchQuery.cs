namespace MessageFlow.Search;

/// <param name="ContentSourceId">
/// Single content source to search. Kept for existing callers; combined with
/// <paramref name="ContentSourceIds"/> into one scope.
/// </param>
/// <param name="ContentSourceIds">
/// Content sources this query may return results from. Used both for the library the operator
/// picked and for the set of sources that are visible in the church UI at all, so hidden or
/// retired sources are excluded by the query itself rather than filtered out of the page
/// afterwards. Null or empty means "no source restriction".
/// </param>
public sealed record SermonSearchQuery(
    int? AuthorId = null,
    int? ContentSourceId = null,
    string? SearchText = null,
    string? Title = null,
    string? SermonCode = null,
    int? Year = null,
    int? ParagraphNumber = null,
    string? Keyword = null,
    int MaxResults = 50,
    string? Language = null,
    IReadOnlyList<int>? ContentSourceIds = null);
