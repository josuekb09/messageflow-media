using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Forms = System.Windows.Forms;

namespace MessageFlow.App.ViewModels;

public static partial class ProjectionDisplayService
{
    public const string AutoPreferenceKey = "auto";

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MessageFlow",
            "projection-display.txt");

    public static IReadOnlyList<ProjectionDisplayOption> GetDisplayOptions()
    {
        var options = new List<ProjectionDisplayOption>
        {
            new(AutoPreferenceKey, "Auto", IsAuto: true)
        };

        options.AddRange(GetDisplayTargets()
            .Select(target => new ProjectionDisplayOption(target.PreferenceKey, target.SelectorLabel)));

        return options;
    }

    public static ProjectionDisplayTarget ResolveTarget(string? preferenceKey)
    {
        var targets = GetDisplayTargets();
        if (targets.Count == 0)
        {
            throw new InvalidOperationException("No Windows display was detected.");
        }

        if (!string.IsNullOrWhiteSpace(preferenceKey) &&
            !string.Equals(preferenceKey, AutoPreferenceKey, StringComparison.OrdinalIgnoreCase))
        {
            var preferred = targets.FirstOrDefault(target =>
                string.Equals(target.PreferenceKey, preferenceKey, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return targets
                   .Where(target => !target.IsPrimary)
                   .OrderByDescending(target => target.WorkingAreaWidth * target.WorkingAreaHeight)
                   .ThenBy(target => target.DisplayNumber)
                   .FirstOrDefault() ??
               targets.First(target => target.IsPrimary);
    }

    public static string LoadPreference()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? File.ReadAllText(SettingsPath).Trim()
                : AutoPreferenceKey;
        }
        catch
        {
            return AutoPreferenceKey;
        }
    }

    public static void SavePreference(string preferenceKey)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, string.IsNullOrWhiteSpace(preferenceKey) ? AutoPreferenceKey : preferenceKey);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Projection display preference could not be saved.", ex);
        }
    }

    public static void PrepareFullscreenWindow(Window window, ProjectionDisplayTarget target)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.ShowActivated = true;
        window.Topmost = true;
        window.WindowState = WindowState.Normal;
        ApplyTargetBounds(window, target);
        window.WindowState = WindowState.Maximized;
    }

    public static void MaximizeOnTarget(Window window, ProjectionDisplayTarget target)
    {
        window.WindowState = WindowState.Normal;
        ApplyTargetBounds(window, target);
        window.WindowState = WindowState.Maximized;
        window.Topmost = true;
        window.Activate();
        window.Focus();
    }

    private static void ApplyTargetBounds(Window window, ProjectionDisplayTarget target)
    {
        window.Left = target.Left;
        window.Top = target.Top;
        window.Width = target.Width;
        window.Height = target.Height;
    }

    public static IReadOnlyList<ProjectionDisplayTarget> GetDisplayTargets()
    {
        var screens = Forms.Screen.AllScreens;
        var screenCount = screens.Length;

        return screens
            .Select((screen, index) => CreateTarget(screen, index + 1, screenCount))
            .OrderBy(target => target.DisplayNumber)
            .ThenBy(target => target.Left)
            .ThenBy(target => target.Top)
            .ToList();
    }

    private static ProjectionDisplayTarget CreateTarget(
        Forms.Screen screen,
        int fallbackDisplayNumber,
        int screenCount)
    {
        var displayNumber = TryParseDisplayNumber(screen.DeviceName) ?? fallbackDisplayNumber;
        var role = screen.Primary ? "Primary" : "External / TV";
        var selectorLabel = $"Display {displayNumber}: {role}";
        var statusDisplayName = screen.Primary ? "Primary Display" : $"Display {displayNumber}";

        return new ProjectionDisplayTarget(
            screen.DeviceName,
            screen.DeviceName,
            selectorLabel,
            statusDisplayName,
            screen.Primary,
            displayNumber,
            screenCount,
            screen.Bounds.Left,
            screen.Bounds.Top,
            screen.Bounds.Width,
            screen.Bounds.Height,
            screen.WorkingArea.Width,
            screen.WorkingArea.Height);
    }

    private static int? TryParseDisplayNumber(string deviceName)
    {
        var match = DisplayNumberRegex().Match(deviceName);
        return match.Success && int.TryParse(match.Groups["number"].Value, out var number)
            ? number
            : null;
    }

    [GeneratedRegex(@"DISPLAY(?<number>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DisplayNumberRegex();
}
