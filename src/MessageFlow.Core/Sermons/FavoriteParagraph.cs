namespace MessageFlow.Core.Sermons;

public sealed class FavoriteParagraph
{
    public int Id { get; set; }

    public int SermonParagraphId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public SermonParagraph? SermonParagraph { get; set; }
}
