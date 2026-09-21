// Consolidates the Brother Frank literature into a single selectable library.
//
// The library grew across four content sources: the real import, an earlier custom-library
// import holding one book, and two abandoned test sources. Two of them carried the identical
// display name, so the operator's Sources dropdown showed two indistinguishable entries and
// neither covered the whole library.
//
// This tool is deliberately a one-off run rather than startup work: it rewrites production
// rows, and that should happen when someone asks for it, not on every launch. Dry run by
// default; pass --apply to write. Every step is idempotent, so a second run is a no-op.
//
//   dotnet run --project tools/RepairBrotherFrankLibrary
//   dotnet run --project tools/RepairBrotherFrankLibrary -- --apply

using MessageFlow.Data;
using Microsoft.Data.Sqlite;

const string CanonicalFrankSourceName = "brother_frank";
const string FrankLibraryFolder = @"E:\Litreture (Bro Frank)";

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));

// Brings the schema up to date without touching any library data, so the consolidation plan
// can be reviewed in a dry run before anything is rewritten.
var schemaOnly = args.Any(arg => string.Equals(arg, "--schema-only", StringComparison.OrdinalIgnoreCase));

// Clears existing content types so they are derived again. Needed after a correction to the
// classification rules, because the normal backfill only fills rows that have no type yet.
var reclassify = args.Any(arg => string.Equals(arg, "--reclassify", StringComparison.OrdinalIgnoreCase));
var databasePath = MessageFlowDatabase.DefaultDatabasePath;

if (!File.Exists(databasePath))
{
    Console.WriteLine($"FAIL Database not found: {databasePath}");
    return 1;
}

Console.WriteLine($"Database : {databasePath}");
Console.WriteLine($"Mode     : {(apply ? "APPLY (writes)" : "DRY RUN (no writes)")}");
Console.WriteLine();

