namespace MessageFlow.Importer;

public sealed record ParagraphQualitySummary(
    int TotalExtractedParagraphs,
    int AcceptedParagraphs,
    int RejectedPageNumbers,
    int RejectedCorruptedText,
    int RejectedHeadersFooters,
    int RejectedTooShort)
{
    public static ParagraphQualitySummary Empty { get; } = new(0, 0, 0, 0, 0, 0);

    public int TotalRejected =>
        RejectedPageNumbers +
        RejectedCorruptedText +
        RejectedHeadersFooters +
        RejectedTooShort;

    public ParagraphQualitySummary Add(ParagraphQualitySummary other)
    {
        return new ParagraphQualitySummary(
            TotalExtractedParagraphs + other.TotalExtractedParagraphs,
            AcceptedParagraphs + other.AcceptedParagraphs,
            RejectedPageNumbers + other.RejectedPageNumbers,
            RejectedCorruptedText + other.RejectedCorruptedText,
            RejectedHeadersFooters + other.RejectedHeadersFooters,
            RejectedTooShort + other.RejectedTooShort);
    }
}
