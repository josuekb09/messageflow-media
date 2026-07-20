namespace MessageFlow.App.ViewModels;

public sealed record ProjectionDisplayTarget(
    string PreferenceKey,
    string DeviceName,
    string SelectorLabel,
    string StatusDisplayName,
    bool IsPrimary,
    int DisplayNumber,
    int ScreenCount,
    double Left,
    double Top,
    double Width,
    double Height,
    double WorkingAreaLeft,
    double WorkingAreaTop,
    double WorkingAreaWidth,
    double WorkingAreaHeight,
    double DpiX,
    double DpiY)
{
    public string BoundsDisplay =>
        $"{Left:0},{Top:0} {Width:0}x{Height:0}";
}
