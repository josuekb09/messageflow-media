using MessageFlow.Core.Bible;

namespace MessageFlow.Core.Localization;

/// <summary>
/// Presentation-only Bible book names. The database, BibleBookSeed and
/// BibleReferenceParser keep their canonical English names; this maps a canonical
/// name to the name shown to the user for the active language.
/// </summary>
public static class LocalizedBibleBookNames
{
    private static readonly Dictionary<string, string> FrenchByCanonicalName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Genesis"] = "Genèse",
        ["Exodus"] = "Exode",
        ["Leviticus"] = "Lévitique",
        ["Numbers"] = "Nombres",
        ["Deuteronomy"] = "Deutéronome",
        ["Joshua"] = "Josué",
        ["Judges"] = "Juges",
        ["Ruth"] = "Ruth",
        ["1 Samuel"] = "1 Samuel",
        ["2 Samuel"] = "2 Samuel",
        ["1 Kings"] = "1 Rois",
        ["2 Kings"] = "2 Rois",
        ["1 Chronicles"] = "1 Chroniques",
        ["2 Chronicles"] = "2 Chroniques",
        ["Ezra"] = "Esdras",
        ["Nehemiah"] = "Néhémie",
        ["Esther"] = "Esther",
        ["Job"] = "Job",
        ["Psalms"] = "Psaumes",
        ["Proverbs"] = "Proverbes",
        ["Ecclesiastes"] = "Ecclésiaste",
        ["Song of Solomon"] = "Cantique des cantiques",
        ["Isaiah"] = "Ésaïe",
        ["Jeremiah"] = "Jérémie",
        ["Lamentations"] = "Lamentations",
        ["Ezekiel"] = "Ézéchiel",
        ["Daniel"] = "Daniel",
        ["Hosea"] = "Osée",
        ["Joel"] = "Joël",
        ["Amos"] = "Amos",
        ["Obadiah"] = "Abdias",
        ["Jonah"] = "Jonas",
        ["Micah"] = "Michée",
        ["Nahum"] = "Nahum",
        ["Habakkuk"] = "Habacuc",
        ["Zephaniah"] = "Sophonie",
        ["Haggai"] = "Aggée",
        ["Zechariah"] = "Zacharie",
        ["Malachi"] = "Malachie",
        ["Matthew"] = "Matthieu",
        ["Mark"] = "Marc",
        ["Luke"] = "Luc",
        ["John"] = "Jean",
        ["Acts"] = "Actes",
        ["Romans"] = "Romains",
        ["1 Corinthians"] = "1 Corinthiens",
        ["2 Corinthians"] = "2 Corinthiens",
        ["Galatians"] = "Galates",
        ["Ephesians"] = "Éphésiens",
        ["Philippians"] = "Philippiens",
        ["Colossians"] = "Colossiens",
        ["1 Thessalonians"] = "1 Thessaloniciens",
        ["2 Thessalonians"] = "2 Thessaloniciens",
        ["1 Timothy"] = "1 Timothée",
        ["2 Timothy"] = "2 Timothée",
        ["Titus"] = "Tite",
        ["Philemon"] = "Philémon",
        ["Hebrews"] = "Hébreux",
        ["James"] = "Jacques",
        ["1 Peter"] = "1 Pierre",
        ["2 Peter"] = "2 Pierre",
        ["1 John"] = "1 Jean",
        ["2 John"] = "2 Jean",
        ["3 John"] = "3 Jean",
        ["Jude"] = "Jude",
        ["Revelation"] = "Apocalypse"
    };

    private static readonly Dictionary<string, string> FrenchShortByCanonicalName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Genesis"] = "Gen",
        ["Exodus"] = "Ex",
        ["Leviticus"] = "Lév",
        ["Numbers"] = "Nom",
        ["Deuteronomy"] = "Deut",
        ["Joshua"] = "Jos",
        ["Judges"] = "Jug",
        ["Ruth"] = "Ruth",
        ["1 Samuel"] = "1 Sam",
        ["2 Samuel"] = "2 Sam",
        ["1 Kings"] = "1 Rois",
        ["2 Kings"] = "2 Rois",
        ["1 Chronicles"] = "1 Chr",
        ["2 Chronicles"] = "2 Chr",
        ["Ezra"] = "Esd",
        ["Nehemiah"] = "Néh",
        ["Esther"] = "Est",
        ["Job"] = "Job",
        ["Psalms"] = "Ps",
        ["Proverbs"] = "Prov",
        ["Ecclesiastes"] = "Ecc",
        ["Song of Solomon"] = "Cant",
        ["Isaiah"] = "És",
        ["Jeremiah"] = "Jér",
        ["Lamentations"] = "Lam",
        ["Ezekiel"] = "Éz",
        ["Daniel"] = "Dan",
        ["Hosea"] = "Os",
        ["Joel"] = "Joël",
        ["Amos"] = "Am",
        ["Obadiah"] = "Abd",
        ["Jonah"] = "Jon",
        ["Micah"] = "Mich",
        ["Nahum"] = "Nah",
        ["Habakkuk"] = "Hab",
        ["Zephaniah"] = "Soph",
        ["Haggai"] = "Ag",
        ["Zechariah"] = "Zach",
        ["Malachi"] = "Mal",
        ["Matthew"] = "Matt",
        ["Mark"] = "Marc",
        ["Luke"] = "Luc",
        ["John"] = "Jean",
        ["Acts"] = "Act",
        ["Romans"] = "Rom",
        ["1 Corinthians"] = "1 Cor",
        ["2 Corinthians"] = "2 Cor",
        ["Galatians"] = "Gal",
        ["Ephesians"] = "Éph",
        ["Philippians"] = "Phil",
        ["Colossians"] = "Col",
        ["1 Thessalonians"] = "1 Thess",
        ["2 Thessalonians"] = "2 Thess",
        ["1 Timothy"] = "1 Tim",
        ["2 Timothy"] = "2 Tim",
        ["Titus"] = "Tite",
        ["Philemon"] = "Phlm",
        ["Hebrews"] = "Héb",
        ["James"] = "Jac",
        ["1 Peter"] = "1 Pi",
        ["2 Peter"] = "2 Pi",
        ["1 John"] = "1 Jean",
        ["2 John"] = "2 Jean",
        ["3 John"] = "3 Jean",
        ["Jude"] = "Jude",
        ["Revelation"] = "Apoc"
    };

    /// <summary>
    /// Returns the book name to display for the given language, falling back to the
    /// canonical English name whenever no localized name is known.
    /// </summary>
    public static string Display(string? canonicalName, string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
        {
            return string.Empty;
        }

        var trimmed = canonicalName.Trim();
        if (IsFrench(languageCode) && FrenchByCanonicalName.TryGetValue(trimmed, out var french))
        {
            return french;
        }

        return trimmed;
    }

    public static string DisplayShort(string? canonicalName, string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
        {
            return string.Empty;
        }

        var trimmed = canonicalName.Trim();
        if (IsFrench(languageCode) && FrenchShortByCanonicalName.TryGetValue(trimmed, out var french))
        {
            return french;
        }

        var seeded = BibleBookSeed.All.FirstOrDefault(book =>
            string.Equals(book.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        return seeded?.ShortName ?? trimmed;
    }

    /// <summary>
    /// Builds a localized reference such as "Genèse 1:16" or "Genesis 1:16".
    /// </summary>
    public static string Reference(string? canonicalName, int chapter, int? verse, string? languageCode)
    {
        var book = Display(canonicalName, languageCode);
        return verse is null
            ? $"{book} {chapter}"
            : $"{book} {chapter}:{verse}";
    }

    /// <summary>
    /// Every canonical name that has no localized entry for the given language.
    /// Used by startup validation so a missing book never surfaces silently.
    /// </summary>
    public static IReadOnlyList<string> FindMissing(string? languageCode)
    {
        if (!IsFrench(languageCode))
        {
            return [];
        }

        return BibleBookSeed.All
            .Where(book => !FrenchByCanonicalName.ContainsKey(book.Name))
            .Select(book => book.Name)
            .ToList();
    }

    private static bool IsFrench(string? languageCode)
    {
        return string.Equals(languageCode, AppLanguages.French.Code, StringComparison.OrdinalIgnoreCase);
    }
}
