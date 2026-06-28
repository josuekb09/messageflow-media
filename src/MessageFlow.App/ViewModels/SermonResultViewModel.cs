namespace MessageFlow.App.ViewModels;

public sealed record SermonResultViewModel(
    int SermonId,
    string Title,
    string SermonCode,
    int Year,
    int ParagraphCount,
    string AuthorDisplayName = "",
    string SourceDisplayName = "",
    string SourceType = "")
{
    public string ContentTypeDisplay => ContentSourceTypeOption.GetLabel(SourceType);

    public string MetaLine
    {
        get
        {
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
