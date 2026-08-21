using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MessageFlow.Data;
using MessageFlow.Importer;
using Microsoft.Data.Sqlite;

var dryRun = args.Any(argument => string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase));
var databasePath = args
    .SkipWhile(argument => !string.Equals(argument, "--database", StringComparison.OrdinalIgnoreCase))
    .Skip(1)
    .FirstOrDefault() ?? MessageFlowDatabase.DefaultDatabasePath;

if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return 1;
}

Console.WriteLine(dryRun ? "Mode: dry-run (no database writes)" : "Mode: update Swahili titles only");
Console.WriteLine($"Database: {databasePath}");

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = dryRun ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();
await using (var busy = connection.CreateCommand())
{
    busy.CommandText = "PRAGMA busy_timeout = 30000;";
    await busy.ExecuteNonQueryAsync();
}

var before = await CaptureIntegritySnapshotAsync(connection);
PrintSnapshot("BEFORE", before);

var sermons = await LoadSwahiliSermonsAsync(connection);
Console.WriteLine($"Swahili sermons loaded: {sermons.Count}");

var translated = 0;
var unchanged = 0;
var skippedMissingPdf = 0;
var skippedLowConfidence = 0;
var pendingUpdates = new List<(int Id, string OldTitle, string NewTitle, string SermonCode)>();
var reportPath = Path.Combine("D:", "Temp", "sw-title-update-report.tsv");
Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await using var report = new StreamWriter(reportPath, false, Encoding.UTF8);
await report.WriteLineAsync("Id\tSermonCode\tStatus\tOldTitle\tNewTitle");

foreach (var sermon in sermons)
{
    if (string.IsNullOrWhiteSpace(sermon.SourceFilePath) || !File.Exists(sermon.SourceFilePath))
    {
        skippedMissingPdf++;
        await report.WriteLineAsync($"{sermon.Id}\t{sermon.SermonCode}\tmissing-pdf\t{sermon.Title}\t");
        continue;
    }

    if (!SwahiliPdfTitleExtractor.TryExtractFromPdf(sermon.SourceFilePath, out var extractedTitle))
    {
        skippedLowConfidence++;
        await report.WriteLineAsync($"{sermon.Id}\t{sermon.SermonCode}\tlow-confidence\t{sermon.Title}\t");
        continue;
    }

    if (string.Equals(sermon.Title.Trim(), extractedTitle, StringComparison.Ordinal))
    {
        unchanged++;
        await report.WriteLineAsync($"{sermon.Id}\t{sermon.SermonCode}\tunchanged\t{sermon.Title}\t{extractedTitle}");
        continue;
    }

    translated++;
    pendingUpdates.Add((sermon.Id, sermon.Title, extractedTitle, sermon.SermonCode));
    await report.WriteLineAsync($"{sermon.Id}\t{sermon.SermonCode}\ttranslate\t{sermon.Title}\t{extractedTitle}");
}

Console.WriteLine($"Pending title translations: {translated}");
Console.WriteLine($"Already Kiswahili / unchanged: {unchanged}");
Console.WriteLine($"Left unchanged (PDF missing): {skippedMissingPdf}");
Console.WriteLine($"Left unchanged (low confidence): {skippedLowConfidence}");
Console.WriteLine($"Report: {reportPath}");

if (dryRun || pendingUpdates.Count == 0)
{
    if (dryRun)
    {
        Console.WriteLine("Dry-run complete. No titles were written.");
    }
    else
    {
        Console.WriteLine("No titles needed updating.");
    }

    return 0;
}

await using var transaction = await connection.BeginTransactionAsync();
await using (var dropTrigger = connection.CreateCommand())
{
    dropTrigger.Transaction = (SqliteTransaction)transaction;
    dropTrigger.CommandText = """DROP TRIGGER IF EXISTS "SermonParagraphsFts_sermon_au";""";
    await dropTrigger.ExecuteNonQueryAsync();
}

