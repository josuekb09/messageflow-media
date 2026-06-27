namespace MessageFlow.Core.Sermons;

public sealed class SermonParagraph
{
    public int Id { get; set; }

    public int SermonId { get; set; }

    public int ParagraphNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Sermon? Sermon { get; set; }

    public ICollection<FavoriteParagraph> Favorites { get; set; } = new List<FavoriteParagraph>();

    public ICollection<ProjectionHistory> ProjectionHistories { get; set; } = new List<ProjectionHistory>();
}
