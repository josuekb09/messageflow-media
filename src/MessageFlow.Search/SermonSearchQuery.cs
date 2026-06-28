namespace MessageFlow.Search;

public sealed record SermonSearchQuery(
    int? AuthorId = null,
    int? ContentSourceId = null,
    string? SearchText = null,
    string? Title = null,
    string? SermonCode = null,
    int? Year = null,
    int? ParagraphNumber = null,
    string? Keyword = null,
    int MaxResults = 50);
