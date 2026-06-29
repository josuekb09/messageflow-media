namespace MessageFlow.App.ViewModels;

public sealed record BibleCsvVerseRow(
    int RowNumber,
    string BookName,
    int Chapter,
    int Verse,
    string Text)
{
    public string SearchText => Text.ToUpperInvariant();

    public string ReferenceDisplay => $"{BookName} {Chapter}:{Verse}";
}
