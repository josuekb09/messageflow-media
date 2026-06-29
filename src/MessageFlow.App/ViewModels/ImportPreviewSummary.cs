namespace MessageFlow.App.ViewModels;

public sealed class ImportPreviewSummary
{
    public ImportPreviewSummary(
        string sourceDisplayName,
        string sourceTypeDisplay,
        string localFolderPath,
        int pdfFilesFound,
        int alreadyImportedFiles,
        int readyToImportFiles,
        IReadOnlyList<string> invalidOrMissingFiles,
        string estimatedAuthorName,
        IReadOnlyList<string> readyFilePaths,
        IReadOnlyList<ImportPreviewMetadataSample> metadataSamples)
    {
        SourceDisplayName = sourceDisplayName;
        SourceTypeDisplay = sourceTypeDisplay;
        LocalFolderPath = localFolderPath;
        PdfFilesFound = pdfFilesFound;
        AlreadyImportedFiles = alreadyImportedFiles;
        ReadyToImportFiles = readyToImportFiles;
        InvalidOrMissingFiles = invalidOrMissingFiles;
        EstimatedAuthorName = estimatedAuthorName;
        ReadyFilePaths = readyFilePaths;
        MetadataSamples = metadataSamples;
    }

    public string SourceDisplayName { get; }

    public string SourceTypeDisplay { get; }

    public string LocalFolderPath { get; }

    public int PdfFilesFound { get; }

    public int AlreadyImportedFiles { get; }

    public int ReadyToImportFiles { get; }

    public IReadOnlyList<string> InvalidOrMissingFiles { get; }

    public int InvalidOrMissingFilesCount => InvalidOrMissingFiles.Count;

    public string InvalidOrMissingFilesDisplay =>
        InvalidOrMissingFiles.Count == 0
            ? "None"
            : string.Join(Environment.NewLine, InvalidOrMissingFiles.Take(8));

    public string EstimatedAuthorName { get; }

    public IReadOnlyList<string> ReadyFilePaths { get; }

    public IReadOnlyList<ImportPreviewMetadataSample> MetadataSamples { get; }

    public bool CanStartImport => ReadyToImportFiles > 0;

    public string CancelButtonText => CanStartImport ? "Cancel" : "Close";

    public string WarningText =>
        PdfFilesFound == 0
            ? "No PDF files found in this source folder."
            : ReadyToImportFiles == 0
                ? "All PDF files in this source folder are already imported. Start Import is disabled."
                : "This will import local PDF files into the MessageFlow database. Existing sermons will not be deleted.";
}