// The ContentType column arrives with the standard startup repair. Running it here lets this
// tool work on a database that has not been opened by the app since the column was introduced.
if (apply || schemaOnly)
{
    Console.WriteLine("Running standard database repair to ensure the schema is current...");
    await MessageFlowDatabaseRepair.RepairAsync(databasePath, message => Console.WriteLine($"  {message}"));
    Console.WriteLine();

    if (schemaOnly)
    {
        Console.WriteLine("Schema prepared. Re-run without --schema-only to review the repair plan.");
        return 0;
    }
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

// Foreign keys are off by default in SQLite. Paragraph cascades depend on them.
await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;");

if (!await ColumnExistsAsync(connection, "Sermons", "ContentType"))
{
    Console.WriteLine("FAIL Sermons.ContentType is missing.");
    Console.WriteLine("     Start MessageFlow once so the database repair adds it, then re-run.");
    return 1;
}

await ReportInventoryAsync(connection, "BEFORE");

var canonicalId = await ExecuteScalarIntAsync(
    connection,
    """SELECT "Id" FROM "ContentSources" WHERE "Name" = $name LIMIT 1;""",
    new SqliteParameter("$name", CanonicalFrankSourceName));

if (canonicalId is null)
{
    Console.WriteLine($"FAIL Canonical source '{CanonicalFrankSourceName}' not found.");
    return 1;
}

Console.WriteLine($"Canonical Brother Frank source: id {canonicalId}");
Console.WriteLine();

var steps = 0;

// ---------------------------------------------------------------------------
// 1. Fold every other Brother Frank source into the canonical one.
//    The FTS trigger on Sermons.ContentSourceId re-indexes the moved paragraphs.
// ---------------------------------------------------------------------------
var strandedSources = await QueryRowsAsync(
    connection,
    """
    SELECT cs."Id", cs."Name", cs."DisplayName",
           (SELECT COUNT(1) FROM "Sermons" s WHERE s."ContentSourceId" = cs."Id")
    FROM "ContentSources" cs
    WHERE cs."Id" <> $canonicalId
      AND (cs."Name" LIKE '%frank%' OR cs."DisplayName" LIKE '%Frank%');
    """,
    new SqliteParameter("$canonicalId", canonicalId.Value));

foreach (var source in strandedSources)
{
    var sourceId = Convert.ToInt32(source[0]);
    var name = Convert.ToString(source[1]) ?? string.Empty;
    var documents = Convert.ToInt64(source[3]);
    var isTestSource = name.Contains("test", StringComparison.OrdinalIgnoreCase);

    if (isTestSource)
    {
        // Abandoned test imports pointing at a drive that no longer exists. They are hidden
        // from the church UI but still occupy the search index, and their publication codes
        // blocked the real April and December 2020 circular letters from importing.
        var paragraphs = await ExecuteScalarLongAsync(
            connection,
            """
            SELECT COUNT(1) FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId"
            WHERE s."ContentSourceId" = $sourceId;
            """,
            new SqliteParameter("$sourceId", sourceId));

        Console.WriteLine(
            $"[{++steps}] Delete test source '{name}' (id {sourceId}): " +
            $"{documents:N0} document(s), {paragraphs:N0} paragraph(s).");

        if (apply)
        {
            await ExecuteAsync(
                connection,
                """DELETE FROM "Sermons" WHERE "ContentSourceId" = $sourceId;""",
                new SqliteParameter("$sourceId", sourceId));
            await ExecuteAsync(
                connection,
                """DELETE FROM "ContentSources" WHERE "Id" = $sourceId;""",
                new SqliteParameter("$sourceId", sourceId));
        }

        continue;
    }

    Console.WriteLine(
        $"[{++steps}] Move {documents:N0} document(s) from '{name}' (id {sourceId}) " +
        $"into the canonical library, then retire the empty source.");

    if (apply)
    {
        await ExecuteAsync(
            connection,
            """
            UPDATE "Sermons" SET "ContentSourceId" = $canonicalId
            WHERE "ContentSourceId" = $sourceId;
            """,
            new SqliteParameter("$canonicalId", canonicalId.Value),
            new SqliteParameter("$sourceId", sourceId));
        await ExecuteAsync(
            connection,
            """DELETE FROM "ContentSources" WHERE "Id" = $sourceId;""",
            new SqliteParameter("$sourceId", sourceId));
    }
}

// ---------------------------------------------------------------------------
// 2. Remove documents imported twice from byte-identical source files.
//    Two filename styles for the same circular letter produced two publication
//    codes, so the importer's code check never saw them as the same document.
// ---------------------------------------------------------------------------
var duplicates = await FindDuplicateDocumentsAsync(connection, canonicalId.Value);
foreach (var duplicate in duplicates)
{
    Console.WriteLine(
        $"[{++steps}] Delete duplicate document {duplicate.DuplicateId} " +
        $"'{duplicate.DuplicateTitle}' ({duplicate.DuplicateCode}, {duplicate.ParagraphCount:N0} paragraphs) " +
        $"- identical content to document {duplicate.KeepId} '{duplicate.KeepCode}'.");

    if (apply)
    {
        await ExecuteAsync(
            connection,
            """DELETE FROM "Sermons" WHERE "Id" = $id;""",
            new SqliteParameter("$id", duplicate.DuplicateId));
    }
}

// ---------------------------------------------------------------------------
// 2b. Retire test authors left without any documents. Deleting the test imports
//     above empties them, and an author row with no content only clutters the
//     author list and the Admin diagnostics.
// ---------------------------------------------------------------------------
var orphanedTestAuthors = await QueryRowsAsync(
    connection,
    """
    SELECT a."Id", a."FullName"
    FROM "Authors" a
    WHERE a."FullName" LIKE '%test%'
      AND NOT EXISTS (SELECT 1 FROM "Sermons" s WHERE s."AuthorId" = a."Id");
    """);

foreach (var author in orphanedTestAuthors)
{
    var authorId = Convert.ToInt32(author[0]);
    Console.WriteLine($"[{++steps}] Delete orphaned test author '{Convert.ToString(author[1])}' (id {authorId}).");

    if (apply)
    {
        await ExecuteAsync(
            connection,
            """DELETE FROM "Authors" WHERE "Id" = $id;""",
            new SqliteParameter("$id", authorId));
    }
}

// ---------------------------------------------------------------------------
// 2c. Repoint records whose source file has moved. The managed library travelled
//     between drives, so some rows still name a path that no longer exists,
//     which stops the importer from recognising the file as already imported.
// ---------------------------------------------------------------------------
var managedRoot = Path.Combine(Path.GetDirectoryName(databasePath)!, "custom-library");
var movedFiles = await QueryRowsAsync(
    connection,
    """
    SELECT "Id", "SourceFilePath" FROM "Sermons"
    WHERE "ContentSourceId" = $canonicalId;
    """,
    new SqliteParameter("$canonicalId", canonicalId.Value));

foreach (var row in movedFiles)
{
    var sermonId = Convert.ToInt32(row[0]);
    var recordedPath = Convert.ToString(row[1]) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(recordedPath) || File.Exists(recordedPath))
    {
        continue;
    }

    var fileName = Path.GetFileName(recordedPath);
    var relocated = Directory.Exists(managedRoot)
        ? Directory.EnumerateFiles(managedRoot, fileName, SearchOption.AllDirectories).FirstOrDefault()
        : null;

    if (relocated is null)
    {
        Console.WriteLine($"WARN Document {sermonId} names a missing file and no replacement was found: {recordedPath}");
        continue;
    }

    Console.WriteLine($"[{++steps}] Repoint document {sermonId} to its moved file: '{recordedPath}' -> '{relocated}'.");

    if (apply)
    {
        await ExecuteAsync(
            connection,
            """UPDATE "Sermons" SET "SourceFilePath" = $path WHERE "Id" = $id;""",
            new SqliteParameter("$path", relocated),
            new SqliteParameter("$id", sermonId));
    }
}

