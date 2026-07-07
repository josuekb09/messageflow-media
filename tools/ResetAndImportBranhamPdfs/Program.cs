using System.Globalization;
using System.Text;
using MessageFlow.Data;
using MessageFlow.Importer;
using Microsoft.Data.Sqlite;

const string BranhamPdfRoot = @"D:\Br William Marrion Branham\PDF";
const string OutputDirectory = @"D:\MessageFlow Archive\BranhamAudit";
const string PreviewReportPath = OutputDirectory + @"\pdf_first_import_preview_report.txt";
const string ApplyReportPath = OutputDirectory + @"\pdf_first_import_apply_report.txt";

Directory.CreateDirectory(OutputDirectory);

var apply = args.Any(arg => string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase));
var databasePath = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ??
                   MessageFlowDatabase.DefaultDatabasePath;

if (!Directory.Exists(BranhamPdfRoot))
{
    Console.Error.WriteLine($"Brother Branham PDF folder not found: {BranhamPdfRoot}");
    return 1;
}

if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Database not found: {databasePath}");
    return 1;
}

var sourceContext = new SourceMetadataContext(
    1,
    "brother_branham",
    "Brother Branham",
    "SermonPdfCollection");
var pdfFiles = Directory.EnumerateFiles(BranhamPdfRoot, "*.pdf", SearchOption.AllDirectories)
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToList();
var extractor = new PdfTextExtractor();
var plans = new List<PdfImportPlan>();
var failures = new List<FailedPdf>();
var processed = 0;

foreach (var pdfFile in pdfFiles)
{
    processed++;
    if (processed % 100 == 0)
    {
        Console.WriteLine($"Scanned {processed:N0}/{pdfFiles.Count:N0} PDFs...");
    }

    try
    {
        var metadata = SermonMetadataParser.Parse(pdfFile, BranhamPdfRoot, sourceContext);
        var pages = extractor.ExtractPages(pdfFile);
        var blocks = PdfFirstBranhamBlockExtractor.Split(pages, metadata);
        if (blocks.Count == 0)
        {
            failures.Add(new FailedPdf(pdfFile, "No sermon body blocks were extracted."));
            continue;
        }

        plans.Add(new PdfImportPlan(pdfFile, metadata, blocks));
    }
    catch (Exception ex)
    {
        failures.Add(new FailedPdf(pdfFile, ex.Message));
    }
}

ResetSummary? resetSummary = null;
if (apply && failures.Count == 0)
{
    resetSummary = await ApplyResetImportAsync(databasePath, plans);
    await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(databasePath, Console.WriteLine);
}

var reportPath = apply ? ApplyReportPath : PreviewReportPath;
await WriteReportAsync(reportPath, databasePath, apply, pdfFiles.Count, plans, failures, resetSummary);

Console.WriteLine(apply ? "PDF-first Brother Branham reset/import applied." : "PDF-first Brother Branham reset/import preview only.");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"PDFs found: {pdfFiles.Count:N0}");
Console.WriteLine($"PDFs ready/imported: {plans.Count:N0}");
Console.WriteLine($"Failed PDFs: {failures.Count:N0}");
Console.WriteLine($"Sermon blocks: {plans.Sum(plan => plan.Blocks.Count):N0}");
Console.WriteLine($"Report: {reportPath}");

if (apply && failures.Count > 0)
{
    Console.Error.WriteLine("Apply was skipped because one or more PDFs failed in preview.");
    return 2;
}

return failures.Count == 0 ? 0 : 2;

static async Task<ResetSummary> ApplyResetImportAsync(
    string databasePath,
    IReadOnlyList<PdfImportPlan> plans)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        ForeignKeys = true
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
    await ExecuteRootNonQueryAsync(connection, "PRAGMA foreign_keys = ON;");

    await using var transaction = await connection.BeginTransactionAsync();
    var sqliteTransaction = (SqliteTransaction)transaction;

    var authorId = await EnsureBrotherBranhamAuthorAsync(connection, sqliteTransaction);
    var sourceId = await EnsureBrotherBranhamSourceAsync(connection, sqliteTransaction);
    var existingSermons = await CountExistingBranhamSermonsAsync(connection, sqliteTransaction, sourceId);
    var existingBlocks = await CountExistingBranhamBlocksAsync(connection, sqliteTransaction, sourceId);

    await DeleteExistingBranhamDataAsync(connection, sqliteTransaction, sourceId);

    var importedSermons = 0;
    var importedBlocks = 0;
    foreach (var plan in plans)
    {
        var sermonId = await InsertSermonAsync(connection, sqliteTransaction, authorId, sourceId, plan);
        importedSermons++;

        foreach (var block in plan.Blocks)
        {
            await InsertParagraphAsync(connection, sqliteTransaction, sermonId, block);
            importedBlocks++;
        }

        await InsertImportLogAsync(
            connection,
            sqliteTransaction,
            plan.SourcePdfPath,
            "Imported",
            $"PDF-first import wrote {plan.Blocks.Count:N0} block(s).");
    }

    await transaction.CommitAsync();
    return new ResetSummary(existingSermons, existingBlocks, importedSermons, importedBlocks);
}

