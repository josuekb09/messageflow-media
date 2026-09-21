using MessageFlow.App.Localization;

namespace MessageFlow.App.ViewModels;

public sealed record SermonResultViewModel(
    int SermonId,
    string Title,
    string SermonCode,
    int Year,
    int ParagraphCount,
    string AuthorDisplayName = "",
    string SourceDisplayName = "",
    string SourceType = "",
    string BestMatchPreview = "",
    string ContentType = "")
{
    public string MatchCountDisplay => $"{ParagraphCount:N0} {(ParagraphCount == 1 ? "match" : "matches")}";

    public string BestMatchLine => string.IsNullOrWhiteSpace(BestMatchPreview)
        ? MatchCountDisplay
        : $"{MatchCountDisplay} - {BestMatchPreview}";

    /// <summary>
    /// The document's own type when it has one. A library holds several kinds of document, so
    /// the source's type is only a fallback for records imported before types were recorded.
    /// </summary>
    public string EffectiveContentType =>
        string.IsNullOrWhiteSpace(ContentType) ? SourceType : ContentType;

    public string ContentTypeDisplay => ContentSourceTypeOption.GetLabel(EffectiveContentType);

    /// <summary>
    /// "Open Sermon" for preached material, "Open Document" for circular letters and books,
    /// so the button describes what the operator is actually about to open.
    /// </summary>
    public string OpenButtonText =>
        string.Equals(EffectiveContentType, "CircularLetter", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(EffectiveContentType, "Book", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(EffectiveContentType, "Brochure", StringComparison.OrdinalIgnoreCase)
            ? Loc.T("Document_Open")
            : Loc.T("Sermon_Open");

    public bool IsCircularLetter =>
        string.Equals(EffectiveContentType, "CircularLetter", StringComparison.OrdinalIgnoreCase) ||
        Title.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase) ||
        SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase);

    public string DateDisplay
    {
        get
        {
            if (IsCircularLetter)
            {
                const string prefix = "Circular Letter - ";
                if (Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return Title[prefix.Length..].Trim();
                }
            }

            return Year > 0 ? Year.ToString() : string.Empty;
        }
    }

    public string MetaLine
    {
        get
        {
            if (IsCircularLetter)
            {
                var circularParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(DateDisplay))
                {
                    circularParts.Add(DateDisplay);
                }

                if (!string.IsNullOrWhiteSpace(AuthorDisplayName))
                {
                    circularParts.Add(AuthorDisplayName);
                }

                return string.Join(" | ", circularParts);
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(SermonCode))
            {
                parts.Add(SermonCode);
            }

            if (Year > 0)
            {
                parts.Add(Year.ToString());
            }

            if (!string.IsNullOrWhiteSpace(AuthorDisplayName))
            {
                parts.Add(AuthorDisplayName);
            }

            if (!string.IsNullOrWhiteSpace(SourceDisplayName))
            {
                parts.Add(SourceDisplayName);
            }

            return string.Join(" | ", parts);
        }
    }
}
