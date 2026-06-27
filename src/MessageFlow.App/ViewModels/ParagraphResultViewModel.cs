using MessageFlow.App.Infrastructure;
using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public sealed class ParagraphResultViewModel : ObservableObject
{
    private bool isFavorite;

    public ParagraphResultViewModel(SearchResult result)
        : this(
            result.SermonId,
            result.ParagraphId,
            result.SermonTitle,
            result.SermonCode,
            result.Year,
            result.ParagraphNumber,
            result.ParagraphTextPreview,
            result.FullParagraphText,
            result.SourceFilePath,
            result.PageNumber)
    {
    }

    public ParagraphResultViewModel(
        int sermonId,
        int paragraphId,
        string sermonTitle,
        string sermonCode,
        int year,
        int paragraphNumber,
        string paragraphTextPreview,
        string fullParagraphText,
        string sourceFilePath,
        int? pageNumber)
    {
        SermonId = sermonId;
        ParagraphId = paragraphId;
        SermonTitle = sermonTitle;
        SermonCode = sermonCode;
        Year = year;
        ParagraphNumber = paragraphNumber;
        FullParagraphText = ParagraphDisplayTextCleaner.Clean(fullParagraphText);
        ParagraphTextPreview = ParagraphDisplayTextCleaner.CreatePreview(
            string.IsNullOrWhiteSpace(FullParagraphText) ? paragraphTextPreview : FullParagraphText);
        SourceFilePath = sourceFilePath;
        PageNumber = pageNumber;
    }

    public int SermonId { get; }

    public int ParagraphId { get; }

    public string SermonTitle { get; }

    public string SermonCode { get; }

    public int Year { get; }

    public int ParagraphNumber { get; }

    public string ParagraphTextPreview { get; }

    public string FullParagraphText { get; }

    public string SourceFilePath { get; }

    public int? PageNumber { get; }

    public bool IsFavorite
    {
        get => isFavorite;
        set => SetProperty(ref isFavorite, value);
    }
}