static async Task<int> EnsureBrotherBranhamAuthorAsync(
    SqliteConnection connection,
    SqliteTransaction transaction)
{
    var existingId = await ExecuteScalarIntAsync(
        connection,
        transaction,
        """SELECT "Id" FROM "Authors" WHERE "FullName" = 'William Marrion Branham' LIMIT 1;""");
    if (existingId is not null)
    {
        return existingId.Value;
    }

    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO "Authors" ("FullName", "DisplayName", "Description")
        VALUES ('William Marrion Branham', 'Brother Branham', 'Primary sermon author for the local MessageFlow sermon library.');
        SELECT last_insert_rowid();
        """;
    return Convert.ToInt32(await command.ExecuteScalarAsync());
}

static async Task<int> EnsureBrotherBranhamSourceAsync(
    SqliteConnection connection,
    SqliteTransaction transaction)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO "ContentSources" (
            "Name",
            "DisplayName",
            "SourceType",
            "Description",
            "LocalFolderPath",
            "CreatedAt")
        VALUES (
            'brother_branham',
            'Brother Branham',
            'SermonPdfCollection',
            'Local Brother William Marrion Branham sermon PDF library.',
            $folder,
            CURRENT_TIMESTAMP)
        ON CONFLICT("Name") DO UPDATE SET
            "DisplayName" = excluded."DisplayName",
            "SourceType" = excluded."SourceType",
            "Description" = excluded."Description",
            "LocalFolderPath" = excluded."LocalFolderPath";

        SELECT "Id"
        FROM "ContentSources"
        WHERE "Name" = 'brother_branham'
        LIMIT 1;
        """;
    command.Parameters.AddWithValue("$folder", BranhamPdfRoot);
    return Convert.ToInt32(await command.ExecuteScalarAsync());
}

static async Task DeleteExistingBranhamDataAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int sourceId)
{
    const string sermonFilter = """
        SELECT "Id"
        FROM "Sermons"
        WHERE "ContentSourceId" = $sourceId
           OR "SourceFilePath" LIKE $sourcePathPrefix
        """;

    await ExecuteTransactionNonQueryAsync(
        connection,
        transaction,
        $"""
        DELETE FROM "FavoriteParagraphs"
        WHERE "SermonParagraphId" IN (
            SELECT p."Id"
            FROM "SermonParagraphs" p
            WHERE p."SermonId" IN ({sermonFilter})
        );
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));

    await ExecuteTransactionNonQueryAsync(
        connection,
        transaction,
        $"""
        DELETE FROM "ProjectionHistories"
        WHERE "SermonParagraphId" IN (
            SELECT p."Id"
            FROM "SermonParagraphs" p
            WHERE p."SermonId" IN ({sermonFilter})
        );
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));

    await ExecuteTransactionNonQueryAsync(
        connection,
        transaction,
        $"""
        DELETE FROM "SermonParagraphs"
        WHERE "SermonId" IN ({sermonFilter});
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));

    await ExecuteTransactionNonQueryAsync(
        connection,
        transaction,
        """
        DELETE FROM "ImportLogs"
        WHERE "FilePath" LIKE $sourcePathPrefix;
        """,
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));

    await ExecuteTransactionNonQueryAsync(
        connection,
        transaction,
        """
        DELETE FROM "Sermons"
        WHERE "ContentSourceId" = $sourceId
           OR "SourceFilePath" LIKE $sourcePathPrefix;
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));
}

