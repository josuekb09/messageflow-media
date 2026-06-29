namespace MessageFlow.App.ViewModels;

public sealed record ImportPreviewMetadataSample(
    string FileName,
    string DetectedTitle,
    string DetectedCode,
    int DetectedYear,
    string DetectedAuthor,
    string DetectedSourceType,
    string Status,
    string Warning)
{
    public string DetectedYearDisplay => DetectedYear > 0 ? DetectedYear.ToString() : "Unknown";

    public string WarningDisplay => string.IsNullOrWhiteSpace(Warning) ? string.Empty : Warning;
}
