namespace MessageFlow.Core.Songs;

public sealed class SongSection
{
    public int Id { get; set; }

    public int SongId { get; set; }

    public int SectionOrder { get; set; }

    public string SectionType { get; set; } = string.Empty;

    public string SectionLabel { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string NormalizedText { get; set; } = string.Empty;

    public Song? Song { get; set; }
}
