using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MessageFlow.App;

internal static class WindowPlacement
{
    public static Window? ResolveOwner()
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return null;
        }

        foreach (Window window in application.Windows)
        {
            if (window.IsVisible && window.IsActive)
            {
                return window;
            }
        }

        return application.MainWindow;
    }

    public static void ConfigureDialog(
        Window window,
        double desiredWidth,
        double desiredHeight,
        double minWidth,
        double minHeight,
        bool canResize)
    {
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.WindowState = WindowState.Normal;
        window.ResizeMode = canResize ? ResizeMode.CanResize : ResizeMode.NoResize;
        window.SizeToContent = SizeToContent.Manual;
        window.Owner ??= ResolveOwner();

        FitToWorkArea(window, desiredWidth, desiredHeight, minWidth, minHeight);

        window.StateChanged += (_, _) =>
        {
            if (window.WindowState != WindowState.Maximized)
            {
                return;
            }

            // Child windows that inherit maximize from a maximized owner bleed off-screen.
            // Snap them to the work area instead of using OS maximize.
            window.WindowState = WindowState.Normal;
            SnapToWorkArea(window);
        };

        window.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape || Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            if (Keyboard.FocusedElement is TextBox or PasswordBox or RichTextBox)
            {
                return;
            }

            e.Handled = true;
            window.Close();
        };
    }

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

    private static void SnapToWorkArea(Window window, double margin = 24)
    {
        var workArea = SystemParameters.WorkArea;
        var width = Math.Max(window.MinWidth, workArea.Width - (margin * 2));
        var height = Math.Max(window.MinHeight, workArea.Height - (margin * 2));

        window.Width = Math.Min(width, window.MaxWidth > 0 ? window.MaxWidth : width);
        window.Height = Math.Min(height, window.MaxHeight > 0 ? window.MaxHeight : height);
        window.Left = workArea.Left + margin;
        window.Top = workArea.Top + margin;
    }
}
