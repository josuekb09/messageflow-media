namespace MessageFlow.Search;

public interface ISongSearchService
{
    Task<IReadOnlyList<SongSearchResult>> SearchAsync(
        SongSearchQuery query,
        CancellationToken cancellationToken = default);
}
