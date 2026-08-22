using System.Security.Cryptography;
using System.Text;
using ImportSwahiliPptxSongs;
using MessageFlow.Core.Songs;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

const string DefaultZipPath = @"D:\drive-download-20260822T182431Z-1-001.zip";
const string ExtractDirectory = @"D:\Temp\swahili-pptx-hymns";
const string PreviewPath = @"D:\Temp\swahili-pptx-preview.txt";
const string SourceFolder = SwahiliPptxParser.SourceFolder;

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));
var zipPath = GetOption(args, "--zip") ?? DefaultZipPath;
var extractDir = GetOption(args, "--extract") ?? ExtractDirectory;
var databasePath = GetOption(args, "--database") ?? MessageFlowDatabase.DefaultDatabasePath;

if (!File.Exists(zipPath) && !Directory.Exists(extractDir))
{
    Console.WriteLine($"ZIP not found: {zipPath}");
    return 1;
}

Directory.CreateDirectory(@"D:\Temp");
if (File.Exists(zipPath))
{
    Directory.CreateDirectory(extractDir);
    if (!Directory.EnumerateFiles(extractDir, "*.pptx", SearchOption.AllDirectories).Any())
    {
        Console.WriteLine($"Extracting {zipPath} -> {extractDir}");
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
    }
}

var archiveCopy = Path.Combine(
    @"D:\My Projects\MessageFlow",
    "database",
    "custom-library",
    "songs",
    "Nyimbo-za-Kiswahili.zip");
Directory.CreateDirectory(Path.GetDirectoryName(archiveCopy)!);
if (File.Exists(zipPath) && !File.Exists(archiveCopy))
{
    File.Copy(zipPath, archiveCopy, overwrite: false);
}

var songs = SwahiliPptxParser.ParseDirectory(extractDir);
WritePreview(PreviewPath, songs);
var successful = songs.Where(song => song.Success).ToList();
var failed = songs.Where(song => !song.Success).ToList();
var withChorus = successful.Count(song => song.Sections.Any(section => section.Type == "Chorus"));
var interleaved = successful.Count(song => SwahiliPptxParser.IsAlreadyInterleaved(song.Sections));

Console.WriteLine($"Parsed {successful.Count} Swahili songs. Failed: {failed.Count}. Preview: {PreviewPath}");
Console.WriteLine($"With labeled chorus: {withChorus}. Interleaved verse-chorus: {interleaved}.");
foreach (var song in failed)
{
    Console.WriteLine($"  SKIP {song.FileName}: {song.Error}");
}

if (!apply)
{
    Console.WriteLine("Preview only. Pass --apply to write songs into the database.");
    return successful.Count >= 200 ? 0 : 2;
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
    await ImportSongsAsync(target, successful);
}

return 0;

static string? GetOption(string[] args, string name)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

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

static async Task ImportSongsAsync(string databasePath, IReadOnlyList<ParsedSwahiliSong> songs)
{
    await MessageFlowDatabaseRepair.RepairAsync(databasePath, Console.WriteLine);

    var options = new DbContextOptionsBuilder<MessageFlowDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString())
        .Options;

    await using var dbContext = new MessageFlowDbContext(options);
    await using var transaction = await dbContext.Database.BeginTransactionAsync();

    var existing = await dbContext.Songs
        .Where(song => song.SourceFilePath.StartsWith(SwahiliPptxParser.SourceKeyPrefix))
        .ToListAsync();
    if (existing.Count > 0)
    {
        dbContext.Songs.RemoveRange(existing);
        await dbContext.SaveChangesAsync();
    }

    var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var song in songs)
    {
        var sourceKey = UniqueKey(SwahiliPptxParser.SourceKey(song.FileName), usedKeys);
        dbContext.Songs.Add(new Song
        {
            Title = song.Title,
            NormalizedTitle = SongTextNormalizer.Normalize(song.Title),
            SourceFilePath = sourceKey,
            SourceFolder = SourceFolder,
            FileName = song.FileName,
            ImportedAtUtc = DateTime.UtcNow,
            ContentHash = ComputeHash(song),
            WarningSummary = string.Empty,
            Language = SwahiliPptxParser.LanguageCode,
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

    var swCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "sw");
    var enCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "en");
    var frCount = await dbContext.Songs.CountAsync(song => song.IsActive && song.Language == "fr");
    Console.WriteLine(
        $"Imported {songs.Count} Swahili songs into {databasePath}. Active songs en={enCount}, fr={frCount}, sw={swCount}.");
}

static string UniqueKey(string sourceKey, HashSet<string> used)
{
    var candidate = sourceKey;
    var suffix = 2;
    while (!used.Add(candidate))
    {
        candidate = sourceKey + "/" + suffix;
        suffix++;
    }

    return candidate;
}

static string ComputeHash(ParsedSwahiliSong song)
{
    var payload = song.FileName + "\n" + song.Title + "\n" +
                  string.Join("\n---\n", song.Sections.Select(section => section.Type + "\n" + section.Text));
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}

static void WritePreview(string path, IReadOnlyList<ParsedSwahiliSong> songs)
{
    var builder = new StringBuilder();
    builder.AppendLine($"Swahili PPTX songs parsed: {songs.Count(song => song.Success)}");
    builder.AppendLine($"Failed: {songs.Count(song => !song.Success)}");
    foreach (var song in songs)
    {
        builder.AppendLine();
        if (!song.Success)
        {
            builder.AppendLine($"===== FAIL {song.FileName}: {song.Error} =====");
            continue;
        }

        builder.AppendLine($"===== {song.Title} ({song.FileName}, {song.Sections.Count} sections) =====");
        foreach (var section in song.Sections)
        {
            builder.AppendLine($"[{section.Type}] {section.Label}");
            builder.AppendLine(section.Text);
            builder.AppendLine();
        }
    }

    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}
