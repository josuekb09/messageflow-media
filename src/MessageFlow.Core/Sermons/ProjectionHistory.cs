namespace MessageFlow.Core.Sermons;

public sealed class ProjectionHistory
{
    public int Id { get; set; }

    public int SermonParagraphId { get; set; }

    public DateTime ProjectedAt { get; set; } = DateTime.UtcNow;

    public string? SearchQuery { get; set; }

    public SermonParagraph? SermonParagraph { get; set; }
}
