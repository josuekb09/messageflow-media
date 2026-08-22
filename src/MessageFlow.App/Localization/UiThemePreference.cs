using System.IO;
using MessageFlow.Data;

namespace MessageFlow.App.Localization;

/// <summary>
/// Persists the operator UI theme between launches, using the D: settings
/// folder shared with <see cref="UiLanguagePreference"/>. Default is dark.
/// </summary>
public static class UiThemePreference
{
    public const string Dark = "dark";
    public const string Light = "light";

    private static string SettingsPath =>
        Path.Combine(MessageFlowDatabase.UserSettingsDirectory, "ui-theme.txt");

    public static bool LoadIsLight()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return false;
            }

            var value = File.ReadAllText(SettingsPath).Trim();
            return string.Equals(value, Light, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void Save(bool isLight)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, isLight ? Light : Dark);
        }
        catch (Exception ex)
        {
            App.LogStartupError("UI theme preference could not be saved.", ex);
        }
    }
}
