using System.IO;
using MessageFlow.Core.Localization;
using MessageFlow.Data;

namespace MessageFlow.App.Localization;

/// <summary>
/// Persists the chosen UI language between launches, using the D: settings
/// folder shared with ProjectionDisplayService. Never writes to %LocalAppData% on C:.
/// </summary>
public static class UiLanguagePreference
{
    private static string SettingsPath =>
        Path.Combine(MessageFlowDatabase.UserSettingsDirectory, "ui-language.txt");

    public static AppLanguage Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return AppLanguages.FromCode(File.ReadAllText(SettingsPath).Trim());
            }

            // A fresh install opens in English. The old %LOCALAPPDATA%\MessageFlow\ui-language.txt
            // of an earlier version is deliberately not read: on a reused PC it opened a new
            // install in whatever language someone last picked there.
            return AppLanguages.Default;
        }
        catch
        {
            return AppLanguages.Default;
        }
    }

    public static void Save(AppLanguage language)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, language.Code);
        }
        catch (Exception ex)
        {
            App.LogStartupError("UI language preference could not be saved.", ex);
        }
    }
}
