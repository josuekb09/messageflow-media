namespace MessageFlow.App.ViewModels;

public sealed record ContentSourceTypeOption(string Value, string Label)
{
    /// <summary>
    /// Types offered when creating a content source. "Sermon" and "Meeting" also appear as
    /// per-document content types, which is why both spellings map to a label here.
    /// </summary>
    public static IReadOnlyList<ContentSourceTypeOption> All { get; } =
    [
        new("SermonPdfCollection", "Sermon"),
        new("CircularLetter", "Circular Letter"),
        new("Bible", "Bible"),
        new("Book", "Book"),
        new("Brochure", "Brochure"),
        new("Other", "Other")
    ];

    /// <summary>
    /// Per-document content types that are not offered as source types. Kept separate so the
    /// Add Source dialog keeps showing only the types a whole library can have.
    /// </summary>
    private static IReadOnlyList<ContentSourceTypeOption> DocumentOnlyTypes { get; } =
    [
        new("Sermon", "Sermon"),
        new("Meeting", "Meeting")
    ];

    public static string GetLabel(string value)
    {
        return All.Concat(DocumentOnlyTypes)
            .FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            ?.Label ?? value;
    }

    public override string ToString()
    {
        return Label;
    }
}
