// Imports the Brother Frank broadcast transcriptions (Krefeld/Zurich retransmissions and
// meetings, English and French) into the MessageFlow database.
//
// Text is stored exactly as extracted from the source PDFs: no word or character is changed
// and the document's own printed paragraph numbers stay inside each paragraph. Only empty
// lines are not stored. ParagraphNumber is the position of the paragraph in the document,
// like the existing Brother Frank documents. SearchText uses the app's normal search form.
//
// Dry run by default; pass --apply to write. Items already in the database (same date and
// title, or the same text) are skipped and listed.
//
//   dotnet run --project tools/ImportFrankRetransmissions -- --db <messageflow.db> [--apply]

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Importer;
using Microsoft.Data.Sqlite;

const string SourceRoot = @"E:\bro frank transcripts\Retransmissions";
const string TextCache = @"E:\Cache\retr\text";

var apply = args.Contains("--apply");
var dbPath = ArgValue("--db") ?? @"E:\My Projects\MessageFlow\database\messageflow.db";
var reportPath = ArgValue("--report") ?? Path.Combine(AppContext.BaseDirectory, "import-report.txt");
Console.WriteLine($"Database : {dbPath}");
Console.WriteLine($"Mode     : {(apply ? "APPLY (writes)" : "DRY RUN (no writes)")}");

string? ArgValue(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

var items = new List<Item>();
foreach (var lang in new[] { "EN", "FR" })
{
    foreach (var row in ReadCsv(Path.Combine(SourceRoot, lang, "index.csv")))
    {
        var collection = row["collection"];
        var number = int.Parse(row["number"], CultureInfo.InvariantCulture);
        var textFile = Path.Combine(TextCache, $"{lang}_{collection}_{number:000}.txt");
        // The source files end their lines with CR LF: only the trailing whitespace (the CR) is
        // taken off each paragraph; every other character is stored as it is.
        var lines = File.ReadAllText(textFile, new UTF8Encoding(false)).Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.TrimEnd())
            .ToList();
        var date = Regex.Match(row["original date"], @"(\d{4})\D(\d{2})\D(\d{2})");
        // A few items have no original date in the app ("…"): the date is left empty and the
        // year comes from the Krefeld broadcast date, so nothing is invented.
        var iso = date.Success ? $"{date.Groups[1].Value}-{date.Groups[2].Value}-{date.Groups[3].Value}" : string.Empty;
        var krefeld = Regex.Match(row["Krefeld date"], @"(\d{2})\.(\d{2})\.(\d{4})");
        var year = date.Success ? int.Parse(date.Groups[1].Value, CultureInfo.InvariantCulture) : int.Parse(krefeld.Groups[3].Value, CultureInfo.InvariantCulture);
        var branham = row["Branham"] is "sermon" or "sermon (date <= 1965)";
        items.Add(new Item(
            lang.ToLowerInvariant(), lang, collection, number, iso, year, row["Krefeld date"], row["place"], row["title"],
            row["Branham"], branham,
            Path.Combine(SourceRoot, lang, row["subfolder"], row["file name"]), lines));
    }
}

Console.WriteLine($"Source items: {items.Count} (EN {items.Count(i => i.Language == "en")}, FR {items.Count(i => i.Language == "fr")})");

using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
}.ToString());
connection.Open();

int franks = Scalar("SELECT Id FROM Authors WHERE DisplayName = 'Brother Frank'");
int branhams = Scalar("SELECT Id FROM Authors WHERE DisplayName = 'Brother Branham'");
int frankSrc = Scalar("SELECT Id FROM ContentSources WHERE Name = 'brother_frank'");
int branhamSrc = Scalar("SELECT Id FROM ContentSources WHERE Name = 'brother_branham'");
Console.WriteLine($"Authors Frank={franks} Branham={branhams}; sources Frank={frankSrc} Branham={branhamSrc}");

