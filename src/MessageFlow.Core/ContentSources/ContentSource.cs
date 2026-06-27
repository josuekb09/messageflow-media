using MessageFlow.Core.Sermons;

namespace MessageFlow.Core.ContentSources;

public sealed class ContentSource
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? LocalFolderPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Sermon> Sermons { get; set; } = new List<Sermon>();
}
