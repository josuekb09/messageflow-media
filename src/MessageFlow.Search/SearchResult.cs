namespace MessageFlow.Search;

public sealed record SearchResult(
    int SermonId,
    int ParagraphId,
    string SermonTitle,
    string SermonCode,
    int Year,
    int ParagraphNumber,
    string ParagraphTextPreview,
    string FullParagraphText,
    string SourceFilePath,
    int? PageNumber,
    string AuthorDisplayName = "",
    string SourceDisplayName = "",
    string SourceType = "");
