namespace MessageFlow.Importer;

public sealed record ImportProgress(
    string Message,
    int CurrentFile,
    int TotalFiles,
    int ImportedParagraphs,
    int SkippedFiles,
    int ErrorCount);
