using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

const string SourceDirectory = @"D:\MessageFlow-Bible-Sources\LSG1910";
const string OutputPath = @"D:\Bible\LSG\lsg.csv";

var converter = new LsgReadConverter(SourceDirectory, OutputPath);
var summary = converter.Convert();

Console.WriteLine("Louis Segond 1910 conversion complete.");
Console.WriteLine($"chapter files processed: {summary.ChapterFilesProcessed}");
Console.WriteLine($"non chapter files skipped: {summary.SkippedFiles}");
Console.WriteLine($"physical lines read: {summary.PhysicalLinesRead}");
Console.WriteLine($"header lines discarded: {summary.HeaderLinesDiscarded}");
Console.WriteLine($"blank lines skipped: {summary.BlankLinesSkipped}");
Console.WriteLine($"books exported: {summary.BooksExported}");
Console.WriteLine($"verses exported: {summary.VersesExported}");
Console.WriteLine($"output path: {summary.OutputPath}");

if (summary.VersesExported == LsgReadConverter.ExpectedVerseCount)
{
    Console.WriteLine($"expected verse count: {LsgReadConverter.ExpectedVerseCount} (OK)");
}
else
{
    Console.WriteLine($"expected verse count: {LsgReadConverter.ExpectedVerseCount} (got {summary.VersesExported})");
}

internal sealed class LsgReadConverter
{
    public const int ExpectedVerseCount = 31_170;

    public const int ExpectedChapterFileCount = 1_189;

    public const int ExpectedBookCount = 66;

    private const int HeaderLineCount = 2;

    private const string FrontMatterSequence = "000";

    private const string LsgFileNamePrefix = "fraLSG";

    // Canonical book order, USFM code from the eBible fraLSG file names, and the
    // verse totals established by the read-only source investigation. LSG follows
    // Hebrew/Masoretic versification, so these totals intentionally differ from KJV
    // for Psalms, Mark, Acts, 2 Corinthians, 3 John, Revelation, 1 Samuel, 1 Kings,
    // Job and Isaiah. They must not be normalised to the KJV numbering.
    private static readonly BookDefinition[] Books =
    [
        new("GEN", "Genesis", 50, 1533),
        new("EXO", "Exodus", 40, 1213),
        new("LEV", "Leviticus", 27, 859),
        new("NUM", "Numbers", 36, 1288),
        new("DEU", "Deuteronomy", 34, 959),
        new("JOS", "Joshua", 24, 658),
        new("JDG", "Judges", 21, 618),
        new("RUT", "Ruth", 4, 85),
        new("1SA", "1 Samuel", 31, 811),
        new("2SA", "2 Samuel", 24, 695),
        new("1KI", "1 Kings", 22, 817),
        new("2KI", "2 Kings", 25, 719),
        new("1CH", "1 Chronicles", 29, 942),
        new("2CH", "2 Chronicles", 36, 822),
        new("EZR", "Ezra", 10, 280),
        new("NEH", "Nehemiah", 13, 406),
        new("EST", "Esther", 10, 167),
        new("JOB", "Job", 42, 1069),
        new("PSA", "Psalms", 150, 2527),
        new("PRO", "Proverbs", 31, 915),
        new("ECC", "Ecclesiastes", 12, 222),
        new("SNG", "Song of Solomon", 8, 117),
        new("ISA", "Isaiah", 66, 1291),
        new("JER", "Jeremiah", 52, 1364),
        new("LAM", "Lamentations", 5, 154),
        new("EZK", "Ezekiel", 48, 1273),
        new("DAN", "Daniel", 12, 357),
        new("HOS", "Hosea", 14, 197),
        new("JOL", "Joel", 3, 73),
        new("AMO", "Amos", 9, 146),
        new("OBA", "Obadiah", 1, 21),
        new("JON", "Jonah", 4, 48),
        new("MIC", "Micah", 7, 105),
        new("NAM", "Nahum", 3, 47),
        new("HAB", "Habakkuk", 3, 56),
        new("ZEP", "Zephaniah", 3, 53),
        new("HAG", "Haggai", 2, 38),
        new("ZEC", "Zechariah", 14, 211),
        new("MAL", "Malachi", 4, 55),
        new("MAT", "Matthew", 28, 1071),
        new("MRK", "Mark", 16, 680),
        new("LUK", "Luke", 24, 1151),
        new("JHN", "John", 21, 879),
        new("ACT", "Acts", 28, 1006),
        new("ROM", "Romans", 16, 433),
        new("1CO", "1 Corinthians", 16, 437),
        new("2CO", "2 Corinthians", 13, 256),
        new("GAL", "Galatians", 6, 149),
        new("EPH", "Ephesians", 6, 155),
        new("PHP", "Philippians", 4, 104),
        new("COL", "Colossians", 4, 95),
        new("1TH", "1 Thessalonians", 5, 89),
        new("2TH", "2 Thessalonians", 3, 47),
        new("1TI", "1 Timothy", 6, 113),
        new("2TI", "2 Timothy", 4, 83),
        new("TIT", "Titus", 3, 46),
        new("PHM", "Philemon", 1, 25),
        new("HEB", "Hebrews", 13, 303),
        new("JAS", "James", 5, 108),
        new("1PE", "1 Peter", 5, 105),
        new("2PE", "2 Peter", 3, 61),
        new("1JN", "1 John", 5, 105),
        new("2JN", "2 John", 1, 13),
        new("3JN", "3 John", 1, 15),
        new("JUD", "Jude", 1, 25),
        new("REV", "Revelation", 22, 405)
    ];

