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
}
