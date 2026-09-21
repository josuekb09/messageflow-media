namespace MessageFlow.Search;

public interface ISermonSearchService
{
    /// <param name="contentSourceIds">
    /// Content sources results may come from. Applied inside the ranked full-text candidate set,
    /// so a small library is never starved by a large one. Null or empty means no restriction.
    /// </param>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default,
        string? language = null,
        IReadOnlyList<int>? contentSourceIds = null);

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SermonSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> BrowseSermonsAsync(
        int? authorId = null,
        int? contentSourceId = null,
        int? year = null,
        int maxResults = 2000,
        CancellationToken cancellationToken = default,
        string? language = null,
        IReadOnlyList<int>? contentSourceIds = null);
}
