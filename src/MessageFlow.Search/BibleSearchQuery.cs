namespace MessageFlow.Search;

public sealed record BibleSearchQuery(
    string SearchText,
    int? TranslationId,
    int MaxResults = 100);
