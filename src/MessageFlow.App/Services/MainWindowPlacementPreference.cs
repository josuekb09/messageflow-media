using System.IO;
using System.Text.Json;
using System.Windows;
using MessageFlow.Data;
using Forms = System.Windows.Forms;

namespace MessageFlow.App.Services;

public sealed record WindowPlacementData(
    double Left,
    double Top,
    double Width,
    double Height,
    bool IsMaximized);

public static class MainWindowPlacementPreference
{
    private static string SettingsPath =>
        Path.Combine(MessageFlowDatabase.UserSettingsDirectory, "window-placement.json");

    public static WindowPlacementData? Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<WindowPlacementData>(json);
            if (data is null || data.Width < 600 || data.Height < 400)
            {
                return null;
            }

            // Verify the window will be visible on at least one currently active monitor
            var windowRect = new System.Drawing.Rectangle(
                (int)data.Left,
                (int)data.Top,
                (int)data.Width,
                (int)data.Height);

            var isVisibleOnAnyScreen = Forms.Screen.AllScreens.Any(screen =>
                screen.WorkingArea.IntersectsWith(windowRect));

            return isVisibleOnAnyScreen ? data : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(Window window)
    {
        try
        {
            var isMaximized = window.WindowState == WindowState.Maximized;
            Rect bounds;

            if (isMaximized)
            {
                bounds = window.RestoreBounds.IsEmpty
                    ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
                    : window.RestoreBounds;
            }
            else
            {
                bounds = new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
            }

            if (bounds.Width < 600 || bounds.Height < 400)
            {
                return;
            }

            var data = new WindowPlacementData(
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                isMaximized);

            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Non-critical persistence failure must never interrupt the application.
        }
    }
}
