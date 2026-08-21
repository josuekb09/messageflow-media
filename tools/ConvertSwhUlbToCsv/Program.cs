using System.Text;
using System.Text.RegularExpressions;

const string SourceDirectory = @"D:\MessageFlow-Bible-Sources\SWA";
const string OutputPath = @"D:\Bible\SWHULB\swhulb.csv";

var converter = new SwhUlbUsfmConverter(SourceDirectory, OutputPath);
var summary = converter.Convert();

Console.WriteLine("Swahili Unlocked Literal Bible conversion complete.");
Console.WriteLine($"usfm files processed: {summary.FilesProcessed}");
Console.WriteLine($"books exported: {summary.BooksExported}");
Console.WriteLine($"verses exported: {summary.VersesExported}");
Console.WriteLine($"output path: {summary.OutputPath}");

if (summary.VersesExported == SwhUlbUsfmConverter.ExpectedVerseCount)
{
    Console.WriteLine($"expected verse count: {SwhUlbUsfmConverter.ExpectedVerseCount} (OK)");
}
else
{
    Console.WriteLine($"expected verse count: {SwhUlbUsfmConverter.ExpectedVerseCount} (got {summary.VersesExported})");
}

internal sealed class SwhUlbUsfmConverter
{
    public const int ExpectedVerseCount = 31_101;
    public const int ExpectedBookCount = 66;

    private static readonly Dictionary<string, string> CanonicalBookNames = new(StringComparer.Ordinal)
    {
        ["GEN"] = "Genesis",
        ["EXO"] = "Exodus",
        ["LEV"] = "Leviticus",
        ["NUM"] = "Numbers",
        ["DEU"] = "Deuteronomy",
        ["JOS"] = "Joshua",
        ["JDG"] = "Judges",
        ["RUT"] = "Ruth",
        ["1SA"] = "1 Samuel",
        ["2SA"] = "2 Samuel",
        ["1KI"] = "1 Kings",
        ["2KI"] = "2 Kings",
        ["1CH"] = "1 Chronicles",
        ["2CH"] = "2 Chronicles",
        ["EZR"] = "Ezra",
        ["NEH"] = "Nehemiah",
        ["EST"] = "Esther",
        ["JOB"] = "Job",
        ["PSA"] = "Psalms",
        ["PRO"] = "Proverbs",
        ["ECC"] = "Ecclesiastes",
        ["SNG"] = "Song of Solomon",
        ["ISA"] = "Isaiah",
        ["JER"] = "Jeremiah",
        ["LAM"] = "Lamentations",
        ["EZK"] = "Ezekiel",
        ["DAN"] = "Daniel",
        ["HOS"] = "Hosea",
        ["JOL"] = "Joel",
        ["AMO"] = "Amos",
        ["OBA"] = "Obadiah",
        ["JON"] = "Jonah",
        ["MIC"] = "Micah",
        ["NAM"] = "Nahum",
        ["HAB"] = "Habakkuk",
        ["ZEP"] = "Zephaniah",
        ["HAG"] = "Haggai",
        ["ZEC"] = "Zechariah",
        ["MAL"] = "Malachi",
        ["MAT"] = "Matthew",
        ["MRK"] = "Mark",
        ["LUK"] = "Luke",
        ["JHN"] = "John",
        ["ACT"] = "Acts",
        ["ROM"] = "Romans",
        ["1CO"] = "1 Corinthians",
        ["2CO"] = "2 Corinthians",
        ["GAL"] = "Galatians",
        ["EPH"] = "Ephesians",
        ["PHP"] = "Philippians",
        ["COL"] = "Colossians",
        ["1TH"] = "1 Thessalonians",
        ["2TH"] = "2 Thessalonians",
        ["1TI"] = "1 Timothy",
        ["2TI"] = "2 Timothy",
        ["TIT"] = "Titus",
        ["PHM"] = "Philemon",
        ["HEB"] = "Hebrews",
        ["JAS"] = "James",
        ["1PE"] = "1 Peter",
        ["2PE"] = "2 Peter",
        ["1JN"] = "1 John",
        ["2JN"] = "2 John",
        ["3JN"] = "3 John",
        ["JUD"] = "Jude",
        ["REV"] = "Revelation"
    };