// ---------------------------------------------------------------------------
// 3. Point the library at the folder the PDFs actually live in, so
//    Admin -> Import Selected Source can reach it.
// ---------------------------------------------------------------------------
var currentFolder = await ExecuteScalarStringAsync(
    connection,
    """SELECT "LocalFolderPath" FROM "ContentSources" WHERE "Id" = $id;""",
    new SqliteParameter("$id", canonicalId.Value));

if (!string.Equals(currentFolder, FrankLibraryFolder, StringComparison.OrdinalIgnoreCase))
{
    if (Directory.Exists(FrankLibraryFolder))
    {
        Console.WriteLine(
            $"[{++steps}] Set library folder: '{currentFolder ?? "(none)"}' -> '{FrankLibraryFolder}'.");

        if (apply)
        {
            await ExecuteAsync(
                connection,
                """UPDATE "ContentSources" SET "LocalFolderPath" = $path WHERE "Id" = $id;""",
                new SqliteParameter("$path", FrankLibraryFolder),
                new SqliteParameter("$id", canonicalId.Value));
        }
    }
    else
    {
        Console.WriteLine(
            $"SKIP Library folder not present on this machine: {FrankLibraryFolder}. " +
            $"Leaving LocalFolderPath as '{currentFolder ?? "(none)"}'.");
    }
}

// ---------------------------------------------------------------------------
// 4. Classify anything still missing a content type. The startup repair does
//    this too; running it here keeps a single-run repair self-contained.
// ---------------------------------------------------------------------------
if (reclassify)
{
    var affected = await ExecuteScalarLongAsync(
        connection,
        """SELECT COUNT(1) FROM "Sermons" WHERE "ContentType" IS NOT NULL;""");
    Console.WriteLine($"[{++steps}] Re-derive the content type of {affected:N0} document(s).");

    if (apply)
    {
        await ExecuteAsync(connection, """UPDATE "Sermons" SET "ContentType" = NULL;""");
    }
}

var unclassified = await ExecuteScalarLongAsync(
    connection,
    """SELECT COUNT(1) FROM "Sermons" WHERE "ContentType" IS NULL;""");

if (unclassified > 0 || (reclassify && apply))
{
    if (unclassified > 0)
    {
        Console.WriteLine($"[{++steps}] Classify {unclassified:N0} document(s) with a ContentType.");
    }

    if (apply)
    {
        await ExecuteAsync(
            connection,
            """
            UPDATE "Sermons"
            SET "ContentType" = CASE
                WHEN (SELECT "SourceType" FROM "ContentSources"
                      WHERE "ContentSources"."Id" = "Sermons"."ContentSourceId") = 'SermonPdfCollection'
                    THEN 'Sermon'
                WHEN "SermonCode" LIKE 'CL-%' THEN 'CircularLetter'
                WHEN "SermonCode" GLOB 'EF-[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-*' THEN 'Meeting'
                WHEN "SermonCode" LIKE 'EF-%' THEN 'Book'
                ELSE (SELECT "SourceType" FROM "ContentSources"
                      WHERE "ContentSources"."Id" = "Sermons"."ContentSourceId")
            END
            WHERE "ContentType" IS NULL;
            """);
    }
}

Console.WriteLine();
if (steps == 0)
{
    Console.WriteLine("Nothing to repair. The library is already consolidated.");
}
else if (!apply)
{
    Console.WriteLine($"{steps} step(s) pending. Re-run with --apply to write them.");
}
else
{
    Console.WriteLine($"{steps} step(s) applied.");
    await ReportInventoryAsync(connection, "AFTER");
    await VerifyProtectedBaselinesAsync(connection);
}

return 0;

// ---------------------------------------------------------------------------

static async Task ReportInventoryAsync(SqliteConnection connection, string label)
{
    Console.WriteLine($"--- Content sources {label} ---");
    var rows = await QueryRowsAsync(
        connection,
        """
        SELECT cs."Id", cs."Name", cs."DisplayName", cs."SourceType",
               (SELECT COUNT(1) FROM "Sermons" s WHERE s."ContentSourceId" = cs."Id"),
               (SELECT COUNT(1) FROM "SermonParagraphs" p
                JOIN "Sermons" s2 ON s2."Id" = p."SermonId"
                WHERE s2."ContentSourceId" = cs."Id")
        FROM "ContentSources" cs
        ORDER BY cs."Id";
        """);

    foreach (var row in rows)
    {
        Console.WriteLine(
            $"  id {Convert.ToInt32(row[0]),-3} {Convert.ToString(row[1]),-24} " +
            $"{Convert.ToString(row[2]),-28} {Convert.ToString(row[3]),-20} " +
            $"{Convert.ToInt64(row[4]),6:N0} docs {Convert.ToInt64(row[5]),9:N0} paragraphs");
    }

    Console.WriteLine();
}

