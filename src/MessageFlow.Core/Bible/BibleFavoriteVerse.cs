namespace MessageFlow.Core.Bible;

public sealed class BibleFavoriteVerse
{
    public int Id { get; set; }

    public int BibleVerseId { get; set; }

    public BibleVerse? BibleVerse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