foreach (var update in pendingUpdates)
{
    await using var command = connection.CreateCommand();
    command.Transaction = (SqliteTransaction)transaction;
    command.CommandText = """
        UPDATE "Sermons"
        SET "Title" = $title
        WHERE "Id" = $id
          AND "Language" = 'sw';
        """;
    command.Parameters.AddWithValue("$title", update.NewTitle);
    command.Parameters.AddWithValue("$id", update.Id);
    var rows = await command.ExecuteNonQueryAsync();
    if (rows != 1)
    {
        throw new InvalidOperationException($"Expected 1 row updated for sermon {update.Id}, got {rows}.");
    }
}

await using (var refreshFts = connection.CreateCommand())
{
    refreshFts.Transaction = (SqliteTransaction)transaction;
    refreshFts.CommandText = """
        DELETE FROM "SermonParagraphsFts"
        WHERE "SermonId" IN (
            SELECT "Id" FROM "Sermons" WHERE "Language" = 'sw'
        );

        INSERT INTO "SermonParagraphsFts" (
            rowid,
            ParagraphId,
            SermonId,
            Title,
            SermonCode,
            Year,
            AuthorName,
            AuthorDisplayName,
            SourceName,
            SourceDisplayName,
            SourceType,
            ParagraphNumber,
            SearchText
        )
        SELECT
            p."Id",
            p."Id",
            p."SermonId",
            s."Title",
            s."SermonCode",
            s."Year",
            COALESCE(a."FullName", ''),
            COALESCE(a."DisplayName", ''),
            COALESCE(cs."Name", ''),
            COALESCE(cs."DisplayName", ''),
            COALESCE(cs."SourceType", ''),
            p."ParagraphNumber",
            p."SearchText"
        FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        LEFT JOIN "Authors" a ON a."Id" = s."AuthorId"
        LEFT JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE s."Language" = 'sw';

        CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_sermon_au"
        AFTER UPDATE OF "Title", "SermonCode", "Year", "AuthorId", "ContentSourceId" ON "Sermons"
        BEGIN
            DELETE FROM "SermonParagraphsFts"
            WHERE "SermonId" = new."Id";

            INSERT INTO "SermonParagraphsFts" (
                rowid,
                ParagraphId,
                SermonId,
                Title,
                SermonCode,
                Year,
                AuthorName,
                AuthorDisplayName,
                SourceName,
                SourceDisplayName,
                SourceType,
                ParagraphNumber,
                SearchText
            )
            SELECT
                p."Id",
                p."Id",
                p."SermonId",
                new."Title",
                new."SermonCode",
                new."Year",
                COALESCE(a."FullName", ''),
                COALESCE(a."DisplayName", ''),
                COALESCE(cs."Name", ''),
                COALESCE(cs."DisplayName", ''),
                COALESCE(cs."SourceType", ''),
                p."ParagraphNumber",
                p."SearchText"
            FROM "SermonParagraphs" p
            LEFT JOIN "Authors" a ON a."Id" = new."AuthorId"
            LEFT JOIN "ContentSources" cs ON cs."Id" = new."ContentSourceId"
            WHERE p."SermonId" = new."Id";
        END;
        """;
    await refreshFts.ExecuteNonQueryAsync();
}

await transaction.CommitAsync();

var after = await CaptureIntegritySnapshotAsync(connection);
PrintSnapshot("AFTER", after);

if (!before.MatchesParagraphsAndNonSwahili(after))
{
    Console.WriteLine("INTEGRITY CHECK FAILED: paragraph bodies or non-Swahili metadata changed.");
    return 2;
}

Console.WriteLine("Integrity check passed: paragraph bodies and non-Swahili sermons are unchanged.");
Console.WriteLine($"Translated titles: {translated}");
return 0;

