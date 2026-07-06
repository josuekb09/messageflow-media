using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public sealed class SongResultViewModel
{
    public SongResultViewModel(SongSearchResult result)
    {
        SongId = result.SongId;
        MatchedSectionId = result.MatchedSectionId;
        Title = result.Title;
        SourceFolder = result.SourceFolder;
        FileName = result.FileName;
        SourceFilePath = result.SourceFilePath;
        WarningSummary = result.WarningSummary;
        SectionLabel = result.SectionLabel;
        LyricSnippet = result.LyricSnippet;
    }

    public int SongId { get; }

    public int? MatchedSectionId { get; }

    public string Title { get; }

    public string SourceFolder { get; }

    public string FileName { get; }

    public string SourceFilePath { get; }

    public string WarningSummary { get; }

    public string SectionLabel { get; }

    public string LyricSnippet { get; }

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningSummary);

    public string SourceDisplay =>
        string.IsNullOrWhiteSpace(SourceFolder) ? "Songs" : SourceFolder;

    public string MetaLine =>
        string.IsNullOrWhiteSpace(SectionLabel)
            ? SourceDisplay
            : $"{SourceDisplay} | {SectionLabel}";

    public string TooltipText =>
        string.IsNullOrWhiteSpace(WarningSummary)
            ? SourceFilePath
            : $"{SourceFilePath}{Environment.NewLine}{WarningSummary}";
}
