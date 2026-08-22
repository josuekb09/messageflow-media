using System.Security.Cryptography;
using System.Text;
using ImportFrenchSongbook;
using MessageFlow.Core.Songs;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

const string DefaultPdfPath =
    @"D:\My Projects\MessageFlow\database\custom-library\songs\Receuil-de-cantiques-francais-TABERNACLE-DINANGA-CORRIGE.pdf";

var apply = HasFlag(args, "--apply");
var dumpText = HasFlag(args, "--dump-text");
var pairColumns = HasFlag(args, "--pair-columns");
var noAppendix = HasFlag(args, "--no-appendix");
var pdfPath = GetOption(args, "--pdf") ?? DefaultPdfPath;
var sourceKeyPrefix = NormalizeSourcePrefix(
    GetOption(args, "--source-prefix") ?? FrenchSongbookParser.SourceKeyPrefix);
var sourceFolder = GetOption(args, "--source-folder") ?? FrenchSongbookParser.SourceFolder;
var maxNumber = ParsePositiveInt(GetOption(args, "--max-number"), defaultValue: 396);
var databasePath = GetOption(args, "--database") ?? MessageFlowDatabase.DefaultDatabasePath;
var parseOptions = new FrenchSongbookParseOptions
{
    PairColumns = pairColumns,
    StopOnIndex = !pairColumns,
    EnableAppendix = !noAppendix,
    RequireDottedNumbers = pairColumns,
    OneSongPerPage = pairColumns || HasFlag(args, "--one-song-per-page"),
    JoinWrappedTitles = pairColumns,
    MaxNumber = maxNumber
};

if (!File.Exists(pdfPath))
{
    Console.WriteLine($"PDF not found: {pdfPath}");
    return 1;
}

Directory.CreateDirectory(@"D:\Temp");
if (dumpText)
{
    var dumpPath = pairColumns
        ? @"D:\Temp\aigles-songbook-dump.txt"
        : @"D:\Temp\french-songbook-dump.txt";
    File.WriteAllText(dumpPath, FrenchSongbookParser.DumpRawText(pdfPath, pairColumns), Encoding.UTF8);
    Console.WriteLine($"Wrote raw text dump: {dumpPath}");
    return 0;
}

var songs = FrenchSongbookParser.Parse(pdfPath, parseOptions);
var previewPath = pairColumns
    ? @"D:\Temp\aigles-songbook-preview.txt"
    : @"D:\Temp\french-songbook-preview.txt";
WritePreview(previewPath, songs);
Console.WriteLine($"Parsed {songs.Count} French songs. Preview: {previewPath}");
Console.WriteLine($"Numbers: {songs.FirstOrDefault()?.Number} .. {songs.LastOrDefault()?.Number}");
Console.WriteLine($"Missing numbers: {FormatMissingNumbers(songs, maxNumber)}");
Console.WriteLine($"With refrain: {songs.Count(song => song.Sections.Any(section => section.Type == "Chorus"))}");
Console.WriteLine($"Interleaved verse-chorus: {songs.Count(IsInterleaved)}");
Console.WriteLine($"Source: {sourceKeyPrefix} / {sourceFolder}");
Console.WriteLine($"Database: {databasePath}");

if (!apply)
{
    Console.WriteLine("Preview only. Pass --apply to write songs into the database.");
    return songs.Count >= Math.Min(300, maxNumber) ? 0 : 2;
}

var targets = DiscoverWritableDatabases(databasePath);
if (targets.Count == 0)
{
    Console.WriteLine("No writable D: database was found.");
    return 1;
}

foreach (var target in targets)
{
    Console.WriteLine($"Importing into {target}");
    await ImportSongsAsync(target, pdfPath, songs, sourceKeyPrefix, sourceFolder);
}

return 0;

static string? GetOption(string[] args, string name)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static bool HasFlag(string[] args, string name)
    => args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

static int ParsePositiveInt(string? value, int defaultValue)
    => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;

static string NormalizeSourcePrefix(string prefix)
    => prefix.EndsWith('/') ? prefix : prefix + "/";

