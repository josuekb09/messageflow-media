namespace MessageFlow.Importer;

public sealed record SourceMetadataContext(
    int? Id,
    string Name,
    string DisplayName,
    string SourceType);

public sealed record ImportAuthorMetadata(
    string FullName,
    string DisplayName,
    string Description);
