namespace MessageFlow.Core.Sermons;

public sealed class ImportLog
{
    public int Id { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
