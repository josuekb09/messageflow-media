using System.ComponentModel;
using System.Globalization;

namespace MessageFlow.Core.Localization;

/// <summary>
/// Single application-wide UI language state and string lookup.
///
/// WPF binds to the <c>Item[]</c> indexer through the Tr markup extension, so raising
/// PropertyChanged for "Item[]" when the language changes refreshes every bound string
/// in place. That is what allows switching language without restarting the application.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    private static readonly Lazy<Localizer> LazyInstance = new(() => new Localizer());

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> tablesByLanguageCode;
    private readonly HashSet<string> missingKeys = new(StringComparer.Ordinal);

    private Localizer()
    {
        tablesByLanguageCode = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [AppLanguages.English.Code] = UiStringsEnglish.Values,
            [AppLanguages.French.Code] = UiStringsFrench.Values,
            [AppLanguages.Swahili.Code] = UiStringsSwahili.Values
        };

        CurrentLanguage = AppLanguages.Default;
    }

    public static Localizer Instance => LazyInstance.Value;

    public AppLanguage CurrentLanguage { get; private set; }

    public string LanguageCode => CurrentLanguage.Code;

    public bool IsFrench => CurrentLanguage == AppLanguages.French;

    public IReadOnlyList<AppLanguage> AvailableLanguages => AppLanguages.All;

    /// <summary>Keys requested at runtime that had no entry in any table.</summary>
    public IReadOnlyCollection<string> MissingKeys => missingKeys;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<AppLanguage>? LanguageChanged;

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (tablesByLanguageCode.TryGetValue(CurrentLanguage.Code, out var table) &&
            table.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fall back to English so a missing translation degrades to a real word
        // rather than a placeholder in front of a congregation.
        if (tablesByLanguageCode.TryGetValue(AppLanguages.English.Code, out var english) &&
            english.TryGetValue(key, out var fallback))
        {
            missingKeys.Add($"{CurrentLanguage.Code}:{key}");
            return fallback;
        }

        missingKeys.Add($"*:{key}");
        return key;
    }

    public string Format(string key, params object?[] arguments)
    {
        var format = Get(key);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    /// <summary>
    /// Localized count label, for example "3 sermons" / "3 sermons" or
    /// "1 Bible result" / "1 résultat biblique".
    /// </summary>
    public string Count(int count, string singularKey, string pluralKey)
    {
        var noun = Get(count == 1 ? singularKey : pluralKey);
        return $"{count.ToString("N0", CultureInfo.CurrentCulture)} {noun}";
    }

    /// <summary>Localized Bible book name for a canonical English book name.</summary>
    public string BookName(string? canonicalBookName)
    {
        return LocalizedBibleBookNames.Display(canonicalBookName, CurrentLanguage.Code);
    }

    /// <summary>Localized reference such as "Genèse 1:16".</summary>
    public string BookReference(string? canonicalBookName, int chapter, int? verse = null)
    {
        return LocalizedBibleBookNames.Reference(canonicalBookName, chapter, verse, CurrentLanguage.Code);
    }

    public void SetLanguage(AppLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;

        // "Item[]" refreshes every indexer binding created by the Tr markup extension.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFrench)));
        LanguageChanged?.Invoke(this, language);
    }

    public void SetLanguage(string? languageCode)
    {
        SetLanguage(AppLanguages.FromCode(languageCode));
    }

    /// <summary>
    /// Keys present in English but absent from the given language table, plus any
    /// Bible book without a localized name. Used by startup validation and by the
    /// localization coverage check so gaps are reported instead of shipped silently.
    /// </summary>
    public IReadOnlyList<string> FindUntranslated(string languageCode)
    {
        if (!tablesByLanguageCode.TryGetValue(languageCode, out var table))
        {
            return ["(no string table registered)"];
        }

        var english = tablesByLanguageCode[AppLanguages.English.Code];
        var gaps = english.Keys
            .Where(key => !table.ContainsKey(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        gaps.AddRange(LocalizedBibleBookNames
            .FindMissing(languageCode)
            .Select(book => $"BibleBook:{book}"));

        return gaps;
    }

    public int StringCount(string languageCode)
    {
        return tablesByLanguageCode.TryGetValue(languageCode, out var table) ? table.Count : 0;
    }
}