static List<string> DiscoverWritableDatabases(string preferred)
{
    var paths = new[]
    {
        preferred,
        Path.Combine(@"D:\My Projects\MessageFlow", "database", "messageflow.db"),
        Path.Combine(@"D:\My Projects\MessageFlow", "dist", "publish", "database", "messageflow.db"),
        Path.Combine(@"D:\MessageFlowMedia", "database", "messageflow.db")
    };

    return paths
        .Where(File.Exists)
        .Where(MessageFlowDatabase.IsAllowedDataPath)
        .Select(path => Path.GetFullPath(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static async Task ImportSongsAsync(
    string databasePath,
    string pdfPath,
    IReadOnlyList<ParsedFrenchSong> songs,
    string sourceKeyPrefix,
    string sourceFolder)
{
    await MessageFlowDatabaseRepair.RepairAsync(databasePath, Console.WriteLine);

    var options = new DbContextOptionsBuilder<MessageFlowDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString())
        .Options;

    await using var dbContext = new MessageFlowDbContext(options);
    await using var transaction = await dbContext.Database.BeginTransactionAsync();

    var existing = await dbContext.Songs
        .Where(song => song.SourceFilePath.StartsWith(sourceKeyPrefix))
        .ToListAsync();
    if (existing.Count > 0)
    {
        dbContext.Songs.RemoveRange(existing);
        await dbContext.SaveChangesAsync();
    }

    foreach (var song in songs)
    {
        dbContext.Songs.Add(new Song
        {
            Title = song.Title,
            NormalizedTitle = SongTextNormalizer.Normalize(song.Title),
            SourceFilePath = sourceKeyPrefix + song.Number,
            SourceFolder = sourceFolder,
            FileName = Path.GetFileName(pdfPath),
            ImportedAtUtc = DateTime.UtcNow,
            ContentHash = ComputeHash(song),
            WarningSummary = string.Empty,
            Language = FrenchSongbookParser.LanguageCode,
            IsActive = true,
            Sections = song.Sections
                .Select((section, index) => new SongSection
                {
                    SectionOrder = index + 1,
                    SectionType = section.Type,
                    SectionLabel = section.Label,
                    Text = section.Text,
                    NormalizedText = SongTextNormalizer.Normalize(section.Text)
                })
                .ToList()
        });
    }

    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();

    var importedCount = await dbContext.Songs.CountAsync(song =>
        song.IsActive && song.SourceFilePath.StartsWith(sourceKeyPrefix));
    var frenchCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "fr");
    var dinangaCount = await dbContext.Songs.CountAsync(song =>
        song.IsActive && song.SourceFilePath.StartsWith(FrenchSongbookParser.SourceKeyPrefix));
    var englishCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "en");
    var swahiliCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "sw");
    Console.WriteLine(
        $"Imported {importedCount} songs ({sourceKeyPrefix}) into {databasePath}. " +
        $"Active songs en={englishCount}, fr={frenchCount} (dinanga={dinangaCount}), sw={swahiliCount}.");
}

static string ComputeHash(ParsedFrenchSong song)
{
    var payload = song.Number + "\n" + song.Title + "\n" +
                  string.Join("\n---\n", song.Sections.Select(section => section.Type + "\n" + section.Text));
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}

static bool IsInterleaved(ParsedFrenchSong song)
{
    var types = song.Sections.Select(section => section.Type).ToList();
    if (types.Count < 2 || !types.Contains("Chorus"))
    {
        return false;
    }

    for (var i = 0; i < types.Count - 1; i++)
    {
        if (types[i] == "Verse" && types[i + 1] != "Chorus")
        {
            return false;
        }
    }

    return true;
}

static string FormatMissingNumbers(IReadOnlyList<ParsedFrenchSong> songs, int maxNumber)
{
    var present = songs
        .Select(song => int.TryParse(song.Number, out var value) ? value : 0)
        .Where(value => value > 0)
        .ToHashSet();
    var missing = Enumerable.Range(1, maxNumber).Where(value => !present.Contains(value)).ToList();
    return missing.Count == 0 ? "(none)" : string.Join(", ", missing);
}

static void WritePreview(string path, IReadOnlyList<ParsedFrenchSong> songs)
{
    var builder = new StringBuilder();
    builder.AppendLine($"French songs parsed: {songs.Count}");
    foreach (var song in songs)
    {
        builder.AppendLine();
        builder.AppendLine($"===== {song.Title} ({song.Sections.Count} sections) =====");
        foreach (var section in song.Sections)
        {
            builder.AppendLine($"[{section.Type}] {section.Label}");
            builder.AppendLine(section.Text);
            builder.AppendLine();
        }
    }

    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}
