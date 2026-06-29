namespace MessageFlow.Search;

public sealed record BibleReference(
    string BookName,
    int Chapter,
    int? Verse,
    bool IsValid,
    string ErrorMessage);
