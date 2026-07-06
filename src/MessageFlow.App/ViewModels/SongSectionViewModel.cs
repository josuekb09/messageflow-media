namespace MessageFlow.App.ViewModels;

public sealed class SongSectionViewModel
{
    public SongSectionViewModel(
        int sectionId,
        int songId,
        int sectionOrder,
        string sectionType,
        string sectionLabel,
        string text)
    {
        SectionId = sectionId;
        SongId = songId;
        SectionOrder = sectionOrder;
        SectionType = sectionType;
        SectionLabel = sectionLabel;
        Text = text;
    }

    public int SectionId { get; }

    public int SongId { get; }

    public int SectionOrder { get; }

    public string SectionType { get; }

    public string SectionLabel { get; }

    public string Text { get; }

    public string PreviewText => Text.Length <= 220 ? Text : $"{Text[..220]}...";
}
