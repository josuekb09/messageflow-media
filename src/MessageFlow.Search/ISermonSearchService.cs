namespace MessageFlow.Search;

public interface ISermonSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SermonSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> BrowseSermonsAsync(
        int? authorId = null,
        int? contentSourceId = null,
        int? year = null,
        int maxResults = 2000,
        CancellationToken cancellationToken = default);
}