static async Task<List<SwahiliSermonRow>> LoadSwahiliSermonsAsync(SqliteConnection connection)
{
    var sermons = new List<SwahiliSermonRow>();
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT "Id", "SermonCode", "Title", "SourceFilePath"
        FROM "Sermons"
        WHERE "Language" = 'sw'
        ORDER BY "Id";
        """;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        sermons.Add(new SwahiliSermonRow(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
    }

    return sermons;
}

static async Task<IntegritySnapshot> CaptureIntegritySnapshotAsync(SqliteConnection connection)
{
    return new IntegritySnapshot(
        await ScalarLongAsync(connection, """SELECT COUNT(1) FROM "Sermons" WHERE "Language" = 'sw';"""),
        await ScalarLongAsync(connection, """SELECT COUNT(1) FROM "Sermons" WHERE "Language" = 'en';"""),
        await ScalarLongAsync(connection, """SELECT COUNT(1) FROM "Sermons" WHERE "Language" = 'fr';"""),
        await ScalarLongAsync(connection, """SELECT COUNT(1) FROM "SermonParagraphs";"""),
        await ScalarLongAsync(
            connection,
            """
            SELECT COUNT(1)
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."Language" = 'sw';
            """),
        await ScalarLongAsync(
            connection,
            """
            SELECT COUNT(1)
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."Language" = 'en';
            """),
        await ScalarDoubleAsync(
            connection,
            """
            SELECT COALESCE(TOTAL(LENGTH(p."Text")), 0)
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."Language" = 'sw';
            """),
        await ScalarDoubleAsync(
            connection,
            """
            SELECT COALESCE(TOTAL(LENGTH(p."Text")), 0)
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."Language" = 'en';
            """),
        await HashAsync(
            connection,
            """
            SELECT p."Id", p."SermonId", p."ParagraphNumber", p."Text"
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."Language" = 'sw'
            ORDER BY p."Id";
            """),
        await HashAsync(
            connection,
            """
            SELECT "Id", "Title", "SermonCode", "Language"
            FROM "Sermons"
            WHERE "Language" <> 'sw'
            ORDER BY "Id";
            """));
}

static void PrintSnapshot(string label, IntegritySnapshot snapshot)
{
    Console.WriteLine($"{label} sw sermons={snapshot.SwahiliSermons} en sermons={snapshot.EnglishSermons} fr sermons={snapshot.FrenchSermons}");
    Console.WriteLine($"{label} paragraphs total={snapshot.TotalParagraphs} sw={snapshot.SwahiliParagraphs} en={snapshot.EnglishParagraphs}");
    Console.WriteLine($"{label} sw text chars={snapshot.SwahiliTextChars:N0} en text chars={snapshot.EnglishTextChars:N0}");
    Console.WriteLine($"{label} sw paragraph hash={snapshot.SwahiliParagraphHash}");
    Console.WriteLine($"{label} non-sw sermon hash={snapshot.NonSwahiliSermonHash}");
}

static async Task<long> ScalarLongAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt64(result, CultureInfo.InvariantCulture);
}

static async Task<double> ScalarDoubleAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    var result = await command.ExecuteScalarAsync();
    return Convert.ToDouble(result, CultureInfo.InvariantCulture);
}

static async Task<string> HashAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await using var reader = await command.ExecuteReaderAsync();
    using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    while (await reader.ReadAsync())
    {
        for (var index = 0; index < reader.FieldCount; index++)
        {
            var value = reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(value);
            sha.AppendData(bytes);
            sha.AppendData("|"u8.ToArray());
        }

        sha.AppendData("\n"u8.ToArray());
    }

    return Convert.ToHexString(sha.GetHashAndReset());
}

internal sealed record SwahiliSermonRow(int Id, string SermonCode, string Title, string SourceFilePath);

internal sealed record IntegritySnapshot(
    long SwahiliSermons,
    long EnglishSermons,
    long FrenchSermons,
    long TotalParagraphs,
    long SwahiliParagraphs,
    long EnglishParagraphs,
    double SwahiliTextChars,
    double EnglishTextChars,
    string SwahiliParagraphHash,
    string NonSwahiliSermonHash)
{
    public bool MatchesParagraphsAndNonSwahili(IntegritySnapshot other)
    {
        return SwahiliSermons == other.SwahiliSermons &&
               EnglishSermons == other.EnglishSermons &&
               FrenchSermons == other.FrenchSermons &&
               TotalParagraphs == other.TotalParagraphs &&
               SwahiliParagraphs == other.SwahiliParagraphs &&
               EnglishParagraphs == other.EnglishParagraphs &&
               SwahiliTextChars.Equals(other.SwahiliTextChars) &&
               EnglishTextChars.Equals(other.EnglishTextChars) &&
               SwahiliParagraphHash == other.SwahiliParagraphHash &&
               NonSwahiliSermonHash == other.NonSwahiliSermonHash;
    }
}
