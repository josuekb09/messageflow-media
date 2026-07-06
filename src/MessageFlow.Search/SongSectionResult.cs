namespace MessageFlow.Search;

public sealed record SongSectionResult(
    int SectionId,
    int SongId,
    int SectionOrder,
    string SectionType,
    string SectionLabel,
    string Text);
