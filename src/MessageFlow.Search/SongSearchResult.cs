namespace MessageFlow.Search;

public sealed record SongSearchResult(
    int SongId,
    int? MatchedSectionId,
    string Title,
    string SourceFolder,
    string FileName,
    string SourceFilePath,
    string WarningSummary,
    string SectionLabel,
    string LyricSnippet);
