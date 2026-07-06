using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Search;

public sealed class SongSearchService(MessageFlowDbContext dbContext) : ISongSearchService
{
    public async Task<IReadOnlyList<SongSearchResult>> SearchAsync(
        SongSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(query.MaxResults, 1, 250);
        var normalized = SongTextNormalizer.Normalize(query.SearchText);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return await dbContext.Songs
                .AsNoTracking()
                .Where(song => song.IsActive)
                .OrderBy(song => song.Title)
                .ThenBy(song => song.Id)
                .Select(song => new SongSearchResult(
                    song.Id,
                    song.Sections
                        .OrderBy(section => section.SectionOrder)
                        .Select(section => (int?)section.Id)
                        .FirstOrDefault(),
                    song.Title,
                    song.SourceFolder,
                    song.FileName,
                    song.SourceFilePath,
                    song.WarningSummary,
                    song.Sections
                        .OrderBy(section => section.SectionOrder)
                        .Select(section => section.SectionLabel)
                        .FirstOrDefault() ?? string.Empty,
                    song.Sections
                        .OrderBy(section => section.SectionOrder)
                        .Select(section => section.Text.Length <= 220 ? section.Text : section.Text.Substring(0, 220) + "...")
                        .FirstOrDefault() ?? string.Empty))
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        var containsLike = BuildContainsLike(normalized);
        var startsWithLike = BuildStartsWithLike(normalized);
        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(8)
            .ToArray();

        var songs = dbContext.Songs
            .AsNoTracking()
            .Where(song => song.IsActive)
            .Where(song =>
                EF.Functions.Like(song.NormalizedTitle, containsLike, "\\") ||
                song.Sections.Any(section => EF.Functions.Like(section.NormalizedText, containsLike, "\\")));

        foreach (var term in terms)
        {
            var termLike = BuildContainsLike(term);
            songs = songs.Where(song =>
                EF.Functions.Like(song.NormalizedTitle, termLike, "\\") ||
                song.Sections.Any(section => EF.Functions.Like(section.NormalizedText, termLike, "\\")));
        }

        return await songs
            .Select(song => new
            {
                Song = song,
                Rank =
                    song.NormalizedTitle == normalized ? 0 :
                    EF.Functions.Like(song.NormalizedTitle, startsWithLike, "\\") ? 1 :
                    EF.Functions.Like(song.NormalizedTitle, containsLike, "\\") ? 2 : 3,
                MatchedSection = song.Sections
                    .Where(section => EF.Functions.Like(section.NormalizedText, containsLike, "\\"))
                    .OrderBy(section => section.SectionOrder)
                    .Select(section => new
                    {
                        section.Id,
                        section.SectionLabel,
                        section.Text
                    })
                    .FirstOrDefault(),
                FirstSection = song.Sections
                    .OrderBy(section => section.SectionOrder)
                    .Select(section => new
                    {
                        section.Id,
                        section.SectionLabel,
                        section.Text
                    })
                    .FirstOrDefault()
            })
            .OrderBy(row => row.Rank)
            .ThenBy(row => row.Song.Title)
            .ThenBy(row => row.Song.Id)
            .Take(limit)
            .Select(row => new SongSearchResult(
                row.Song.Id,
                row.MatchedSection != null ? row.MatchedSection.Id : row.FirstSection != null ? row.FirstSection.Id : null,
                row.Song.Title,
                row.Song.SourceFolder,
                row.Song.FileName,
                row.Song.SourceFilePath,
                row.Song.WarningSummary,
                row.MatchedSection != null ? row.MatchedSection.SectionLabel : row.FirstSection != null ? row.FirstSection.SectionLabel : string.Empty,
                row.MatchedSection != null
                    ? CreateSnippet(row.MatchedSection.Text)
                    : row.FirstSection != null
                        ? CreateSnippet(row.FirstSection.Text)
                        : string.Empty))
            .ToListAsync(cancellationToken);
    }

    private static string CreateSnippet(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= 220 ? trimmed : $"{trimmed[..220]}...";
    }

    private static string BuildContainsLike(string value)
    {
        return $"%{EscapeLike(value.Trim())}%";
    }

    private static string BuildStartsWithLike(string value)
    {
        return $"{EscapeLike(value.Trim())}%";
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
