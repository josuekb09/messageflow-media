namespace MessageFlow.App.ViewModels;

public sealed record ContentSourceTypeOption(string Value, string Label)
{
    public static IReadOnlyList<ContentSourceTypeOption> All { get; } =
    [
        new("SermonPdfCollection", "Sermon PDF Collection"),
        new("Bible", "Bible"),
        new("Book", "Book"),
        new("CircularLetter", "Circular Letter"),
        new("Other", "Other")
    ];

    public static string GetLabel(string value)
    {
        return All.FirstOrDefault(option =>
                string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            ?.Label ?? value;
    }
}
