namespace MessageFlow.Search;

/// <param name="SourceType">Type declared by the owning content source.</param>
/// <param name="ContentType">
/// Type of this specific document (Sermon, CircularLetter, Meeting, Book, ...). Falls back to
/// <paramref name="SourceType"/> while a document has not been classified yet.
/// </param>
/// <param name="Language">Content language of the document, not the language of the UI.</param>
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
    string SourceType = "",
    string ContentType = "",
    string Language = "");
