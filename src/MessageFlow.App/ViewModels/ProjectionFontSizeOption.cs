namespace MessageFlow.App.ViewModels;

public sealed record ProjectionFontSizeOption(string Label, double FontSize, double LineHeight)
{
    public override string ToString()
    {
        return Label;
    }
}
