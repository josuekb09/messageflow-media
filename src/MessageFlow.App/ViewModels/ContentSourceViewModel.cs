namespace MessageFlow.App.ViewModels;

public sealed record ContentSourceViewModel(
    int Id,
    string Name,
    string DisplayName,
    string SourceType,
    string Description,
    string? LocalFolderPath)
{
    public string SourceTypeDisplay => ContentSourceTypeOption.GetLabel(SourceType);

    public string LocationDisplay =>
        string.IsNullOrWhiteSpace(LocalFolderPath) ? "No local folder configured." : LocalFolderPath;

    public override string ToString()
    {
        return DisplayName;
    }
}