// Existing documents: titles by date and text hashes, for the duplicate check.
var existingKeys = new HashSet<string>();
var existingHashes = new Dictionary<string, string>();
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT Id, Title, substr(Date,1,10), Language FROM Sermons WHERE Language IN ('en','fr') AND Date IS NOT NULL";
    using var r = cmd.ExecuteReader();
    while (r.Read())
    {
        existingKeys.Add($"{r.GetString(3)}|{r.GetString(2)}|{r.GetString(1).Trim().ToLowerInvariant()}");
    }
}

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT p.SermonId, p.Text FROM SermonParagraphs p JOIN Sermons s ON s.Id = p.SermonId
        WHERE s.Language IN ('en','fr') ORDER BY p.SermonId, p.ParagraphNumber
        """;
    using var r = cmd.ExecuteReader();
    long current = -1;
    using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    while (r.Read())
    {
        var id = r.GetInt64(0);
        if (id != current)
        {
            if (current >= 0) existingHashes[Convert.ToHexString(sha.GetHashAndReset())] = current.ToString(CultureInfo.InvariantCulture);
            current = id;
        }

        sha.AppendData(Encoding.UTF8.GetBytes(r.GetString(1) + "\n"));
    }

    if (current >= 0) existingHashes[Convert.ToHexString(sha.GetHashAndReset())] = current.ToString(CultureInfo.InvariantCulture);
}

var skipped = new List<string>();
var toInsert = new List<(Item item, string title, string code)>();
var usedCodes = new HashSet<string>();
// Two list entries can carry the same date and title (the same meeting broadcast again on
// another day). They are different documents, so the Krefeld broadcast date tells them apart.
var titleCounts = items.GroupBy(i => $"{i.Language}|{i.Date}|{BuildTitle(i).ToLowerInvariant()}").ToDictionary(g => g.Key, g => g.Count());
foreach (var item in items)
{
    var title = BuildTitle(item);
    if (titleCounts[$"{item.Language}|{item.Date}|{title.ToLowerInvariant()}"] > 1)
    {
        title += $" [{item.KrefeldDate}]";
    }

    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(item.Lines.Select(l => l + "\n")))));
    if (existingKeys.Contains($"{item.Language}|{item.Date}|{title.Trim().ToLowerInvariant()}"))
    {
        skipped.Add($"{item.Lang} {item.Collection} #{item.Number}: same date and title already in the database");
        continue;
    }

    if (existingHashes.TryGetValue(hash, out var same))
    {
        skipped.Add($"{item.Lang} {item.Collection} #{item.Number}: identical text to existing sermon id {same}");
        continue;
    }

    toInsert.Add((item, title, BuildCode(item, usedCodes)));
}

Console.WriteLine($"To import: {toInsert.Count}; skipped as duplicates: {skipped.Count}");
foreach (var group in toInsert.GroupBy(t => (t.item.Language, Branham: t.item.IsBranham ? "Branham" : "Frank")).OrderBy(g => g.Key))
{
    Console.WriteLine($"  {group.Key.Language} {group.Key.Branham}: {group.Count()} documents, {group.Sum(t => t.item.Lines.Count):N0} paragraphs");
}

foreach (var s in skipped) Console.WriteLine("  SKIP " + s);
File.WriteAllLines(reportPath, skipped);

if (!apply)
{
    return 0;
}

using var tx = connection.BeginTransaction();
var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
long paragraphTotal = 0;
foreach (var (item, title, code) in toInsert)
{
    long sermonId;
    using (var cmd = connection.CreateCommand())
    {
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO Sermons (AuthorId, Title, SermonCode, Year, Date, Location, Language, SourceFilePath, CreatedAt, ContentSourceId, ContentType)
            VALUES ($a, $t, $c, $y, $d, $l, $lang, $p, $now, $s, $ct); SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$a", item.IsBranham ? branhams : franks);
        cmd.Parameters.AddWithValue("$t", title);
        cmd.Parameters.AddWithValue("$c", code);
        cmd.Parameters.AddWithValue("$y", item.Year);
        cmd.Parameters.AddWithValue("$d", item.Date.Length == 0 ? DBNull.Value : item.Date + " 00:00:00");
        cmd.Parameters.AddWithValue("$l", item.Place);
        cmd.Parameters.AddWithValue("$lang", item.Language);
        cmd.Parameters.AddWithValue("$p", item.PdfPath);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.Parameters.AddWithValue("$s", item.IsBranham ? branhamSrc : frankSrc);
        cmd.Parameters.AddWithValue("$ct", item.IsBranham ? "Sermon" : "Meeting");
        sermonId = (long)cmd.ExecuteScalar()!;
    }

    using var insert = connection.CreateCommand();
    insert.Transaction = tx;
    insert.CommandText = "INSERT INTO SermonParagraphs (SermonId, ParagraphNumber, Text, SearchText, PageNumber, CreatedAt) VALUES ($s, $n, $t, $st, 1, $now)";
    var ps = insert.Parameters.Add("$s", SqliteType.Integer);
    var pn = insert.Parameters.Add("$n", SqliteType.Integer);
    var pt = insert.Parameters.Add("$t", SqliteType.Text);
    var pst = insert.Parameters.Add("$st", SqliteType.Text);
    insert.Parameters.AddWithValue("$now", now);
    for (var i = 0; i < item.Lines.Count; i++)
    {
        ps.Value = sermonId;
        pn.Value = i + 1;
        pt.Value = item.Lines[i];
        pst.Value = TextCleaner.NormalizeSearchText(item.Lines[i]);
        insert.ExecuteNonQuery();
        paragraphTotal++;
    }

    using var log = connection.CreateCommand();
    log.Transaction = tx;
    log.CommandText = "INSERT INTO ImportLogs (FilePath, Status, Message, ImportedAt) VALUES ($f, 'Imported', $m, $now)";
    log.Parameters.AddWithValue("$f", item.PdfPath);
    log.Parameters.AddWithValue("$m", $"Imported {item.Lines.Count} paragraphs (Brother Frank broadcast transcription).");
    log.Parameters.AddWithValue("$now", now);
    log.ExecuteNonQuery();
}

tx.Commit();
Console.WriteLine($"APPLIED: {toInsert.Count} documents, {paragraphTotal:N0} paragraphs.");
return 0;

int Scalar(string sql)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
}

static string BuildTitle(Item item)
{
    var title = item.Title.Trim();
    var when = item.Date.Length == 0 ? item.KrefeldDate : item.Date;
    if (string.IsNullOrEmpty(title) || title.Equals("Sans titre", StringComparison.OrdinalIgnoreCase))
    {
        return item.Language == "fr" ? $"Réunion de {item.Place} - {when}" : $"Meeting in {item.Place} - {when}";
    }

    return $"{title} ({when}, {item.Place})";
}

static string BuildCode(Item item, HashSet<string> used)
{
    var place = new string(item.Place.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
        .ToArray()).ToUpperInvariant();
    var kind = item.IsBranham ? "RETR-BR" : "RETR";
    var code = $"EF-{(item.Date.Length == 0 ? "UNDATED" : item.Date)}-{place}-{kind}-{item.Lang}{item.Number:000}";
    if (item.Collection.Contains("union", StringComparison.Ordinal))
    {
        code = code.Replace("-RETR-", "-REUN-", StringComparison.Ordinal).Replace("-RETR", "-REUN", StringComparison.Ordinal);
    }

    if (!used.Add(code)) throw new InvalidOperationException("Duplicate code " + code);
    return code;
}

static List<Dictionary<string, string>> ReadCsv(string path)
{
    var text = File.ReadAllText(path, new UTF8Encoding(true));
    var rows = new List<List<string>>();
    var row = new List<string>();
    var field = new StringBuilder();
    var quoted = false;
    for (var i = 0; i < text.Length; i++)
    {
        var c = text[i];
        if (quoted)
        {
            if (c == '"')
            {
                if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = false;
            }
            else field.Append(c);
        }
        else if (c == '"') quoted = true;
        else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
        else if (c == '\n' || c == '\r')
        {
            if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            row.Add(field.ToString());
            field.Clear();
            if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
            row = new List<string>();
        }
        else field.Append(c);
    }

    if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
    var header = rows[0];
    return rows.Skip(1)
        .Select(r => header.Select((h, i) => (h, v: i < r.Count ? r[i] : "")).ToDictionary(x => x.h, x => x.v))
        .ToList();
}

internal sealed record Item(string Language, string Lang, string Collection, int Number, string Date, int Year, string KrefeldDate, string Place,
    string Title, string BranhamKind, bool IsBranham, string PdfPath, List<string> Lines);
