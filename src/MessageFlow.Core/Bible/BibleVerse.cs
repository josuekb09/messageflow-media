namespace MessageFlow.Core.Bible;

public sealed class BibleVerse
{
    public int Id { get; set; }

    public int TranslationId { get; set; }

    public BibleTranslation? BibleTranslation { get; set; }

    public int BookId { get; set; }

    public BibleBook? BibleBook { get; set; }

    public int Chapter { get; set; }

    public int Verse { get; set; }

    public string Text { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BibleFavoriteVerse> Favorites { get; } = new List<BibleFavoriteVerse>();
}
