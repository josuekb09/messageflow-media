namespace MessageFlow.Importer;

public sealed record ParagraphDraft(
    int ParagraphNumber,
    string Text,
    string SearchText,
    int? PageNumber,
    bool HasDetectedParagraphNumber);