// Branham, Bible and Songs are production content this repair must never touch.
static async Task VerifyProtectedBaselinesAsync(SqliteConnection connection)
{
    Console.WriteLine("--- Protected baselines ---");

    var branhamDocuments = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1) FROM "Sermons" s
        JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham' AND s."Language" = 'en';
        """);
    var branhamParagraphs = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1) FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham' AND s."Language" = 'en';
        """);
    var bibleVerses = await ExecuteScalarLongAsync(connection, """SELECT COUNT(1) FROM "BibleVerses";""");
    var bibleBooks = await ExecuteScalarLongAsync(connection, """SELECT COUNT(1) FROM "BibleBooks";""");
    var englishSongs = await ExecuteScalarLongAsync(
        connection,
        """SELECT COUNT(1) FROM "Songs" WHERE "IsActive" = 1 AND "Language" = 'en';""");

    Report("Branham English documents", branhamDocuments, 1_203);
    Report("Branham English paragraphs", branhamParagraphs, 210_061);
    Report("Bible verses", bibleVerses, 93_373);
    Report("Bible books", bibleBooks, 66);
    Report("English songs", englishSongs, 357);

    static void Report(string label, long actual, long expected)
    {
        var status = actual == expected ? "OK  " : "FAIL";
        var detail = actual == expected ? string.Empty : $" (expected {expected:N0})";
        Console.WriteLine($"  {status} {label}: {actual:N0}{detail}");
    }
}

// Two documents are the same publication when their paragraph text matches exactly.
// Comparing content rather than filenames or codes is what catches the case where one
// circular letter was published under two different filename styles.
static async Task<List<DuplicateDocument>> FindDuplicateDocumentsAsync(
    SqliteConnection connection,
    int canonicalSourceId)
{
    var rows = await QueryRowsAsync(
        connection,
        """
        SELECT s."Id", s."Title", s."SermonCode",
               (SELECT COUNT(1) FROM "SermonParagraphs" p WHERE p."SermonId" = s."Id"),
               (SELECT group_concat(p2."Text", char(30))
                FROM (SELECT "Text" FROM "SermonParagraphs"
                      WHERE "SermonId" = s."Id"
                      ORDER BY "ParagraphNumber", "Id") p2)
        FROM "Sermons" s
        WHERE s."ContentSourceId" = $sourceId
        ORDER BY s."Id";
        """,
        new SqliteParameter("$sourceId", canonicalSourceId));

    var duplicates = new List<DuplicateDocument>();
    var seen = new Dictionary<string, (int Id, string Code)>(StringComparer.Ordinal);

    foreach (var row in rows)
    {
        var content = Convert.ToString(row[4]);
        if (string.IsNullOrEmpty(content))
        {
            continue;
        }

        var id = Convert.ToInt32(row[0]);
        var title = Convert.ToString(row[1]) ?? string.Empty;
        var code = Convert.ToString(row[2]) ?? string.Empty;
        var paragraphs = Convert.ToInt64(row[3]);

        if (seen.TryGetValue(content, out var kept))
        {
            duplicates.Add(new DuplicateDocument(kept.Id, kept.Code, id, title, code, paragraphs));
            continue;
        }

        seen[content] = (id, code);
    }

    return duplicates;
}

static async Task<int> ExecuteAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddRange(parameters);
    return await command.ExecuteNonQueryAsync();
}

static async Task<List<object?[]>> QueryRowsAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddRange(parameters);

    var rows = new List<object?[]>();
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var values = new object?[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        rows.Add(values);
    }

    return rows;
}

static async Task<int?> ExecuteScalarIntAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddRange(parameters);
    var result = await command.ExecuteScalarAsync();
    return result is null or DBNull ? null : Convert.ToInt32(result);
}

static async Task<long> ExecuteScalarLongAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddRange(parameters);
    var result = await command.ExecuteScalarAsync();
    return result is null or DBNull ? 0 : Convert.ToInt64(result);
}

static async Task<string?> ExecuteScalarStringAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.Parameters.AddRange(parameters);
    var result = await command.ExecuteScalarAsync();
    return result is null or DBNull ? null : Convert.ToString(result);
}

static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string table, string column)
{
    await using var command = connection.CreateCommand();
    command.CommandText = $"""PRAGMA table_info("{table}");""";
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

internal sealed record DuplicateDocument(
    int KeepId,
    string KeepCode,
    int DuplicateId,
    string DuplicateTitle,
    string DuplicateCode,
    long ParagraphCount);
