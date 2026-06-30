using System.Text;
using System.Text.RegularExpressions;

const string SourcePath = @"D:\Bible\Sources\kjv_gutenberg.txt";
const string OutputPath = @"D:\Bible\KJV\kjv.csv";

var converter = new KjvGutenbergConverter(SourcePath, OutputPath);
var summary = converter.Convert();

Console.WriteLine("KJV Gutenberg conversion complete.");
Console.WriteLine($"books exported: {summary.BooksExported}");
Console.WriteLine($"verses exported: {summary.VersesExported}");
Console.WriteLine($"skipped lines: {summary.SkippedLines}");
Console.WriteLine($"output path: {summary.OutputPath}");

if (summary.VersesExported == KjvGutenbergConverter.ExpectedVerseCount)
{
    Console.WriteLine($"expected verse count: {KjvGutenbergConverter.ExpectedVerseCount} (OK)");
}
else
{
    Console.WriteLine($"expected verse count: {KjvGutenbergConverter.ExpectedVerseCount} (got {summary.VersesExported})");
}

internal sealed class KjvGutenbergConverter
{
    public const int ExpectedVerseCount = 31_102;

    private static readonly string[] CanonicalBooks =
    [
        "Genesis",
        "Exodus",
        "Leviticus",
        "Numbers",
        "Deuteronomy",
        "Joshua",
        "Judges",
        "Ruth",
        "1 Samuel",
        "2 Samuel",
        "1 Kings",
        "2 Kings",
        "1 Chronicles",
        "2 Chronicles",
        "Ezra",
        "Nehemiah",
        "Esther",
        "Job",
        "Psalms",
        "Proverbs",
        "Ecclesiastes",
        "Song of Solomon",
        "Isaiah",
        "Jeremiah",
        "Lamentations",
        "Ezekiel",
        "Daniel",
        "Hosea",
        "Joel",
        "Amos",
        "Obadiah",
        "Jonah",
        "Micah",
        "Nahum",
        "Habakkuk",
        "Zephaniah",
        "Haggai",
        "Zechariah",
        "Malachi",
        "Matthew",
        "Mark",
        "Luke",
        "John",
        "Acts",
        "Romans",
        "1 Corinthians",
        "2 Corinthians",
        "Galatians",
        "Ephesians",
        "Philippians",
        "Colossians",
        "1 Thessalonians",
        "2 Thessalonians",
        "1 Timothy",
        "2 Timothy",
        "Titus",
        "Philemon",
        "Hebrews",
        "James",
        "1 Peter",
        "2 Peter",
        "1 John",
        "2 John",
        "3 John",
        "Jude",
        "Revelation"
    ];

