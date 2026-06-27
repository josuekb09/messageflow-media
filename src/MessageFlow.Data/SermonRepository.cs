using MessageFlow.Core.Abstractions;
using MessageFlow.Core.Search;
using MessageFlow.Core.Sermons;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Data;

public sealed class SermonRepository(MessageFlowDbContext dbContext) : ISermonRepository
{
    public async Task AddAsync(Sermon sermon, CancellationToken cancellationToken = default)
    {
        dbContext.Sermons.Add(sermon);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchParagraphsAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var searchText = query.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        var take = Math.Clamp(maxResults, 1, 100);

        return await dbContext.SermonParagraphs
            .AsNoTracking()
            .Where(paragraph => paragraph.SearchText.Contains(searchText))
            .OrderBy(paragraph => paragraph.Sermon!.Year)
            .ThenBy(paragraph => paragraph.Sermon!.Date)
            .ThenBy(paragraph => paragraph.ParagraphNumber)
            .Select(paragraph => new SearchResult(
                paragraph.SermonId,
                paragraph.Sermon!.Title,
                paragraph.Sermon.SermonCode,
                paragraph.Sermon.Year,
                paragraph.ParagraphNumber,
                paragraph.PageNumber,
                paragraph.Text.Length <= 240 ? paragraph.Text : paragraph.Text.Substring(0, 240)))
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
