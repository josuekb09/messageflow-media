namespace MessageFlow.App.ViewModels;

public sealed class SourceDiagnosticsViewModel
{
    public SourceDiagnosticsViewModel(
        string displayName,
        string sourceTypeDisplay,
        string localFolderPathDisplay,
        string sourceStatus,
        int importedDocumentCount,
        int importedParagraphCount,
        int pdfFilesFound,
        string linkedAuthorDisplay,
        bool looksLikeTestSource,
        int suspiciousMetadataCount,
        IReadOnlyList<string> suspiciousMetadataSamples)
    {
        DisplayName = displayName;
        SourceTypeDisplay = sourceTypeDisplay;
        LocalFolderPathDisplay = localFolderPathDisplay;
        SourceStatus = sourceStatus;
        ImportedDocumentCount = importedDocumentCount;
        ImportedParagraphCount = importedParagraphCount;
        PdfFilesFound = pdfFilesFound;
        LinkedAuthorDisplay = linkedAuthorDisplay;
        LooksLikeTestSource = looksLikeTestSource;
        SuspiciousMetadataCount = suspiciousMetadataCount;
        SuspiciousMetadataSamples = suspiciousMetadataSamples;
    }

    public string DisplayName { get; }

    public string SourceTypeDisplay { get; }

    public string LocalFolderPathDisplay { get; }

    public string SourceStatus { get; }

    public int ImportedDocumentCount { get; }

    public int ImportedParagraphCount { get; }

    public int PdfFilesFound { get; }

    public string LinkedAuthorDisplay { get; }

    public bool LooksLikeTestSource { get; }

    public int SuspiciousMetadataCount { get; }

    public IReadOnlyList<string> SuspiciousMetadataSamples { get; }

    public string ImportedDocumentCountDisplay => ImportedDocumentCount.ToString("N0");

    public string ImportedParagraphCountDisplay => ImportedParagraphCount.ToString("N0");

    public string PdfFilesFoundDisplay => PdfFilesFound.ToString("N0");

    public string LooksLikeTestSourceDisplay => LooksLikeTestSource ? "Yes" : "No";

    public string SuspiciousMetadataDisplay =>
        SuspiciousMetadataCount == 0 ? "No" : $"Yes ({SuspiciousMetadataCount:N0})";

    public string SuspiciousMetadataSamplesDisplay =>
        SuspiciousMetadataSamples.Count == 0
            ? "No suspicious metadata found."
            : string.Join(Environment.NewLine, SuspiciousMetadataSamples);

    public static SourceDiagnosticsViewModel None { get; } = new(
        "No source selected",
        "None",
        "No local folder selected.",
        "No Source Selected",
        0,
        0,
        0,
        "No imported author available.",
        false,
        0,
        []);

    public static SourceDiagnosticsViewModel Loading(ContentSourceViewModel source) => new(
        source.DisplayName,
        source.SourceTypeDisplay,
        string.IsNullOrWhiteSpace(source.LocalFolderPath) ? "No local folder configured." : source.LocalFolderPath,
        "Checking Source",
        0,
        0,
        0,
        "Checking...",
        source.DisplayName.Contains("Test", StringComparison.OrdinalIgnoreCase),
        0,
        []);
}
