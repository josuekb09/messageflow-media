namespace MessageFlow.Core.Bible;

public sealed class BibleTranslation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BibleVerse> Verses { get; set; } = new List<BibleVerse>();
}
