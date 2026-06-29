namespace MessageFlow.App.ViewModels;

public sealed record BibleTranslationOption(
    int Id,
    string Name,
    string Abbreviation,
    string Language)
{
    public string DisplayName => $"{Abbreviation} - {Name}";

    public override string ToString()
    {
        return DisplayName;
    }
}
