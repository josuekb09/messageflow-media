using System.Text;
using System.Text.Json;
using MessageFlow.Data;
using MessageFlow.Importer;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;

// Applies a reviewed list of text corrections that were derived from the original source files
// (sermon PDFs, Bible source texts, song PowerPoints, the French songbook PDF). It never creates text:
// every correction carries the exact text currently stored, and the whole run is rolled back if any
// stored text differs from it. Search columns are recomputed with the same normalizers as the importers.
//
// Usage:
//   ApplySourceCorrections --database <db> --corrections <file.jsonl> [--apply]
//
// One JSON object per line:
//   {"op":"paragraph","id":1,"old":"...","new":"..."}
//   {"op":"sermon","sermonId":1,"expectedIds":[...],"paragraphs":[{"number":1,"text":"...","page":3}]}
//   {"op":"verse","id":1,"old":"...","new":"..."}
//   {"op":"section","id":1,"old":"...","new":"..."}
//   {"op":"song","songId":1,"expectedIds":[...],"sections":[{"type":"Verse","label":"...","text":"..."}]}
// Without --apply the corrections are checked and rolled back.

Console.OutputEncoding = Encoding.UTF8;

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));
var databasePath = GetOption(args, "--database") ?? MessageFlowDatabase.DefaultDatabasePath;
var correctionsPath = GetOption(args, "--corrections");

if (!File.Exists(databasePath) || correctionsPath is null || !File.Exists(correctionsPath))
{
    Console.WriteLine("Usage: ApplySourceCorrections --database <db> --corrections <file.jsonl> [--apply]");
    return 1;
}

await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWrite
}.ToString());
await connection.OpenAsync();
await using (var pragma = connection.CreateCommand())
{
    pragma.CommandText = "PRAGMA foreign_keys = ON;";
    await pragma.ExecuteNonQueryAsync();
}

await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
var counts = new Dictionary<string, int>(StringComparer.Ordinal);
var failures = new List<string>();
var lineNumber = 0;

foreach (var line in File.ReadLines(correctionsPath, Encoding.UTF8))
{
    lineNumber++;
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    using var document = JsonDocument.Parse(line);
    var root = document.RootElement;
    var op = root.GetProperty("op").GetString() ?? string.Empty;
    var error = op switch
    {
        "paragraph" => await EditParagraphAsync(root),
        "sermon" => await ReplaceSermonParagraphsAsync(root),
        "verse" => await EditVerseAsync(root),
        "section" => await EditSongSectionAsync(root),
        "song" => await ReplaceSongSectionsAsync(root),
        _ => $"unknown op '{op}'"
    };

    if (error is null)
    {
        counts[op] = counts.GetValueOrDefault(op) + 1;
    }
    else
    {
        failures.Add($"line {lineNumber} ({op}): {error}");
    }
}

foreach (var (op, count) in counts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"{op}: {count:N0}");
}

if (failures.Count > 0)
{
    await transaction.RollbackAsync();
    Console.WriteLine($"FAIL: {failures.Count:N0} corrections do not match the stored text. Nothing was changed.");
    foreach (var failure in failures.Take(40))
    {
        Console.WriteLine("  " + failure);
    }

    return 1;
}

if (!apply)
{
    await transaction.RollbackAsync();
    Console.WriteLine("PASS: every correction matches the stored text. Preview only; pass --apply to write.");
    return 0;
}

await transaction.CommitAsync();
Console.WriteLine("Applied.");
return 0;

async Task<string?> EditParagraphAsync(JsonElement root)
{
    var id = root.GetProperty("id").GetInt32();
    var stored = await ScalarStringAsync("SELECT Text FROM SermonParagraphs WHERE Id = $id", ("$id", id));
    if (!string.Equals(stored, root.GetProperty("old").GetString(), StringComparison.Ordinal))
    {
        return $"paragraph {id} text differs";
    }

    var text = root.GetProperty("new").GetString() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(text))
    {
        return $"paragraph {id} would become empty";
    }

    await ExecuteAsync(
        "UPDATE SermonParagraphs SET Text = $text, SearchText = $search WHERE Id = $id",
        ("$text", text), ("$search", TextCleaner.NormalizeSearchText(text)), ("$id", id));
    return null;
}