    private static readonly Dictionary<string, BookDefinition> BooksByCode =
        Books.ToDictionary(book => book.Code, StringComparer.Ordinal);

    private static readonly Dictionary<string, BookDefinition> BooksByName =
        Books.ToDictionary(book => book.Name, StringComparer.Ordinal);

    private static readonly Dictionary<string, int> BookOrderByName =
        Books.Select((book, index) => (book.Name, index))
            .ToDictionary(entry => entry.Name, entry => entry.index, StringComparer.Ordinal);

    private static readonly Regex ChapterFileNameRegex =
        new(@"^fraLSG_(\d{3})_([A-Z0-9]{3})_(\d+)_read\.txt$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChapterHeaderRegex =
        new(@"^(\d+)\.$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string sourceDirectory;
    private readonly string outputPath;

    public LsgReadConverter(string sourceDirectory, string outputPath)
    {
        this.sourceDirectory = sourceDirectory;
        this.outputPath = outputPath;
    }

    public ConversionSummary Convert()
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"LSG 1910 source directory was not found: {sourceDirectory}");
        }

        var verses = new List<VerseRow>(ExpectedVerseCount);
        var seenVerseKeys = new HashSet<string>(StringComparer.Ordinal);
        var chaptersByBook = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        var chapterFilesProcessed = 0;
        var skippedFiles = 0;
        var physicalLinesRead = 0;
        var headerLinesDiscarded = 0;
        var blankLinesSkipped = 0;

        var files = Directory.GetFiles(sourceDirectory)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();

        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            var match = ChapterFileNameRegex.Match(fileName);

            if (!match.Success)
            {
                // A file that advertises itself as part of this corpus but does not match the
                // expected chapter layout is a hard error, never a silent skip.
                if (fileName.StartsWith(LsgFileNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"File '{fileName}' resembles an LSG chapter file but does not match the expected " +
                        $"pattern 'fraLSG_<seq3>_<CODE3>_<chapter>_read.txt'.");
                }

                skippedFiles++;
                continue;
            }

            var sequence = match.Groups[1].Value;
            var bookCode = match.Groups[2].Value;
            var chapterText = match.Groups[3].Value;

            if (string.Equals(sequence, FrontMatterSequence, StringComparison.Ordinal))
            {
                skippedFiles++;
                continue;
            }

            if (!BooksByCode.TryGetValue(bookCode, out var book))
            {
                throw new InvalidDataException(
                    $"File '{fileName}' uses USFM book code '{bookCode}', which is not part of the " +
                    $"fixed {ExpectedBookCount} book mapping.");
            }

            if (!int.TryParse(chapterText, NumberStyles.None, CultureInfo.InvariantCulture, out var chapter)
                || chapter <= 0)
            {
                throw new InvalidDataException($"File '{fileName}' has an invalid chapter number '{chapterText}'.");
            }

