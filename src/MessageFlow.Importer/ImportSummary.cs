namespace MessageFlow.Importer;

public sealed class ImportSummary
{
    public int TotalFiles { get; set; }

    public int ImportedFiles { get; set; }

    public int SkippedFiles { get; set; }

    public int ImportedParagraphs { get; set; }

    public int ErrorCount { get; set; }
}
