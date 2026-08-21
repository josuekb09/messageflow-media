namespace MessageFlow.Core.Localization;

/// <summary>
/// Describes one language the application can present, together with the content
/// selectors that language implies. Adding a language means adding one entry here
/// plus one string table; no other code needs to change.
/// </summary>
/// <param name="Code">Stable identifier persisted in the user preference file.</param>
/// <param name="NativeName">Name shown in the language selector, in its own language.</param>
/// <param name="ContentLanguageCode">Matches Sermon.Language and Song.Language values.</param>
/// <param name="BibleLanguageName">Matches BibleTranslation.Language values.</param>
/// <param name="PreferredBibleAbbreviation">Translation preferred when this language is active.</param>
public sealed record AppLanguage(
    string Code,
    string NativeName,
    string ContentLanguageCode,
    string BibleLanguageName,
    string PreferredBibleAbbreviation)
{
    public override string ToString()
    {
        return NativeName;
    }
}

public static class AppLanguages
{
    public static AppLanguage English { get; } = new(
        Code: "en",
        NativeName: "English",
        ContentLanguageCode: "en",
        BibleLanguageName: "English",
        PreferredBibleAbbreviation: "KJV");

    public static AppLanguage French { get; } = new(
        Code: "fr",
        NativeName: "Français",
        ContentLanguageCode: "fr",
        BibleLanguageName: "French",
        PreferredBibleAbbreviation: "LSG");

    public static AppLanguage Swahili { get; } = new(
        Code: "sw",
        NativeName: "Kiswahili",
        ContentLanguageCode: "sw",
        BibleLanguageName: "Swahili",
        PreferredBibleAbbreviation: "SWHULB");

    public static AppLanguage Default => English;

    public static IReadOnlyList<AppLanguage> All { get; } = [English, French, Swahili];

    public static AppLanguage FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Default;
        }

        var trimmed = code.Trim();
        return All.FirstOrDefault(language =>
                   string.Equals(language.Code, trimmed, StringComparison.OrdinalIgnoreCase)) ??
               Default;
    }
}
