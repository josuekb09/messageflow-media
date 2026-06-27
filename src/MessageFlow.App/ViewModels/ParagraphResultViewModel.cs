using MessageFlow.App.Infrastructure;
using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public sealed class ParagraphResultViewModel : ObservableObject
{
    private bool isFavorite;

    public ParagraphResultViewModel(SearchResult result)
    {
        SermonId = result.SermonId;
        ParagraphId = result.ParagraphId;
        SermonTitle = result.SermonTitle;
        SermonCode = result.SermonCode;
        Year = result.Year;
        ParagraphNumber = result.ParagraphNumber;
        ParagraphTextPreview = result.ParagraphTextPreview;
        FullParagraphText = result.FullParagraphText;
        SourceFilePath = result.SourceFilePath;
        PageNumber = result.PageNumber;
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
