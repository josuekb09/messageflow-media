namespace MessageFlow.App.ViewModels;

public sealed record SermonResultViewModel(
    int SermonId,
    string Title,
    string SermonCode,
    int Year,
    int ParagraphCount);
