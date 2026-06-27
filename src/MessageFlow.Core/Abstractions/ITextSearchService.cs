using MessageFlow.Core.Search;

namespace MessageFlow.Core.Abstractions;

public interface ITextSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults = 25,
        CancellationToken cancellationToken = default);
}