            if (chapter > book.ChapterCount)
            {
                throw new InvalidDataException(
                    $"File '{fileName}' declares chapter {chapter}, but {book.Name} has only " +
                    $"{book.ChapterCount} chapters.");
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            physicalLinesRead += lines.Length;

            if (lines.Length <= HeaderLineCount)
            {
                throw new InvalidDataException(
                    $"File '{fileName}' has {lines.Length} lines; expected a book heading, a chapter " +
                    $"header and at least one verse line.");
            }

            headerLinesDiscarded += HeaderLineCount;

            // lines[0] is the French book heading. Its wording and casing vary between the first
            // chapter of a book and the rest, so it is discarded and never used for identification.
            var chapterHeader = lines[1].Trim().Trim('\uFEFF').Trim();
            var headerMatch = ChapterHeaderRegex.Match(chapterHeader);
            if (!headerMatch.Success)
            {
                throw new InvalidDataException(
                    $"File '{fileName}' line 2 is '{chapterHeader}'; expected a chapter header such as '{chapter}.'.");
            }

            var headerChapter = int.Parse(headerMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (headerChapter != chapter)
            {
                throw new InvalidDataException(
                    $"File '{fileName}' declares chapter {chapter} in its name but its chapter header " +
                    $"says {headerChapter}.");
            }

            var verseNumber = 0;
            var blankLinesInFile = 0;

            for (var index = HeaderLineCount; index < lines.Length; index++)
            {
                var text = lines[index].Trim();

                if (text.Length == 0)
                {
                    blankLinesInFile++;
                    continue;
                }

                if (blankLinesInFile > 0)
                {
                    throw new InvalidDataException(
                        $"File '{fileName}' has a blank line inside the chapter body before physical " +
                        $"line {index + 1}; verse numbering by line position is unsafe.");
                }

                verseNumber++;

                var key = $"{book.Name}|{chapter}|{verseNumber}";
                if (!seenVerseKeys.Add(key))
                {
                    throw new InvalidDataException(
                        $"Duplicate verse found at {book.Name} {chapter}:{verseNumber} while reading '{fileName}'.");
                }

                verses.Add(new VerseRow(book.Name, chapter, verseNumber, text));
            }

            blankLinesSkipped += blankLinesInFile;

            if (lines.Length - HeaderLineCount - blankLinesInFile != verseNumber)
            {
                throw new InvalidDataException(
                    $"File '{fileName}' has {lines.Length} physical lines and {blankLinesInFile} blank " +
                    $"lines, which does not reconcile with {verseNumber} emitted verses.");
            }

            if (verseNumber == 0)
            {
                throw new InvalidDataException($"File '{fileName}' produced no verses.");
            }

            if (!chaptersByBook.TryGetValue(book.Name, out var chapters))
            {
                chapters = [];
                chaptersByBook[book.Name] = chapters;
            }

            if (!chapters.Add(chapter))
            {
                throw new InvalidDataException($"Chapter {book.Name} {chapter} was read more than once.");
            }

            chapterFilesProcessed++;
        }

        Validate(verses, chaptersByBook, chapterFilesProcessed);

        verses.Sort(CompareVerses);
        WriteCsv(verses);

        return new ConversionSummary(
            ChapterFilesProcessed: chapterFilesProcessed,
            SkippedFiles: skippedFiles,
            PhysicalLinesRead: physicalLinesRead,
            HeaderLinesDiscarded: headerLinesDiscarded,
            BlankLinesSkipped: blankLinesSkipped,
            BooksExported: verses.Select(verse => verse.Book).Distinct(StringComparer.Ordinal).Count(),
            VersesExported: verses.Count,
            OutputPath: outputPath);
    }

    private static int CompareVerses(VerseRow left, VerseRow right)
    {
        var byBook = BookOrderByName[left.Book].CompareTo(BookOrderByName[right.Book]);
        if (byBook != 0)
        {
            return byBook;
        }

        var byChapter = left.Chapter.CompareTo(right.Chapter);
        return byChapter != 0 ? byChapter : left.Verse.CompareTo(right.Verse);
    }

    private static void Validate(
        List<VerseRow> verses,
        Dictionary<string, HashSet<int>> chaptersByBook,
        int chapterFilesProcessed)
    {
        if (chapterFilesProcessed != ExpectedChapterFileCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedChapterFileCount} LSG chapter files, but processed {chapterFilesProcessed}.");
        }

        var exportedBooks = verses
            .Select(verse => verse.Book)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var missingBooks = Books
            .Where(book => !exportedBooks.Contains(book.Name))
            .Select(book => book.Name)
            .ToArray();

