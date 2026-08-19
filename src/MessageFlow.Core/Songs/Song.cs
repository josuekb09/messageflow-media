namespace MessageFlow.Core.Songs;

public sealed class Song
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string NormalizedTitle { get; set; } = string.Empty;

    public string SourceFilePath { get; set; } = string.Empty;

    public string SourceFolder { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DateTime ImportedAtUtc { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public string WarningSummary { get; set; } = string.Empty;

    public string Language { get; set; } = "en";

    public bool IsActive { get; set; } = true;

    public ICollection<SongSection> Sections { get; set; } = new List<SongSection>();
}
