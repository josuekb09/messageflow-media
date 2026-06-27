namespace MessageFlow.App.ViewModels;

public sealed record SavedParagraphViewModel(
    int Id,
    int ParagraphId,
    string SermonTitle,
    string SermonCode,
    int Year,
    int ParagraphNumber,
    string ParagraphTextPreview,
    DateTime SavedAt,
    string Kind)
{
    public string Meta => $"{SermonCode} | {Year} | Paragraph {ParagraphNumber}";

    public string SavedAtText => SavedAt.ToLocalTime().ToString("g");
}
