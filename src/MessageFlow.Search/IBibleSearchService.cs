namespace MessageFlow.Search;

public interface IBibleSearchService
{
    Task<IReadOnlyList<BibleSearchResult>> SearchAsync(
        BibleSearchQuery query,
        CancellationToken cancellationToken = default);
}