    private static readonly Dictionary<string, string> BookHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["The First Book of Moses: Called Genesis"] = "Genesis",
        ["The Second Book of Moses: Called Exodus"] = "Exodus",
        ["The Third Book of Moses: Called Leviticus"] = "Leviticus",
        ["The Fourth Book of Moses: Called Numbers"] = "Numbers",
        ["The Fifth Book of Moses: Called Deuteronomy"] = "Deuteronomy",
        ["The Book of Joshua"] = "Joshua",
        ["The Book of Judges"] = "Judges",
        ["The Book of Ruth"] = "Ruth",
        ["The First Book of Samuel"] = "1 Samuel",
        ["The Second Book of Samuel"] = "2 Samuel",
        ["The First Book of the Kings"] = "1 Kings",
        ["The Second Book of the Kings"] = "2 Kings",
        ["The First Book of the Chronicles"] = "1 Chronicles",
        ["The Second Book of the Chronicles"] = "2 Chronicles",
        ["Ezra"] = "Ezra",
        ["The Book of Nehemiah"] = "Nehemiah",
        ["The Book of Esther"] = "Esther",
        ["The Book of Job"] = "Job",
        ["The Book of Psalms"] = "Psalms",
        ["The Proverbs"] = "Proverbs",
        ["Ecclesiastes"] = "Ecclesiastes",
        ["The Preacher"] = "Ecclesiastes",
        ["The Song of Solomon"] = "Song of Solomon",
        ["The Book of the Prophet Isaiah"] = "Isaiah",
        ["The Book of the Prophet Jeremiah"] = "Jeremiah",
        ["The Lamentations of Jeremiah"] = "Lamentations",
        ["The Book of the Prophet Ezekiel"] = "Ezekiel",
        ["The Book of Daniel"] = "Daniel",
        ["Hosea"] = "Hosea",
        ["Joel"] = "Joel",
        ["Amos"] = "Amos",
        ["Obadiah"] = "Obadiah",
        ["Jonah"] = "Jonah",
        ["Micah"] = "Micah",
        ["Nahum"] = "Nahum",
        ["Habakkuk"] = "Habakkuk",
        ["Zephaniah"] = "Zephaniah",
        ["Haggai"] = "Haggai",
        ["Zechariah"] = "Zechariah",
        ["Malachi"] = "Malachi",
        ["The Gospel According to Saint Matthew"] = "Matthew",
        ["The Gospel According to Saint Mark"] = "Mark",
        ["The Gospel According to Saint Luke"] = "Luke",
        ["The Gospel According to Saint John"] = "John",
        ["The Acts of the Apostles"] = "Acts",
        ["The Epistle of Paul the Apostle to the Romans"] = "Romans",
        ["The First Epistle of Paul the Apostle to the Corinthians"] = "1 Corinthians",
        ["The Second Epistle of Paul the Apostle to the Corinthians"] = "2 Corinthians",
        ["The Epistle of Paul the Apostle to the Galatians"] = "Galatians",
        ["The Epistle of Paul the Apostle to the Ephesians"] = "Ephesians",
        ["The Epistle of Paul the Apostle to the Philippians"] = "Philippians",
        ["The Epistle of Paul the Apostle to the Colossians"] = "Colossians",
        ["The First Epistle of Paul the Apostle to the Thessalonians"] = "1 Thessalonians",
        ["The Second Epistle of Paul the Apostle to the Thessalonians"] = "2 Thessalonians",
        ["The First Epistle of Paul the Apostle to Timothy"] = "1 Timothy",
        ["The Second Epistle of Paul the Apostle to Timothy"] = "2 Timothy",
        ["The Epistle of Paul the Apostle to Titus"] = "Titus",
        ["The Epistle of Paul the Apostle to Philemon"] = "Philemon",
        ["The Epistle of Paul the Apostle to the Hebrews"] = "Hebrews",
        ["The General Epistle of James"] = "James",
        ["The First Epistle General of Peter"] = "1 Peter",
        ["The Second General Epistle of Peter"] = "2 Peter",
        ["The First Epistle General of John"] = "1 John",
        ["The Second Epistle General of John"] = "2 John",
        ["The Third Epistle General of John"] = "3 John",
        ["The General Epistle of Jude"] = "Jude",
        ["The Revelation of Saint John the Divine"] = "Revelation"
    };

    private static readonly Dictionary<string, (int Chapter, int Verse)> LastVerseByBook = new(StringComparer.Ordinal)
    {
        ["Genesis"] = (50, 26),
        ["Exodus"] = (40, 38),
        ["Leviticus"] = (27, 34),
        ["Numbers"] = (36, 13),
        ["Deuteronomy"] = (34, 12),
        ["Joshua"] = (24, 33),
        ["Judges"] = (21, 25),
        ["Ruth"] = (4, 22),
        ["1 Samuel"] = (31, 13),
        ["2 Samuel"] = (24, 25),
        ["1 Kings"] = (22, 53),
        ["2 Kings"] = (25, 30),
        ["1 Chronicles"] = (29, 30),
        ["2 Chronicles"] = (36, 23),
        ["Ezra"] = (10, 44),
        ["Nehemiah"] = (13, 31),
        ["Esther"] = (10, 3),
        ["Job"] = (42, 17),
        ["Psalms"] = (150, 6),
        ["Proverbs"] = (31, 31),
        ["Ecclesiastes"] = (12, 14),
        ["Song of Solomon"] = (8, 14),
        ["Isaiah"] = (66, 24),
        ["Jeremiah"] = (52, 34),
        ["Lamentations"] = (5, 22),
        ["Ezekiel"] = (48, 35),
        ["Daniel"] = (12, 13),
        ["Hosea"] = (14, 9),
        ["Joel"] = (3, 21),
        ["Amos"] = (9, 15),
        ["Obadiah"] = (1, 21),
        ["Jonah"] = (4, 11),
        ["Micah"] = (7, 20),
        ["Nahum"] = (3, 19),
        ["Habakkuk"] = (3, 19),
        ["Zephaniah"] = (3, 20),
        ["Haggai"] = (2, 23),
        ["Zechariah"] = (14, 21),
        ["Malachi"] = (4, 6),
        ["Matthew"] = (28, 20),
        ["Mark"] = (16, 20),
        ["Luke"] = (24, 53),
        ["John"] = (21, 25),
        ["Acts"] = (28, 31),
        ["Romans"] = (16, 27),
        ["1 Corinthians"] = (16, 24),
        ["2 Corinthians"] = (13, 14),
        ["Galatians"] = (6, 18),
        ["Ephesians"] = (6, 24),
        ["Philippians"] = (4, 23),
        ["Colossians"] = (4, 18),
        ["1 Thessalonians"] = (5, 28),
        ["2 Thessalonians"] = (3, 18),
        ["1 Timothy"] = (6, 21),
        ["2 Timothy"] = (4, 22),
        ["Titus"] = (3, 15),
        ["Philemon"] = (1, 25),
        ["Hebrews"] = (13, 25),
        ["James"] = (5, 20),
        ["1 Peter"] = (5, 14),
        ["2 Peter"] = (3, 18),
        ["1 John"] = (5, 21),
        ["2 John"] = (1, 13),
        ["3 John"] = (1, 14),
        ["Jude"] = (1, 25),
        ["Revelation"] = (22, 21)
    };

    private static readonly Regex VerseMarkerRegex =
        new(@"(?<!\d)(\d{1,3}):(\d{1,3})(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespaceRegex =
        new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string sourcePath;
    private readonly string outputPath;

    public KjvGutenbergConverter(string sourcePath, string outputPath)
    {
        this.sourcePath = sourcePath;
        this.outputPath = outputPath;
    }

    public ConversionSummary Convert()
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("KJV Gutenberg source file was not found.", sourcePath);
        }

        var verses = new List<VerseRow>(ExpectedVerseCount);
        var seenVerseKeys = new HashSet<string>(StringComparer.Ordinal);
        PendingVerse? pendingVerse = null;
        string? currentBook = null;
        var currentBookStarted = false;
        var skippedLines = 0;
        var lineNumber = 0;
        var skippedVerseMarkers = 0;
        var skippedVerseMarkerSamples = new List<string>();

        foreach (var line in File.ReadLines(sourcePath, Encoding.UTF8))
        {
            lineNumber++;
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("*** END OF THE PROJECT GUTENBERG EBOOK", StringComparison.OrdinalIgnoreCase))
            {
                FinishPendingVerse();
                skippedLines++;
                break;
            }

            if (trimmedLine.Length == 0)
            {
                if (pendingVerse is not null && IsLastVerseOfBook(pendingVerse))
                {
                    FinishPendingVerse();
                }

                skippedLines++;
                continue;
            }

            if (BookHeadings.TryGetValue(trimmedLine, out var canonicalBook))
            {
                if (currentBook is not null && !currentBookStarted && canonicalBook != currentBook)
                {
                    skippedLines++;
                    continue;
                }

                FinishPendingVerse();
                currentBook = canonicalBook;
                currentBookStarted = false;
                skippedLines++;
                continue;
            }

            var matches = VerseMarkerRegex.Matches(line);
            if (matches.Count > 0)
            {
                if (currentBook is null)
                {
                    FinishPendingVerse();
                    currentBookStarted = false;
                    skippedVerseMarkers += matches.Count;
                    if (skippedVerseMarkerSamples.Count < 8)
                    {
                        skippedVerseMarkerSamples.Add($"{lineNumber}: {trimmedLine}");
                    }

                    skippedLines++;
                    continue;
                }

                var cursor = 0;
                foreach (Match match in matches)
                {
                    AppendToPending(line[cursor..match.Index]);
                    FinishPendingVerse();

                    pendingVerse = new PendingVerse(
                        currentBook,
                        int.Parse(match.Groups[1].Value),
                        int.Parse(match.Groups[2].Value));
                    currentBookStarted = true;

                    cursor = match.Index + match.Length;
                    while (cursor < line.Length && char.IsWhiteSpace(line[cursor]))
                    {
                        cursor++;
                    }
                }

                AppendToPending(line[cursor..]);
                continue;
            }

            if (pendingVerse is not null)
            {
                AppendToPending(line);
                continue;
            }

            if (currentBook is not null && !currentBookStarted)
            {
                skippedLines++;
                continue;
            }

            currentBook = null;
            currentBookStarted = false;
            skippedLines++;
        }

        FinishPendingVerse();
        Validate(verses, seenVerseKeys, skippedVerseMarkers, skippedVerseMarkerSamples);
        WriteCsv(verses);

        return new ConversionSummary(
            BooksExported: verses.Select(verse => verse.Book).Distinct(StringComparer.Ordinal).Count(),
            VersesExported: verses.Count,
            SkippedLines: skippedLines,
            OutputPath: outputPath);

        void AppendToPending(string text)
        {
            if (pendingVerse is null)
            {
                return;
            }

            var normalizedText = WhitespaceRegex.Replace(text.Trim(), " ");
            if (normalizedText.Length == 0)
            {
                return;
            }

            if (pendingVerse.Text.Length > 0)
            {
                pendingVerse.Text.Append(' ');
            }

            pendingVerse.Text.Append(normalizedText);
        }

        void FinishPendingVerse()
        {
            if (pendingVerse is null)
            {
                return;
            }

            var verseText = pendingVerse.Text.ToString().Trim();
            if (verseText.Length == 0)
            {
                throw new InvalidDataException(
                    $"Empty verse text at {pendingVerse.Book} {pendingVerse.Chapter}:{pendingVerse.Verse}.");
            }

            var key = $"{pendingVerse.Book}|{pendingVerse.Chapter}|{pendingVerse.Verse}";
            if (!seenVerseKeys.Add(key))
            {
                throw new InvalidDataException(
                    $"Duplicate verse found at {pendingVerse.Book} {pendingVerse.Chapter}:{pendingVerse.Verse}.");
            }

            verses.Add(new VerseRow(pendingVerse.Book, pendingVerse.Chapter, pendingVerse.Verse, verseText));
            pendingVerse = null;
        }
    }

    private static void Validate(
        List<VerseRow> verses,
        HashSet<string> seenVerseKeys,
        int skippedVerseMarkers,
        List<string> skippedVerseMarkerSamples)
    {
        var exportedBooks = verses
            .Select(verse => verse.Book)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var missingBooks = CanonicalBooks
            .Where(book => !exportedBooks.Contains(book))
            .ToArray();

        if (missingBooks.Length > 0)
        {
            throw new InvalidDataException($"Missing canonical books: {string.Join(", ", missingBooks)}");
        }

        var extraBooks = exportedBooks
            .Where(book => !CanonicalBooks.Contains(book, StringComparer.Ordinal))
            .ToArray();

        if (extraBooks.Length > 0)
        {
            throw new InvalidDataException($"Unexpected books exported: {string.Join(", ", extraBooks)}");
        }

        if (exportedBooks.Count != CanonicalBooks.Length)
        {
            throw new InvalidDataException(
                $"Expected {CanonicalBooks.Length} books, but exported {exportedBooks.Count}.");
        }

        if (verses.Count != ExpectedVerseCount)
        {
            var countsByBook = verses
                .GroupBy(verse => verse.Book)
                .Select(group => $"{group.Key}={group.Count()}")
                .ToArray();

            throw new InvalidDataException(
                $"Expected {ExpectedVerseCount} KJV verses, but exported {verses.Count}. " +
                $"Skipped verse markers: {skippedVerseMarkers}. " +
                $"Skipped marker samples: {string.Join(" | ", skippedVerseMarkerSamples)}. " +
                $"Counts by book: {string.Join("; ", countsByBook)}");
        }

        foreach (var requiredVerse in RequiredValidationVerses())
        {
            var key = $"{requiredVerse.Book}|{requiredVerse.Chapter}|{requiredVerse.Verse}";
            if (!seenVerseKeys.Contains(key))
            {
                throw new InvalidDataException(
                    $"Required validation verse missing: {requiredVerse.Book} {requiredVerse.Chapter}:{requiredVerse.Verse}");
            }
        }
    }

    private void WriteCsv(List<VerseRow> verses)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException($"Output path has no directory: {outputPath}");
        }

        Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(
            outputPath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine("book,chapter,verse,text");
        foreach (var verse in verses)
        {
            writer.Write(CsvEscape(verse.Book));
            writer.Write(',');
            writer.Write(verse.Chapter);
            writer.Write(',');
            writer.Write(verse.Verse);
            writer.Write(',');
            writer.WriteLine(CsvEscape(verse.Text));
        }
    }

    private static string CsvEscape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n', ';', '\'']) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsLastVerseOfBook(PendingVerse pendingVerse)
    {
        return LastVerseByBook.TryGetValue(pendingVerse.Book, out var lastVerse)
            && pendingVerse.Chapter == lastVerse.Chapter
            && pendingVerse.Verse == lastVerse.Verse;
    }

    private static IEnumerable<VerseReference> RequiredValidationVerses()
    {
        yield return new VerseReference("Genesis", 1, 1);
        yield return new VerseReference("Psalms", 23, 1);
        yield return new VerseReference("John", 3, 16);
        yield return new VerseReference("Romans", 8, 28);
        yield return new VerseReference("Daniel", 5, 23);
        yield return new VerseReference("Revelation", 22, 21);
    }
}

internal sealed class PendingVerse
{
    public PendingVerse(string book, int chapter, int verse)
    {
        Book = book;
        Chapter = chapter;
        Verse = verse;
    }

    public string Book { get; }

    public int Chapter { get; }

    public int Verse { get; }

    public StringBuilder Text { get; } = new();
}

internal sealed record VerseRow(string Book, int Chapter, int Verse, string Text);

internal sealed record VerseReference(string Book, int Chapter, int Verse);

internal sealed record ConversionSummary(int BooksExported, int VersesExported, int SkippedLines, string OutputPath);
