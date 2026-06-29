using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Search;

public sealed class BibleSearchService(MessageFlowDbContext dbContext) : IBibleSearchService
{
    public async Task<IReadOnlyList<BibleSearchResult>> SearchAsync(
        BibleSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.SearchText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var limit = Math.Clamp(query.MaxResults, 1, 250);
        var verses = dbContext.BibleVerses
            .AsNoTracking()
            .Include(verse => verse.BibleBook)
            .Include(verse => verse.BibleTranslation)
            .AsQueryable();

        if (query.TranslationId is not null)
        {
            verses = verses.Where(verse => verse.TranslationId == query.TranslationId.Value);
        }

        if (BibleReferenceParser.TryParse(normalized, out var reference) && reference.IsValid)
        {
            verses = verses.Where(verse =>
                verse.BibleBook != null &&
                verse.BibleBook.Name == reference.BookName &&
                verse.Chapter == reference.Chapter);

            if (reference.Verse is not null)
            {
                verses = verses.Where(verse => verse.Verse == reference.Verse.Value);
            }

            return await ProjectResultsAsync(
                verses
                    .OrderBy(verse => verse.BibleBook!.BookOrder)
                    .ThenBy(verse => verse.Chapter)
                    .ThenBy(verse => verse.Verse)
                    .Take(limit),
                cancellationToken);
        }

        var like = BuildContainsLike(normalized.ToUpperInvariant());
        return await ProjectResultsAsync(
            verses
                .Where(verse => EF.Functions.Like(verse.SearchText, like, "\\"))
                .OrderBy(verse => verse.BibleBook!.BookOrder)
                .ThenBy(verse => verse.Chapter)
                .ThenBy(verse => verse.Verse)
                .Take(limit),
            cancellationToken);
    }

    private static async Task<IReadOnlyList<BibleSearchResult>> ProjectResultsAsync(
        IQueryable<MessageFlow.Core.Bible.BibleVerse> query,
        CancellationToken cancellationToken)
    {
        return await query
            .Select(verse => new BibleSearchResult(
                verse.Id,
                verse.TranslationId,
                verse.BibleTranslation!.Name,
                verse.BibleTranslation.Abbreviation,
                verse.BookId,
                verse.BibleBook!.Name,
                verse.BibleBook.BookOrder,
                verse.Chapter,
                verse.Verse,
                verse.Text))
            .ToListAsync(cancellationToken);
    }

    private static string BuildContainsLike(string value)
    {
        return $"%{EscapeLike(value.Trim())}%";
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
