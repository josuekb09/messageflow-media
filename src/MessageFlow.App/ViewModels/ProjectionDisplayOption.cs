namespace MessageFlow.App.ViewModels;

public sealed record ProjectionDisplayOption(
    string PreferenceKey,
    string Label,
    bool IsAuto = false)
{
    public override string ToString()
    {
        return Label;
    }
}
