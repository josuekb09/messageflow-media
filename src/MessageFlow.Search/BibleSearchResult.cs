namespace MessageFlow.Search;

public sealed record BibleSearchResult(
    int VerseId,
    int TranslationId,
    string TranslationName,
    string TranslationAbbreviation,
    int BookId,
    string BookName,
    int BookOrder,
    int Chapter,
    int Verse,
    string Text)
{
    public string ReferenceDisplay => $"{BookName} {Chapter}:{Verse}";

    public string DisplayLine => $"{ReferenceDisplay} ({TranslationAbbreviation})";
}
