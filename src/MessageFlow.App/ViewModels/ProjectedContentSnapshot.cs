namespace MessageFlow.App.ViewModels;

public enum ProjectionContentType
{
    Sermon,
    Bible,
    Song
}

/// <summary>
/// Immutable copy of content explicitly sent to the live church display.
/// Operator searches and preview selections never mutate this snapshot.
/// </summary>
public sealed record ProjectedContentSnapshot(
    ProjectionContentType ContentType,
    string Title,
    string Subtitle,
    string BodyText)
{
    public int? SourceId { get; init; }

    public int? ItemId { get; init; }

    public string SourceKey { get; init; } = string.Empty;
}