    private static readonly Regex BookIdRegex = new(@"^\\id\s+(\S+)", RegexOptions.Compiled);
    private static readonly Regex ChapterRegex = new(@"^\\c\s+(\d+)\s*$", RegexOptions.Compiled);
    private static readonly Regex VerseRegex = new(@"^\\v\s+(\d+)\s*(.*)$", RegexOptions.Compiled);

    private readonly string sourceDirectory;
    private readonly string outputPath;

    public SwhUlbUsfmConverter(string sourceDirectory, string outputPath)
    {
        this.sourceDirectory = sourceDirectory;
        this.outputPath = outputPath;
    }

    public ConversionSummary Convert()
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"SWHULB source directory was not found: {sourceDirectory}");
        }

        var files = Directory.GetFiles(sourceDirectory, "*.usfm")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();

        if (files.Length != ExpectedBookCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedBookCount} USFM files, but found {files.Length}.");
        }

        var verses = new List<VerseRow>(ExpectedVerseCount);
        var seenBooks = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            Console.WriteLine($"Reading {fileName}...");
            ParseUsfmFile(path, verses, seenBooks);
        }

        if (verses.Count != ExpectedVerseCount)
        {
            throw new InvalidDataException(
                $"Expected {ExpectedVerseCount} SWHULB verses, but produced {verses.Count}.");
        }

        WriteCsv(verses);
        PrintLandmarks(verses);

        return new ConversionSummary(files.Length, seenBooks.Count, verses.Count, outputPath);
    }

    private static void ParseUsfmFile(string path, List<VerseRow> verses, HashSet<string> seenBooks)
    {
        string? bookName = null;
        var chapter = 0;
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        foreach (var line in lines)
        {
            var bookMatch = BookIdRegex.Match(line);
            if (bookMatch.Success)
            {
                var code = bookMatch.Groups[1].Value;
                if (!CanonicalBookNames.TryGetValue(code, out bookName))
                {
                    throw new InvalidDataException($"Unknown USFM book code '{code}' in {Path.GetFileName(path)}.");
                }

                if (!seenBooks.Add(bookName))
                {
                    throw new InvalidDataException($"Book '{bookName}' appeared more than once.");
                }

                chapter = 0;
                continue;
            }

            var chapterMatch = ChapterRegex.Match(line);
            if (chapterMatch.Success)
            {
                chapter = int.Parse(chapterMatch.Groups[1].Value);
                continue;
            }

            var verseMatch = VerseRegex.Match(line);
            if (!verseMatch.Success)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(bookName) || chapter <= 0)
            {
                throw new InvalidDataException(
                    $"Verse found before book and chapter were established in {Path.GetFileName(path)}.");
            }

            var verse = int.Parse(verseMatch.Groups[1].Value);
            var text = verseMatch.Groups[2].Value.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidDataException($"Empty verse text at {bookName} {chapter}:{verse}.");
            }

            verses.Add(new VerseRow(bookName, chapter, verse, text));
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

    private static void PrintLandmarks(IReadOnlyList<VerseRow> verses)
    {
        Console.WriteLine("Landmark verses:");
        PrintLandmark(verses, "Genesis", 1, 1);
        PrintLandmark(verses, "John", 3, 16);
        PrintLandmark(verses, "3 John", 1, 15);
        PrintLandmark(verses, "Revelation", 22, 21);
    }

    private static void PrintLandmark(IReadOnlyList<VerseRow> verses, string book, int chapter, int verse)
    {
        var match = verses.FirstOrDefault(row =>
            row.Book == book && row.Chapter == chapter && row.Verse == verse);
        if (match is null)
        {
            throw new InvalidDataException($"Landmark verse missing: {book} {chapter}:{verse}");
        }

        Console.WriteLine($"  {book} {chapter}:{verse} = {match.Text}");
    }

    private static string CsvEscape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n', ';', '\'']) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

internal sealed record VerseRow(string Book, int Chapter, int Verse, string Text);

internal sealed record ConversionSummary(
    int FilesProcessed,
    int BooksExported,
    int VersesExported,
    string OutputPath);
