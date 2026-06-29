namespace MessageFlow.App.ViewModels;

public sealed class BibleImportPreviewSummary
{
    public BibleImportPreviewSummary(
        string translationName,
        string abbreviation,
        string language,
        string description,
        string filePath,
        IReadOnlyList<BibleCsvVerseRow> verses,
        IReadOnlyList<string> invalidRows)
    {
        TranslationName = translationName;
        Abbreviation = abbreviation;
        Language = language;
        Description = description;
        FilePath = filePath;
        Verses = verses;
        InvalidRows = invalidRows;
        Samples = verses
            .Take(10)
            .Select(row => new BibleImportPreviewSample(row.ReferenceDisplay, row.Text))
            .ToList();
    }

    public string TranslationName { get; }

    public string Abbreviation { get; }

    public string Language { get; }

    public string Description { get; }

    public string FilePath { get; }

    public IReadOnlyList<BibleCsvVerseRow> Verses { get; }

    public IReadOnlyList<string> InvalidRows { get; }

    public IReadOnlyList<BibleImportPreviewSample> Samples { get; }

    public int VerseCount => Verses.Count;

    public int InvalidRowCount => InvalidRows.Count;
}
