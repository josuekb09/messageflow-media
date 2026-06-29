namespace MessageFlow.App.ViewModels;

public sealed record BibleImportPreviewSample(
    string Reference,
    string Text)
{
    public string DisplayText => $"{Reference} - {Text}";
}
