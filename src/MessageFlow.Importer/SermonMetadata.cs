namespace MessageFlow.Importer;

public sealed record SermonMetadata(
    string Title,
    string SermonCode,
    int Year,
    DateTime? Date,
    string? Location,
    string Language);
