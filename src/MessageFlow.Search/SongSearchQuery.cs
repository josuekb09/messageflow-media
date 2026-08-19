namespace MessageFlow.Search;

public sealed record SongSearchQuery(
    string SearchText,
    int MaxResults = 150,
    string? Language = null);
