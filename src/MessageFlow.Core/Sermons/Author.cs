namespace MessageFlow.Core.Sermons;

public sealed class Author
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<Sermon> Sermons { get; set; } = new List<Sermon>();
}
