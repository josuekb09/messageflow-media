using MessageFlow.Importer;

namespace MessageFlow.App.ViewModels;

public sealed class ImportPreviewQualitySummary
{
    public ImportPreviewQualitySummary(ParagraphQualitySummary summary)
    {
        TotalExtractedParagraphs = summary.TotalExtractedParagraphs;
        AcceptedParagraphs = summary.AcceptedParagraphs;
        RejectedPageNumbers = summary.RejectedPageNumbers;
        RejectedCorruptedText = summary.RejectedCorruptedText;
        RejectedHeadersFooters = summary.RejectedHeadersFooters;
        RejectedTooShort = summary.RejectedTooShort;
    }

    public static ImportPreviewQualitySummary Empty { get; } = new(ParagraphQualitySummary.Empty);

    public int TotalExtractedParagraphs { get; }

    public int AcceptedParagraphs { get; }

    public int RejectedPageNumbers { get; }

    public int RejectedCorruptedText { get; }

    public int RejectedHeadersFooters { get; }

    public int RejectedTooShort { get; }

    public int TotalRejected =>
        RejectedPageNumbers +
        RejectedCorruptedText +
        RejectedHeadersFooters +
        RejectedTooShort;

    public bool HasCounts => TotalExtractedParagraphs > 0;
}
