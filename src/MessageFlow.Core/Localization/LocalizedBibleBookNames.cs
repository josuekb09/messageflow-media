using System.Globalization;
using System.Text;
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

    // Display names follow the SWHULB USFM \h headings, with compact numbered
    // forms such as "1Yohana 1" / "1Wafalme" normalized to spaced names.
    private static readonly Dictionary<string, string> SwahiliByCanonicalName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Genesis"] = "Mwanzo",
        ["Exodus"] = "Kutoka",
        ["Leviticus"] = "Mambo ya Walawi",
        ["Numbers"] = "Hesabu",
        ["Deuteronomy"] = "Kumbukumbu la Torati",
        ["Joshua"] = "Yoshua",
        ["Judges"] = "Waamuzi",
        ["Ruth"] = "Ruth",
        ["1 Samuel"] = "1 Samweli",
        ["2 Samuel"] = "2 Samweli",
        ["1 Kings"] = "1 Wafalme",
        ["2 Kings"] = "2 Wafalme",
        ["1 Chronicles"] = "1 Mambo ya Nyakati",
        ["2 Chronicles"] = "2 Mambo ya Nyakati",
        ["Ezra"] = "Ezra",
        ["Nehemiah"] = "Nehemia",
        ["Esther"] = "Esta",
        ["Job"] = "Ayubu",
        ["Psalms"] = "Zaburi",
        ["Proverbs"] = "Mithali",
        ["Ecclesiastes"] = "Mhubiri",
        ["Song of Solomon"] = "Wimbo wa Sulemani",
        ["Isaiah"] = "Isaya",
        ["Jeremiah"] = "Yeremia",
        ["Lamentations"] = "Maombolezo",
        ["Ezekiel"] = "Ezekieli",
        ["Daniel"] = "Danieli",
        ["Hosea"] = "Hosea",
        ["Joel"] = "Joeli",
        ["Amos"] = "Amosi",
        ["Obadiah"] = "Obadia",
        ["Jonah"] = "Yona",
        ["Micah"] = "Mika",
        ["Nahum"] = "Nahumu",
        ["Habakkuk"] = "Habakuki",
        ["Zephaniah"] = "Sefania",
        ["Haggai"] = "Hagai",
        ["Zechariah"] = "Zekaria",
        ["Malachi"] = "Malaki",
        ["Matthew"] = "Mathayo",
        ["Mark"] = "Marko",
        ["Luke"] = "Luka",
        ["John"] = "Yohana",
        ["Acts"] = "Matendo ya Mitume",
        ["Romans"] = "Warumi",
        ["1 Corinthians"] = "1 Wakorintho",
        ["2 Corinthians"] = "2 Wakorintho",
        ["Galatians"] = "Wagalatia",
        ["Ephesians"] = "Waefeso",
        ["Philippians"] = "Wafilipi",
        ["Colossians"] = "Wakolosai",
        ["1 Thessalonians"] = "1 Wathesalonike",
        ["2 Thessalonians"] = "2 Wathesalonike",
        ["1 Timothy"] = "1 Timotheo",
        ["2 Timothy"] = "2 Timotheo",
        ["Titus"] = "Tito",
        ["Philemon"] = "Filemoni",
        ["Hebrews"] = "Wahebrania",
        ["James"] = "Waraka wa Yakobo",
        ["1 Peter"] = "1 Petro",
        ["2 Peter"] = "2 Petro",
        ["1 John"] = "1 Yohana",
        ["2 John"] = "2 Yohana",
        ["3 John"] = "3 Yohana",
        ["Jude"] = "Yuda",
        ["Revelation"] = "Ufunuo"
    };

    private static readonly Dictionary<string, string> SwahiliShortByCanonicalName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Genesis"] = "Mwa",
        ["Exodus"] = "Kut",
        ["Leviticus"] = "Wal",
        ["Numbers"] = "Hes",
        ["Deuteronomy"] = "Kum",
        ["Joshua"] = "Yos",
        ["Judges"] = "Waam",
        ["Ruth"] = "Rut",
        ["1 Samuel"] = "1 Sam",
        ["2 Samuel"] = "2 Sam",
        ["1 Kings"] = "1 Fal",
        ["2 Kings"] = "2 Fal",
        ["1 Chronicles"] = "1 Nya",
        ["2 Chronicles"] = "2 Nya",
        ["Ezra"] = "Ezr",
        ["Nehemiah"] = "Neh",
        ["Esther"] = "Est",
        ["Job"] = "Ayu",
        ["Psalms"] = "Zab",
        ["Proverbs"] = "Mit",
        ["Ecclesiastes"] = "Mhu",
        ["Song of Solomon"] = "Wim",
        ["Isaiah"] = "Isa",
        ["Jeremiah"] = "Yer",
        ["Lamentations"] = "Mao",
        ["Ezekiel"] = "Eze",
        ["Daniel"] = "Dan",
        ["Hosea"] = "Hos",
        ["Joel"] = "Joe",
        ["Amos"] = "Amo",
        ["Obadiah"] = "Oba",
        ["Jonah"] = "Yon",
        ["Micah"] = "Mik",
        ["Nahum"] = "Nah",
        ["Habakkuk"] = "Hab",
        ["Zephaniah"] = "Sef",
        ["Haggai"] = "Hag",
        ["Zechariah"] = "Zek",
        ["Malachi"] = "Mal",
        ["Matthew"] = "Mat",
        ["Mark"] = "Mar",
        ["Luke"] = "Luk",
        ["John"] = "Yoh",
        ["Acts"] = "Mdo",
        ["Romans"] = "Rum",
        ["1 Corinthians"] = "1 Kor",
        ["2 Corinthians"] = "2 Kor",
        ["Galatians"] = "Gal",
        ["Ephesians"] = "Efe",
        ["Philippians"] = "Flp",
        ["Colossians"] = "Kol",
        ["1 Thessalonians"] = "1 The",
        ["2 Thessalonians"] = "2 The",
        ["1 Timothy"] = "1 Tim",
        ["2 Timothy"] = "2 Tim",
        ["Titus"] = "Tit",
        ["Philemon"] = "Flm",
        ["Hebrews"] = "Heb",
        ["James"] = "Yak",
        ["1 Peter"] = "1 Pet",
        ["2 Peter"] = "2 Pet",
        ["1 John"] = "1 Yoh",
        ["2 John"] = "2 Yoh",
        ["3 John"] = "3 Yoh",
        ["Jude"] = "Yud",
        ["Revelation"] = "Ufu"
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
        var table = DisplayTableFor(languageCode);
        if (table is not null && table.TryGetValue(trimmed, out var localized))
        {
            return localized;
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
        var table = ShortTableFor(languageCode);
        if (table is not null && table.TryGetValue(trimmed, out var localized))
        {
            return localized;
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
        var table = DisplayTableFor(languageCode);
        if (table is null)
        {
            return [];
        }

        return BibleBookSeed.All
            .Where(book => !table.ContainsKey(book.Name))
            .Select(book => book.Name)
            .ToList();
    }

    /// <summary>
    /// French book names and common aliases that resolve to canonical English
    /// names in <c>BibleReferenceParser</c>. Canonical storage is unchanged.
    /// ASCII foldings (Genese, Esaie) are included so typed input without accents still matches.
    /// </summary>
    public static IEnumerable<(string Alias, string Canonical)> FrenchParserAliases()
    {
        foreach (var pair in FrenchByCanonicalName.Concat(FrenchShortByCanonicalName))
        {
            yield return (pair.Value, pair.Key);
            var folded = FoldLatinAccents(pair.Value);
            if (!string.Equals(folded, pair.Value, StringComparison.OrdinalIgnoreCase))
            {
                yield return (folded, pair.Key);
            }
        }
    }

    /// <summary>
    /// Swahili book names and common aliases that resolve to canonical English
    /// names in <c>BibleReferenceParser</c>. Canonical storage is unchanged.
    /// </summary>
    public static IEnumerable<(string Alias, string Canonical)> SwahiliParserAliases()
    {
        foreach (var pair in SwahiliByCanonicalName)
        {
            yield return (pair.Value, pair.Key);
        }

        foreach (var pair in SwahiliShortByCanonicalName)
        {
            yield return (pair.Value, pair.Key);
        }

        yield return ("Walawi", "Leviticus");
        yield return ("Torati", "Deuteronomy");
        yield return ("Yakobo", "James");
        yield return ("Matendo", "Acts");
        yield return ("Mitume", "Acts");
        yield return ("1 Nyakati", "1 Chronicles");
        yield return ("2 Nyakati", "2 Chronicles");
        yield return ("Wimbo", "Song of Solomon");
        yield return ("1Samweli", "1 Samuel");
        yield return ("1Wafalme", "1 Kings");
        yield return ("1Timotheo", "1 Timothy");
        yield return ("1Yohana", "1 John");
        yield return ("1Yohana 1", "1 John");
    }

    private static IReadOnlyDictionary<string, string>? DisplayTableFor(string? languageCode)
    {
        if (IsFrench(languageCode))
        {
            return FrenchByCanonicalName;
        }

        if (IsSwahili(languageCode))
        {
            return SwahiliByCanonicalName;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string>? ShortTableFor(string? languageCode)
    {
        if (IsFrench(languageCode))
        {
            return FrenchShortByCanonicalName;
        }

        if (IsSwahili(languageCode))
        {
            return SwahiliShortByCanonicalName;
        }

        return null;
    }

    private static string FoldLatinAccents(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsFrench(string? languageCode)
    {
        return string.Equals(languageCode, AppLanguages.French.Code, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSwahili(string? languageCode)
    {
        return string.Equals(languageCode, AppLanguages.Swahili.Code, StringComparison.OrdinalIgnoreCase);
    }
}
