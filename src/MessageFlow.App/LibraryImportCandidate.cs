using MessageFlow.App.Infrastructure;
using System.IO;

namespace MessageFlow.App;

public sealed class LibraryImportCandidate : ObservableObject
{
    private bool isSelected;
    private string title;
    private string status;

    public LibraryImportCandidate(
        string sourcePath,
        string contentType,
        string title,
        string sha256,
        string status,
        bool canImport,
        int itemCount)
    {
        SourcePath = sourcePath;
        ContentType = contentType;
        this.title = title;
        Sha256 = sha256;
        this.status = status;
        CanImport = canImport;
        ItemCount = itemCount;
        isSelected = canImport;
    }

    public string SourcePath { get; }
    public string FileName => Path.GetFileName(SourcePath);
    public string ContentType { get; }
    public string Sha256 { get; }
    public bool CanImport { get; }
    public int ItemCount { get; }
    public object? PreparedContent { get; init; }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value && CanImport);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }
}
