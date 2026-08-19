using System.IO;
using MessageFlow.Core.Localization;

namespace MessageFlow.App.Localization;

/// <summary>
/// Persists the chosen UI language between launches, using the same
/// %LocalAppData%\MessageFlow file convention as ProjectionDisplayService.
/// No database table and no migration are involved.
/// </summary>
public static class UiLanguagePreference
{
    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MessageFlow",
            "ui-language.txt");

    public static AppLanguage Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? AppLanguages.FromCode(File.ReadAllText(SettingsPath).Trim())
                : AppLanguages.Default;
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
