using System.Text.RegularExpressions;
using MessageFlow.Core.Bible;

namespace MessageFlow.Search;

public static partial class BibleReferenceParser
{
    private static readonly Dictionary<string, string> BookAliases = CreateBookAliases();

    public static BibleReference Parse(string value)
    {
        if (TryParse(value, out var reference))
        {
            return reference;
        }

        return new BibleReference(string.Empty, 0, null, false, "Enter a Bible reference such as John 3:16.");
    }

    public static bool TryParse(string value, out BibleReference reference)
    {
        reference = new BibleReference(string.Empty, 0, null, false, string.Empty);
        var normalized = NormalizeSpaces(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var match = ReferenceRegex().Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        var bookInput = match.Groups["book"].Value;
        if (!TryNormalizeBookName(bookInput, out var bookName))
        {
            reference = new BibleReference(string.Empty, 0, null, false, $"Unknown Bible book: {bookInput}.");
            return false;
        }

        if (!int.TryParse(match.Groups["chapter"].Value, out var chapter) || chapter <= 0)
        {
            reference = new BibleReference(bookName, 0, null, false, "Chapter must be a positive number.");
            return false;
        }

        int? verse = null;
        if (match.Groups["verse"].Success)
        {
            if (!int.TryParse(match.Groups["verse"].Value, out var parsedVerse) || parsedVerse <= 0)
            {
                reference = new BibleReference(bookName, chapter, null, false, "Verse must be a positive number.");
                return false;
            }

            verse = parsedVerse;
        }

        reference = new BibleReference(bookName, chapter, verse, true, string.Empty);
        return true;
    }

    public static bool TryNormalizeBookName(string value, out string bookName)
    {
        return BookAliases.TryGetValue(NormalizeBookKey(value), out bookName!);
    }

    public static string NormalizeBookKey(string value)
    {
        var withoutPeriods = value.Replace(".", string.Empty, StringComparison.Ordinal);
        var normalized = NormalizeSpaces(withoutPeriods).ToLowerInvariant();
        return normalized
            .Replace("first ", "1 ", StringComparison.Ordinal)
            .Replace("second ", "2 ", StringComparison.Ordinal)
            .Replace("third ", "3 ", StringComparison.Ordinal)
            .Replace("1st ", "1 ", StringComparison.Ordinal)
            .Replace("2nd ", "2 ", StringComparison.Ordinal)
            .Replace("3rd ", "3 ", StringComparison.Ordinal);
    }

    private static Dictionary<string, string> CreateBookAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var book in BibleBookSeed.All)
        {
            AddAlias(aliases, book.Name, book.Name);
            AddAlias(aliases, book.ShortName, book.Name);
        }

        AddAlias(aliases, "Deut", "Deuteronomy");
        AddAlias(aliases, "Josh", "Joshua");
        AddAlias(aliases, "Judg", "Judges");
        AddAlias(aliases, "1 Sam", "1 Samuel");
        AddAlias(aliases, "2 Sam", "2 Samuel");
        AddAlias(aliases, "1 Kgs", "1 Kings");
        AddAlias(aliases, "2 Kgs", "2 Kings");
        AddAlias(aliases, "Psalm", "Psalms");
        AddAlias(aliases, "Psalms", "Psalms");
        AddAlias(aliases, "Ps", "Psalms");
        AddAlias(aliases, "Prov", "Proverbs");
        AddAlias(aliases, "Isa", "Isaiah");
        AddAlias(aliases, "Jer", "Jeremiah");
        AddAlias(aliases, "Matt", "Matthew");
        AddAlias(aliases, "Mk", "Mark");
        AddAlias(aliases, "Mark", "Mark");
        AddAlias(aliases, "Lk", "Luke");
        AddAlias(aliases, "Luke", "Luke");
        AddAlias(aliases, "Jn", "John");
        AddAlias(aliases, "John", "John");
        AddAlias(aliases, "Rom", "Romans");
        AddAlias(aliases, "1 Cor", "1 Corinthians");
        AddAlias(aliases, "2 Cor", "2 Corinthians");
        AddAlias(aliases, "Gal", "Galatians");
        AddAlias(aliases, "Eph", "Ephesians");
        AddAlias(aliases, "Phil", "Philippians");
        AddAlias(aliases, "Col", "Colossians");
        AddAlias(aliases, "1 Thess", "1 Thessalonians");
        AddAlias(aliases, "2 Thess", "2 Thessalonians");
        AddAlias(aliases, "1 Tim", "1 Timothy");
        AddAlias(aliases, "2 Tim", "2 Timothy");
        AddAlias(aliases, "Heb", "Hebrews");
        AddAlias(aliases, "Jas", "James");
        AddAlias(aliases, "James", "James");
        AddAlias(aliases, "Rev", "Revelation");
        AddAlias(aliases, "Revelation", "Revelation");

        return aliases;
    }

    private static void AddAlias(Dictionary<string, string> aliases, string alias, string bookName)
    {
        aliases[NormalizeBookKey(alias)] = bookName;
        aliases[NormalizeBookKey(alias.Replace(" ", string.Empty, StringComparison.Ordinal))] = bookName;
    }

    private static string NormalizeSpaces(string value)
    {
        return SpaceRegex().Replace(value.Trim(), " ");
    }

    [GeneratedRegex(@"^(?<book>(?:[1-3]\s*)?[A-Za-z]+(?:\s+[A-Za-z]+)*?)\s+(?<chapter>\d{1,3})(?::(?<verse>\d{1,3}))?$")]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaceRegex();
}
