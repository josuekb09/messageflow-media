using MessageFlow.Core.Search;
using MessageFlow.Core.Sermons;

namespace MessageFlow.Core.Abstractions;

public interface ISermonRepository
{
    Task AddAsync(Sermon sermon, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> SearchParagraphsAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default);
}
