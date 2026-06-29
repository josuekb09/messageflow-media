namespace MessageFlow.Core.Bible;

public sealed class BibleBook
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public int BookOrder { get; set; }

    public ICollection<BibleVerse> Verses { get; set; } = new List<BibleVerse>();
}
