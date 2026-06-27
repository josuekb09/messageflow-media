namespace MessageFlow.Core.Sermons;

using MessageFlow.Core.ContentSources;

public sealed class Sermon
{
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public int? ContentSourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SermonCode { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime? Date { get; set; }

    public string? Location { get; set; }

    public string Language { get; set; } = "en";

    public string SourceFilePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Author? Author { get; set; }

    public ContentSource? ContentSource { get; set; }

    public ICollection<SermonParagraph> Paragraphs { get; set; } = new List<SermonParagraph>();
}