static async Task<int> InsertSermonAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int authorId,
    int sourceId,
    PdfImportPlan plan)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO "Sermons" (
            "AuthorId",
            "ContentSourceId",
            "Title",
            "SermonCode",
            "Year",
            "Date",
            "Location",
            "Language",
            "SourceFilePath",
            "CreatedAt")
        VALUES (
            $authorId,
            $sourceId,
            $title,
            $sermonCode,
            $year,
            $date,
            $location,
            $language,
            $sourceFilePath,
            CURRENT_TIMESTAMP);
        SELECT last_insert_rowid();
        """;
    command.Parameters.AddWithValue("$authorId", authorId);
    command.Parameters.AddWithValue("$sourceId", sourceId);
    command.Parameters.AddWithValue("$title", plan.Metadata.Title);
    command.Parameters.AddWithValue("$sermonCode", plan.Metadata.SermonCode);
    command.Parameters.AddWithValue("$year", ResolveYear(plan.SourcePdfPath, plan.Metadata.Year));
    command.Parameters.AddWithValue("$date", plan.Metadata.Date is null ? DBNull.Value : plan.Metadata.Date.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
    command.Parameters.AddWithValue("$location", plan.Metadata.Location is null ? DBNull.Value : plan.Metadata.Location);
    command.Parameters.AddWithValue("$language", plan.Metadata.Language);
    command.Parameters.AddWithValue("$sourceFilePath", plan.SourcePdfPath);

    return Convert.ToInt32(await command.ExecuteScalarAsync());
}

static async Task InsertParagraphAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int sermonId,
    ParagraphDraft block)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO "SermonParagraphs" (
            "SermonId",
            "ParagraphNumber",
            "Text",
            "SearchText",
            "PageNumber",
            "CreatedAt")
        VALUES (
            $sermonId,
            $paragraphNumber,
            $text,
            $searchText,
            $pageNumber,
            CURRENT_TIMESTAMP);
        """;
    command.Parameters.AddWithValue("$sermonId", sermonId);
    command.Parameters.AddWithValue("$paragraphNumber", block.ParagraphNumber);
    command.Parameters.AddWithValue("$text", block.Text);
    command.Parameters.AddWithValue("$searchText", block.SearchText);
    command.Parameters.AddWithValue("$pageNumber", block.PageNumber is null ? DBNull.Value : block.PageNumber.Value);
    await command.ExecuteNonQueryAsync();
}

static async Task InsertImportLogAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    string filePath,
    string status,
    string message)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO "ImportLogs" ("FilePath", "Status", "Message", "ImportedAt")
        VALUES ($filePath, $status, $message, CURRENT_TIMESTAMP);
        """;
    command.Parameters.AddWithValue("$filePath", filePath);
    command.Parameters.AddWithValue("$status", status);
    command.Parameters.AddWithValue("$message", message);
    await command.ExecuteNonQueryAsync();
}

static int ResolveYear(string sourcePdfPath, int metadataYear)
{
    var parentName = Path.GetFileName(Path.GetDirectoryName(sourcePdfPath));
    return int.TryParse(parentName, CultureInfo.InvariantCulture, out var folderYear) &&
           folderYear is >= 1800 and <= 2200
        ? folderYear
        : metadataYear;
}

static async Task<long> CountExistingBranhamSermonsAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int sourceId)
{
    return await ExecuteScalarLongAsync(
        connection,
        transaction,
        """
        SELECT COUNT(1)
        FROM "Sermons"
        WHERE "ContentSourceId" = $sourceId
           OR "SourceFilePath" LIKE $sourcePathPrefix;
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));
}

static async Task<long> CountExistingBranhamBlocksAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int sourceId)
{
    return await ExecuteScalarLongAsync(
        connection,
        transaction,
        """
        SELECT COUNT(1)
        FROM "SermonParagraphs" p
        INNER JOIN "Sermons" s ON s."Id" = p."SermonId"
        WHERE s."ContentSourceId" = $sourceId
           OR s."SourceFilePath" LIKE $sourcePathPrefix;
        """,
        new SqliteParameter("$sourceId", sourceId),
        new SqliteParameter("$sourcePathPrefix", BranhamPdfRoot + "%"));
}

