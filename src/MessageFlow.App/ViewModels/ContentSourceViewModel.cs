namespace MessageFlow.App.ViewModels;

public sealed record ContentSourceViewModel(
    int Id,
    string DisplayName,
    string SourceType,
    string Description,
    string? LocalFolderPath)
{
    public string LocationDisplay =>
        string.IsNullOrWhiteSpace(LocalFolderPath) ? "No local folder configured." : LocalFolderPath;
}
