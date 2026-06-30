using System.Windows;

namespace MessageFlow.App;

internal static class WindowPlacement
{
    public static void FitToWorkArea(
        Window window,
        double desiredWidth,
        double desiredHeight,
        double minWidth,
        double minHeight,
        double margin = 24)
    {
        var workArea = SystemParameters.WorkArea;
        var availableWidth = Math.Max(360, workArea.Width - (margin * 2));
        var availableHeight = Math.Max(320, workArea.Height - (margin * 2));

        window.MaxWidth = availableWidth;
        window.MaxHeight = availableHeight;
        window.MinWidth = Math.Min(minWidth, availableWidth);
        window.MinHeight = Math.Min(minHeight, availableHeight);
        window.Width = Math.Clamp(desiredWidth, window.MinWidth, availableWidth);
        window.Height = Math.Clamp(desiredHeight, window.MinHeight, availableHeight);

        window.Loaded += (_, _) => KeepInsideWorkArea(window, margin);
        window.LocationChanged += (_, _) => KeepInsideWorkArea(window, margin);
        window.SizeChanged += (_, _) => KeepInsideWorkArea(window, margin);
    }

    public static void KeepInsideWorkArea(Window window, double margin = 24)
    {
        if (window.WindowState != WindowState.Normal)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        var maxLeft = workArea.Right - window.ActualWidth - margin;
        var maxTop = workArea.Bottom - window.ActualHeight - margin;
        var minLeft = workArea.Left + margin;
        var minTop = workArea.Top + margin;

        if (window.ActualWidth > 0 && window.Left > maxLeft)
        {
            window.Left = Math.Max(minLeft, maxLeft);
        }

        if (window.ActualHeight > 0 && window.Top > maxTop)
        {
            window.Top = Math.Max(minTop, maxTop);
        }

        if (window.Left < minLeft)
        {
            window.Left = minLeft;
        }

        if (window.Top < minTop)
        {
            window.Top = minTop;
        }
    }
}