async Task<string?> ReplaceSermonParagraphsAsync(JsonElement root)
{
    var sermonId = root.GetProperty("sermonId").GetInt32();
    var expected = root.GetProperty("expectedIds").EnumerateArray().Select(item => item.GetInt32()).ToList();
    var current = await ParagraphIdsAsync(sermonId);
    if (!current.SequenceEqual(expected))
    {
        return $"sermon {sermonId} paragraphs differ from the analysed ones";
    }

    var paragraphs = root.GetProperty("paragraphs").EnumerateArray().ToList();
    if (paragraphs.Count == 0 || paragraphs.Any(item => string.IsNullOrWhiteSpace(item.GetProperty("text").GetString())))
    {
        return $"sermon {sermonId} would get an empty paragraph";
    }

    // Free the (SermonId, ParagraphNumber) unique index before renumbering.
    await ExecuteAsync(
        "UPDATE SermonParagraphs SET ParagraphNumber = -Id WHERE SermonId = $sermon",
        ("$sermon", sermonId));

    for (var index = 0; index < paragraphs.Count; index++)
    {
        var item = paragraphs[index];
        var number = item.GetProperty("number").GetInt32();
        var text = item.GetProperty("text").GetString()!;
        object page = item.TryGetProperty("page", out var pageElement) && pageElement.ValueKind == JsonValueKind.Number
            ? pageElement.GetInt32()
            : DBNull.Value;
        var search = TextCleaner.NormalizeSearchText(text);

        if (index < current.Count)
        {
            // Keeping the row ids keeps favorites and history pointing at the same place in the sermon.
            await ExecuteAsync(
                "UPDATE SermonParagraphs SET ParagraphNumber = $number, Text = $text, SearchText = $search, PageNumber = $page WHERE Id = $id",
                ("$number", number), ("$text", text), ("$search", search), ("$page", page), ("$id", current[index]));
        }
        else
        {
            await ExecuteAsync(
                "INSERT INTO SermonParagraphs (SermonId, ParagraphNumber, Text, SearchText, PageNumber, CreatedAt) " +
                "VALUES ($sermon, $number, $text, $search, $page, CURRENT_TIMESTAMP)",
                ("$sermon", sermonId), ("$number", number), ("$text", text), ("$search", search), ("$page", page));
        }
    }

    foreach (var leftover in current.Skip(paragraphs.Count))
    {
        await ExecuteAsync("DELETE FROM SermonParagraphs WHERE Id = $id", ("$id", leftover));
    }

    return null;
}

async Task<string?> EditVerseAsync(JsonElement root)
{
    var id = root.GetProperty("id").GetInt32();
    var stored = await ScalarStringAsync("SELECT Text FROM BibleVerses WHERE Id = $id", ("$id", id));
    if (!string.Equals(stored, root.GetProperty("old").GetString(), StringComparison.Ordinal))
    {
        return $"verse {id} text differs";
    }

    var text = root.GetProperty("new").GetString() ?? string.Empty;
    await ExecuteAsync(
        "UPDATE BibleVerses SET Text = $text, SearchText = $search WHERE Id = $id",
        ("$text", text), ("$search", text.ToUpperInvariant()), ("$id", id));
    return null;
}

async Task<string?> EditSongSectionAsync(JsonElement root)
{
    var id = root.GetProperty("id").GetInt32();
    var stored = await ScalarStringAsync("SELECT Text FROM SongSections WHERE Id = $id", ("$id", id));
    if (!string.Equals(stored, root.GetProperty("old").GetString(), StringComparison.Ordinal))
    {
        return $"song section {id} text differs";
    }

    var text = root.GetProperty("new").GetString() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(text))
    {
        return $"song section {id} would become empty";
    }

    await ExecuteAsync(
        "UPDATE SongSections SET Text = $text, NormalizedText = $normalized WHERE Id = $id",
        ("$text", text), ("$normalized", SongTextNormalizer.Normalize(text)), ("$id", id));
    return null;
}

async Task<string?> ReplaceSongSectionsAsync(JsonElement root)
{
    var songId = root.GetProperty("songId").GetInt32();
    var expected = root.GetProperty("expectedIds").EnumerateArray().Select(item => item.GetInt32()).ToList();
    var current = new List<int>();
    await using (var command = connection.CreateCommand())
    {
        command.Transaction = transaction;
        command.CommandText = "SELECT Id FROM SongSections WHERE SongId = $song ORDER BY SectionOrder, Id";
        command.Parameters.AddWithValue("$song", songId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            current.Add(reader.GetInt32(0));
        }
    }

    if (!current.SequenceEqual(expected))
    {
        return $"song {songId} sections differ from the analysed ones";
    }

    var sections = root.GetProperty("sections").EnumerateArray().ToList();
    if (sections.Count == 0 || sections.Any(item => string.IsNullOrWhiteSpace(item.GetProperty("text").GetString())))
    {
        return $"song {songId} would get an empty section";
    }

    await ExecuteAsync("DELETE FROM SongSections WHERE SongId = $song", ("$song", songId));
    for (var index = 0; index < sections.Count; index++)
    {
        var item = sections[index];
        var text = item.GetProperty("text").GetString()!;
        await ExecuteAsync(
            "INSERT INTO SongSections (SongId, SectionOrder, SectionType, SectionLabel, Text, NormalizedText) " +
            "VALUES ($song, $order, $type, $label, $text, $normalized)",
            ("$song", songId), ("$order", index + 1), ("$type", item.GetProperty("type").GetString()!),
            ("$label", item.GetProperty("label").GetString()!), ("$text", text),
            ("$normalized", SongTextNormalizer.Normalize(text)));
    }

    return null;
}

async Task<List<int>> ParagraphIdsAsync(int sermonId)
{
    var ids = new List<int>();
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = "SELECT Id FROM SermonParagraphs WHERE SermonId = $sermon ORDER BY ParagraphNumber, Id";
    command.Parameters.AddWithValue("$sermon", sermonId);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        ids.Add(reader.GetInt32(0));
    }

    return ids;
}

async Task<string?> ScalarStringAsync(string sql, params (string Name, object Value)[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    foreach (var (name, value) in parameters)
    {
        command.Parameters.AddWithValue(name, value);
    }

    return await command.ExecuteScalarAsync() as string;
}

async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    foreach (var (name, value) in parameters)
    {
        command.Parameters.AddWithValue(name, value);
    }

    await command.ExecuteNonQueryAsync();
}

static string? GetOption(string[] args, string name)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