        if (missingBooks.Length > 0)
        {
            throw new InvalidDataException($"Missing canonical books: {string.Join(", ", missingBooks)}");
        }

        var extraBooks = exportedBooks
            .Where(book => !BooksByName.ContainsKey(book))
            .ToArray();

        if (extraBooks.Length > 0)
        {
            throw new InvalidDataException($"Unexpected books exported: {string.Join(", ", extraBooks)}");
        }

        if (exportedBooks.Count != ExpectedBookCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedBookCount} books, but exported {exportedBooks.Count}.");
        }

        foreach (var book in Books)
        {
            if (!chaptersByBook.TryGetValue(book.Name, out var chapters))
            {
                throw new InvalidDataException($"No chapters were read for {book.Name}.");
            }

            var missingChapters = Enumerable.Range(1, book.ChapterCount)
                .Where(chapter => !chapters.Contains(chapter))
                .ToArray();

            if (missingChapters.Length > 0)
            {
                throw new InvalidDataException(
                    $"{book.Name} is missing chapters: {string.Join(", ", missingChapters)}");
            }

            if (chapters.Count != book.ChapterCount)
            {
                throw new InvalidDataException(
                    $"{book.Name} should have {book.ChapterCount} chapters, but {chapters.Count} were read.");
            }
        }

        foreach (var verse in verses)
        {
            if (string.IsNullOrWhiteSpace(verse.Text))
            {
                throw new InvalidDataException(
                    $"Empty verse text at {verse.Book} {verse.Chapter}:{verse.Verse}.");
            }
        }

        foreach (var group in verses.GroupBy(verse => verse.Book, StringComparer.Ordinal))
        {
            var expected = BooksByName[group.Key].VerseCount;
            var actual = group.Count();
            if (actual != expected)
            {
                throw new InvalidDataException(
                    $"{group.Key} should contain {expected} LSG verses, but {actual} were produced.");
            }
        }

        foreach (var group in verses.GroupBy(verse => (verse.Book, verse.Chapter)))
        {
            var numbers = group.Select(verse => verse.Verse).OrderBy(number => number).ToArray();
            for (var index = 0; index < numbers.Length; index++)
            {
                if (numbers[index] != index + 1)
                {
                    throw new InvalidDataException(
                        $"{group.Key.Book} {group.Key.Chapter} has non contiguous verse numbering " +
                        $"starting at {numbers[index]}.");
                }
            }
        }

        if (verses.Count != ExpectedVerseCount)
        {
            var countsByBook = verses
                .GroupBy(verse => verse.Book, StringComparer.Ordinal)
                .Select(group => $"{group.Key}={group.Count()}")
                .ToArray();

            throw new InvalidDataException(
                $"Expected {ExpectedVerseCount} LSG verses, but produced {verses.Count}. " +
                $"Counts by book: {string.Join("; ", countsByBook)}");
        }

        var verseKeys = verses
            .Select(verse => $"{verse.Book}|{verse.Chapter}|{verse.Verse}")
            .ToHashSet(StringComparer.Ordinal);

        foreach (var landmark in LandmarkVerses())
        {
            var key = $"{landmark.Book}|{landmark.Chapter}|{landmark.Verse}";
            if (!verseKeys.Contains(key))
            {
                throw new InvalidDataException(
                    $"Landmark verse missing: {landmark.Book} {landmark.Chapter}:{landmark.Verse}");
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

    private static IEnumerable<VerseReference> LandmarkVerses()
    {
        yield return new VerseReference("Genesis", 1, 1);
        yield return new VerseReference("Psalms", 23, 1);
        yield return new VerseReference("Psalms", 119, 105);
        yield return new VerseReference("John", 3, 16);
        yield return new VerseReference("John", 11, 35);
        yield return new VerseReference("Romans", 8, 28);
        yield return new VerseReference("3 John", 1, 15);
        yield return new VerseReference("Revelation", 22, 21);
    }
}

internal sealed record BookDefinition(string Code, string Name, int ChapterCount, int VerseCount);

internal sealed record VerseRow(string Book, int Chapter, int Verse, string Text);

internal sealed record VerseReference(string Book, int Chapter, int Verse);

internal sealed record ConversionSummary(
    int ChapterFilesProcessed,
    int SkippedFiles,
    int PhysicalLinesRead,
    int HeaderLinesDiscarded,
    int BlankLinesSkipped,
    int BooksExported,
    int VersesExported,
    string OutputPath);
