namespace MessageFlow.App.ViewModels;

public sealed record FilterOption(int? Value, string Label)
{
    public override string ToString()
    {
        return Label;
    }
}
