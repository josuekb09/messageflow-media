using MessageFlow.App.Infrastructure;
using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public sealed class ParagraphResultViewModel : ObservableObject
{
    private bool isFavorite;

    public ParagraphResultViewModel(SearchResult result)
        : this(
            result.SermonId,
            result.ParagraphId,
            result.SermonTitle,
            result.SermonCode,
            result.Year,
            result.ParagraphNumber,
            result.ParagraphTextPreview,
            result.FullParagraphText,
            result.SourceFilePath,
            result.PageNumber,
            result.AuthorDisplayName,
            result.SourceDisplayName,
            result.SourceType)
    {
    }

    public ParagraphResultViewModel(
        int sermonId,
        int paragraphId,
        string sermonTitle,
        string sermonCode,
        int year,
        int paragraphNumber,
        string paragraphTextPreview,
        string fullParagraphText,
        string sourceFilePath,
        int? pageNumber,
        string authorDisplayName = "",
        string sourceDisplayName = "",
        string sourceType = "")
    {
        SermonId = sermonId;
        ParagraphId = paragraphId;
        SermonTitle = sermonTitle;
        SermonCode = sermonCode;
        Year = year;
        ParagraphNumber = paragraphNumber;
        FullParagraphText = ParagraphDisplayTextCleaner.Clean(fullParagraphText);
        ParagraphTextPreview = ParagraphDisplayTextCleaner.CreatePreview(
            string.IsNullOrWhiteSpace(FullParagraphText) ? paragraphTextPreview : FullParagraphText);
        SourceFilePath = sourceFilePath;
        PageNumber = pageNumber;
        AuthorDisplayName = authorDisplayName;
        SourceDisplayName = sourceDisplayName;
        SourceType = sourceType;
    }

    public int SermonId { get; }

    public int ParagraphId { get; }

    public string SermonTitle { get; }

    public string SermonCode { get; }

    public int Year { get; }

    public int ParagraphNumber { get; }

    public string ParagraphTextPreview { get; }

    public string FullParagraphText { get; }

    public string SourceFilePath { get; }

    public int? PageNumber { get; }

    public string AuthorDisplayName { get; }

    public string SourceDisplayName { get; }

    public string SourceType { get; }

    public string ContentSourceDisplay =>
        string.IsNullOrWhiteSpace(SourceDisplayName) ? "Local library" : SourceDisplayName;

    public string ContentTypeDisplay => ContentSourceTypeOption.GetLabel(SourceType);

    public bool IsCircularLetter =>
        string.Equals(SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase) ||
        SermonTitle.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase) ||
        SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase);

    public string DateDisplay
    {
        get
        {
            if (IsCircularLetter)
            {
                const string prefix = "Circular Letter - ";
                if (SermonTitle.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return SermonTitle[prefix.Length..].Trim();
                }
            }

            return Year > 0 ? Year.ToString() : string.Empty;
        }
    }

    public string MetadataLine
    {
        get
        {
            if (IsCircularLetter)
            {
                var circularParts = new List<string> { "Circular Letter" };
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
                parts.Add($"Author: {AuthorDisplayName}");
            }

            if (!string.IsNullOrWhiteSpace(SourceDisplayName))
            {
                parts.Add($"Source: {SourceDisplayName}");
            }

            return string.Join(" | ", parts);
        }
    }

    public bool IsFavorite
    {
        get => isFavorite;
        set => SetProperty(ref isFavorite, value);
    }
}
