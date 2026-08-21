using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using MessageFlow.Core.Localization;
using MessageFlow.Data;
using Forms = System.Windows.Forms;

namespace MessageFlow.App.ViewModels;

public static partial class ProjectionDisplayService
{
    public const string AutoPreferenceKey = "auto";

    private static string SettingsPath =>
        Path.Combine(MessageFlowDatabase.UserSettingsDirectory, "projection-display.txt");

    public static IReadOnlyList<ProjectionDisplayOption> GetDisplayOptions()
    {
        var options = new List<ProjectionDisplayOption>
        {
            new(AutoPreferenceKey, Localizer.Instance.Get("Display_Auto"), IsAuto: true)
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

    public static ProjectionDisplayTarget ResolveLiveProjectionTarget(string? preferenceKey)
    {
        var targets = GetDisplayTargets().Where(HasValidBounds).ToList();
        LogDisplayTargets(targets, preferenceKey);
        if (targets.Count == 0)
        {
            throw new InvalidOperationException("No usable Windows display was detected.");
        }

        if (targets.Count > 1 &&
            !string.IsNullOrWhiteSpace(preferenceKey) &&
            !string.Equals(preferenceKey, AutoPreferenceKey, StringComparison.OrdinalIgnoreCase))
        {
            var preferredExternal = targets.FirstOrDefault(target =>
                !target.IsPrimary &&
                string.Equals(target.PreferenceKey, preferenceKey, StringComparison.OrdinalIgnoreCase));
            if (preferredExternal is not null)
            {
                return preferredExternal;
            }
        }

        var external = targets
            .Where(target => !target.IsPrimary)
            .OrderByDescending(target => target.WorkingAreaWidth * target.WorkingAreaHeight)
            .ThenBy(target => target.DisplayNumber)
            .FirstOrDefault();
        if (external is not null)
        {
            if (!string.IsNullOrWhiteSpace(preferenceKey) &&
                !string.Equals(preferenceKey, AutoPreferenceKey, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(preferenceKey, external.PreferenceKey, StringComparison.OrdinalIgnoreCase))
            {
                App.LogStartupMessage(
                    $"Saved projection display '{preferenceKey}' is unavailable or unsafe; " +
                    $"using {external.StatusDisplayName}.");
            }

            return external;
        }

        App.LogStartupMessage("Live projection fallback: only the primary/operator display is available; using windowed mode.");
        return targets.FirstOrDefault(target => target.IsPrimary) ?? targets[0];
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
        window.WindowState = WindowState.Normal;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Topmost = true;
        ApplyTargetBounds(window, target);
    }

    public static void ConfigureAdaptiveWindowedProjection(
        Window window,
        ProjectionDisplayTarget target,
        bool preserveExistingWindowBounds,
        Rect? savedWindowedBounds = null)
    {
        var canPreserve = preserveExistingWindowBounds &&
                          window.WindowStyle == WindowStyle.SingleBorderWindow &&
                          IsWindowAtLeastPartlyVisible(window, GetDisplayTargets());

        if (!canPreserve)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Topmost = false;
        window.ShowInTaskbar = true;
        window.ShowActivated = true;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.SingleBorderWindow;
        window.ResizeMode = ResizeMode.CanResize;

        if (canPreserve)
        {
            return;
        }

        if (savedWindowedBounds is not null &&
            TryApplySavedWindowedBounds(window, target, savedWindowedBounds.Value))
        {
            return;
        }

        ApplyWindowedPreviewBounds(window, target);
    }

    public static bool ShouldUseFullscreenProjection(ProjectionDisplayTarget target)
    {
        return target.ScreenCount > 1 && !target.IsPrimary && HasValidBounds(target);
    }

    public static void PrepareWindowedPreviewWindow(Window window, ProjectionDisplayTarget target)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.SingleBorderWindow;
        window.ResizeMode = ResizeMode.CanResize;
        window.ShowInTaskbar = true;
        window.ShowActivated = true;
        window.Topmost = false;
        window.WindowState = WindowState.Normal;
        ApplyWindowedPreviewBounds(window, target);
    }

    public static void MaximizeOnTarget(Window window, ProjectionDisplayTarget target)
    {
        window.WindowState = WindowState.Normal;
        ApplyTargetBounds(window, target);
        window.Topmost = true;
    }

    public static void BringWindowedProjectionToFront(Window window)
    {
        window.Topmost = false;
        window.ShowActivated = true;
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Focus();
        window.Topmost = true;
        window.Topmost = false;
        window.Activate();
    }

    public static void ShowWindowedPreviewOnTarget(Window window, ProjectionDisplayTarget target)
    {
        window.Topmost = false;
        window.WindowState = WindowState.Normal;
        ApplyWindowedPreviewBounds(window, target);
        window.Activate();
        window.Focus();
    }

    public static bool ShouldUseWindowedPreview(ProjectionDisplayTarget target)
    {
        return target.ScreenCount <= 1;
    }

    private static void ApplyTargetBounds(Window window, ProjectionDisplayTarget target)
    {
        window.Left = ToWpfX(target.Left, target);
        window.Top = ToWpfY(target.Top, target);
        window.Width = ToWpfX(target.Width, target);
        window.Height = ToWpfY(target.Height, target);
    }

    private static void ApplyWindowedPreviewBounds(Window window, ProjectionDisplayTarget target)
    {
        var maximumWidth = Math.Max(640, target.WorkingAreaWidth);
        var maximumHeight = Math.Max(420, target.WorkingAreaHeight);
        var minimumWidth = Math.Min(860, maximumWidth);
        var minimumHeight = Math.Min(560, maximumHeight);
        var width = Math.Clamp(target.WorkingAreaWidth * 0.88, minimumWidth, maximumWidth);
        var height = Math.Clamp(target.WorkingAreaHeight * 0.84, minimumHeight, maximumHeight);

        window.Left = ToWpfX(target.WorkingAreaLeft + ((target.WorkingAreaWidth - width) / 2), target);
        window.Top = ToWpfY(target.WorkingAreaTop + ((target.WorkingAreaHeight - height) / 2), target);
        window.Width = ToWpfX(width, target);
        window.Height = ToWpfY(height, target);
    }

    private static bool TryApplySavedWindowedBounds(
        Window window,
        ProjectionDisplayTarget target,
        Rect savedBounds)
    {
        var workLeft = ToWpfX(target.WorkingAreaLeft, target);
        var workTop = ToWpfY(target.WorkingAreaTop, target);
        var workWidth = ToWpfX(target.WorkingAreaWidth, target);
        var workHeight = ToWpfY(target.WorkingAreaHeight, target);

        if (workWidth <= 0 ||
            workHeight <= 0 ||
            savedBounds.Width < 360 ||
            savedBounds.Height < 300)
        {
            return false;
        }

        var width = Math.Clamp(savedBounds.Width, Math.Min(360, workWidth), workWidth);
        var height = Math.Clamp(savedBounds.Height, Math.Min(300, workHeight), workHeight);
        var maxLeft = workLeft + workWidth - width;
        var maxTop = workTop + workHeight - height;

        window.Left = Math.Clamp(savedBounds.Left, workLeft, Math.Max(workLeft, maxLeft));
        window.Top = Math.Clamp(savedBounds.Top, workTop, Math.Max(workTop, maxTop));
        window.Width = width;
        window.Height = height;
        return true;
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
        var role = screen.Primary
            ? Localizer.Instance.Get("Display_Primary")
            : Localizer.Instance.Get("Display_Secondary");
        var selectorLabel = Localizer.Instance.Format(
            "Display_Selector",
            displayNumber,
            role,
            screen.Bounds.Width,
            screen.Bounds.Height);
        var statusDisplayName = screen.Primary
            ? Localizer.Instance.Format("Display_StatusPrimary", displayNumber)
            : Localizer.Instance.Format("Display_StatusSecondary", displayNumber);

        var (dpiX, dpiY) = GetEffectiveDpi(screen);
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
            screen.WorkingArea.Left,
            screen.WorkingArea.Top,
            screen.WorkingArea.Width,
            screen.WorkingArea.Height,
            dpiX,
            dpiY);
    }

    private static bool HasValidBounds(ProjectionDisplayTarget target)
    {
        return target.Width > 0 && target.Height > 0 &&
               target.WorkingAreaWidth > 0 && target.WorkingAreaHeight > 0;
    }

    private static void LogDisplayTargets(
        IReadOnlyCollection<ProjectionDisplayTarget> targets,
        string? preferenceKey)
    {
        var details = string.Join(
            Environment.NewLine,
            targets.Select(target =>
                $"{target.DeviceName}: primary={target.IsPrimary}, bounds={target.BoundsDisplay}, working={target.WorkingAreaWidth:0}x{target.WorkingAreaHeight:0}"));

        App.LogStartupMessage(
            $"Projection displays enumerated: {targets.Count:N0}. Selected preference: {preferenceKey ?? AutoPreferenceKey}." +
            (string.IsNullOrWhiteSpace(details) ? string.Empty : $"{Environment.NewLine}{details}"));
    }

    private static bool IsWindowAtLeastPartlyVisible(
        Window window,
        IReadOnlyCollection<ProjectionDisplayTarget> targets)
    {
        var left = window.RestoreBounds.Left;
        var top = window.RestoreBounds.Top;
        var right = left + window.RestoreBounds.Width;
        var bottom = top + window.RestoreBounds.Height;

        return targets.Any(target =>
            right > ToWpfX(target.WorkingAreaLeft, target) &&
            left < ToWpfX(target.WorkingAreaLeft + target.WorkingAreaWidth, target) &&
            bottom > ToWpfY(target.WorkingAreaTop, target) &&
            top < ToWpfY(target.WorkingAreaTop + target.WorkingAreaHeight, target));
    }

    private static double ToWpfX(double pixels, ProjectionDisplayTarget target) =>
        pixels * 96d / Math.Max(96d, target.DpiX);

    private static double ToWpfY(double pixels, ProjectionDisplayTarget target) =>
        pixels * 96d / Math.Max(96d, target.DpiY);

    private static (double DpiX, double DpiY) GetEffectiveDpi(Forms.Screen screen)
    {
        try
        {
            var point = new NativePoint(
                screen.Bounds.Left + (screen.Bounds.Width / 2),
                screen.Bounds.Top + (screen.Bounds.Height / 2));
            var monitor = MonitorFromPoint(point, 2);
            if (monitor != IntPtr.Zero &&
                GetDpiForMonitor(monitor, 0, out var dpiX, out var dpiY) == 0)
            {
                return (dpiX, dpiY);
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        return (96d, 96d);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        IntPtr monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

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
