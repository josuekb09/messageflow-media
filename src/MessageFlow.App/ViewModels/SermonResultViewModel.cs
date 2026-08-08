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
    string BestMatchPreview = "")
{
    public string MatchCountDisplay => $"{ParagraphCount:N0} {(ParagraphCount == 1 ? "match" : "matches")}";

    public string BestMatchLine => string.IsNullOrWhiteSpace(BestMatchPreview)
        ? MatchCountDisplay
        : $"{MatchCountDisplay} - {BestMatchPreview}";

    public string ContentTypeDisplay => ContentSourceTypeOption.GetLabel(SourceType);

    public bool IsCircularLetter =>
        string.Equals(SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase) ||
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