static async Task WriteReportAsync(
    string reportPath,
    string databasePath,
    bool apply,
    int pdfFileCount,
    IReadOnlyList<PdfImportPlan> plans,
    IReadOnlyList<FailedPdf> failures,
    ResetSummary? resetSummary)
{
    var builder = new StringBuilder();
    builder.AppendLine(apply ? "PDF-First Brother Branham Import Apply Report" : "PDF-First Brother Branham Import Preview Report");
    builder.AppendLine(apply ? "==============================================" : "================================================");
    builder.AppendLine($"Run time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Mode: {(apply ? "Apply" : "Preview only")}");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"PDF root: {BranhamPdfRoot}");
    builder.AppendLine();
    builder.AppendLine($"PDF files found: {pdfFileCount:N0}");
    builder.AppendLine($"PDF files ready/imported: {plans.Count:N0}");
    builder.AppendLine($"Failed PDFs: {failures.Count:N0}");
    builder.AppendLine($"Sermons ready/imported: {plans.Count:N0}");
    builder.AppendLine($"Blocks ready/imported: {plans.Sum(plan => plan.Blocks.Count):N0}");
    builder.AppendLine("Import strategy: PDF page order is the authority. Clear leading paragraph numbers are kept only when they preserve monotonic PDF order; otherwise page/block order wins.");
    builder.AppendLine("Cleanup strategy: remove repeated PDF headers/footers and trailing VGR publication/copyright pages; keep editorial bracket text.");
    builder.AppendLine();

    if (resetSummary is not null)
    {
        builder.AppendLine("Reset summary");
        builder.AppendLine("-------------");
        builder.AppendLine($"Old Brother Branham sermons reset: {resetSummary.OldSermons:N0}");
        builder.AppendLine($"Old Brother Branham blocks reset: {resetSummary.OldBlocks:N0}");
        builder.AppendLine($"New Brother Branham sermons imported: {resetSummary.NewSermons:N0}");
        builder.AppendLine($"New Brother Branham blocks imported: {resetSummary.NewBlocks:N0}");
        builder.AppendLine();
    }

    AppendGoldenSample(builder, plans, "58-1228", "Why Little Bethlehem");
    AppendGoldenSample(builder, plans, "63-1201X", "Wedding Ceremony");
    AppendGoldenSample(builder, plans, "63-1214", "Why Little Bethlehem");

    if (failures.Count > 0)
    {
        builder.AppendLine("Failed PDFs");
        builder.AppendLine("-----------");
        foreach (var failure in failures)
        {
            builder.AppendLine($"{failure.SourcePdfPath}: {failure.Error}");
        }
    }

    await File.WriteAllTextAsync(reportPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static void AppendGoldenSample(StringBuilder builder, IReadOnlyList<PdfImportPlan> plans, string code, string titlePart)
{
    var plan = plans.FirstOrDefault(plan =>
        plan.Metadata.SermonCode.Equals(code, StringComparison.OrdinalIgnoreCase) &&
        plan.Metadata.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase));
    if (plan is null)
    {
        builder.AppendLine($"Golden sample missing: {code} {titlePart}");
        builder.AppendLine();
        return;
    }

    builder.AppendLine($"Sample: {plan.Metadata.SermonCode} {plan.Metadata.Title}");
    builder.AppendLine($"PDF: {plan.SourcePdfPath}");
    builder.AppendLine($"Blocks: {plan.Blocks.Count:N0}");
    foreach (var block in plan.Blocks.Take(5))
    {
        builder.AppendLine($"  Block {block.ParagraphNumber} | Page {block.PageNumber?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} | {Shorten(block.Text, 260)}");
    }

    builder.AppendLine();
}

static async Task ExecuteRootNonQueryAsync(
    SqliteConnection connection,
    string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

static async Task ExecuteTransactionNonQueryAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    foreach (var parameter in parameters)
    {
        command.Parameters.Add(parameter);
    }

    await command.ExecuteNonQueryAsync();
}

static async Task<int?> ExecuteScalarIntAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    string sql)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    var result = await command.ExecuteScalarAsync();
    return result is null || result == DBNull.Value ? null : Convert.ToInt32(result);
}

static async Task<long> ExecuteScalarLongAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = sql;
    foreach (var parameter in parameters)
    {
        command.Parameters.Add(parameter);
    }

    var result = await command.ExecuteScalarAsync();
    return result is null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
}

static string Shorten(string text, int maxLength)
{
    var normalized = string.Join(' ', text.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    return normalized.Length <= maxLength
        ? normalized
        : $"{normalized[..maxLength].TrimEnd()}...";
}

sealed record PdfImportPlan(
    string SourcePdfPath,
    SermonMetadata Metadata,
    IReadOnlyList<ParagraphDraft> Blocks);

sealed record FailedPdf(string SourcePdfPath, string Error);

sealed record ResetSummary(
    long OldSermons,
    long OldBlocks,
    int NewSermons,
    int NewBlocks);
