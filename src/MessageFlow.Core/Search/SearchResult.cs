namespace MessageFlow.Core.Search;

public sealed record SearchResult(
    int SermonId,
    string Title,
    string SermonCode,
    int Year,
    int ParagraphNumber,
    int? PageNumber,
    string Snippet);
