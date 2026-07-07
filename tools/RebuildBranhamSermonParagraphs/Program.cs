using System.Text;
using MessageFlow.Data;
using MessageFlow.Importer;
using Microsoft.Data.Sqlite;

const string BranhamPdfRoot = @"D:\Br William Marrion Branham\PDF";
const string OutputDirectory = @"D:\MessageFlow Archive\BranhamAudit";
const string PreviewReportPath = OutputDirectory + @"\branham_rebuild_preview_report.txt";
const string ApplyReportPath = OutputDirectory + @"\branham_rebuild_apply_report.txt";
const string BaselineBackupPath = @"D:\MessageFlow Archive\Database Backups\messageflow_before_sermon_accuracy_rebuild.db";

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

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    ForeignKeys = true
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();
await EnableForeignKeysAsync(connection);

var sourceContext = new SourceMetadataContext(
    1,
    "brother_branham",
    "Brother Branham",
    "SermonPdfCollection");

var pdfFiles = Directory.EnumerateFiles(BranhamPdfRoot, "*.pdf", SearchOption.AllDirectories)
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToList();

var oldParagraphCount = await CountBranhamParagraphsAsync(connection);
var oldDocumentCount = await CountBranhamDocumentsAsync(connection);
var baselineParagraphCount = File.Exists(BaselineBackupPath)
    ? (long?)await CountBranhamParagraphsInDatabaseAsync(BaselineBackupPath)
    : null;
var baselineDocumentCount = File.Exists(BaselineBackupPath)
    ? (long?)await CountBranhamDocumentsInDatabaseAsync(BaselineBackupPath)
    : null;
var databaseSermons = await LoadDatabaseSermonsAsync(connection);
var extractor = new PdfTextExtractor();
var rebuilds = new List<SermonRebuildPlan>();
var unmatchedFiles = new List<string>();

foreach (var pdfFile in pdfFiles)
{
    var metadata = SermonMetadataParser.Parse(pdfFile, BranhamPdfRoot, sourceContext);
    if (!TryFindDatabaseSermon(databaseSermons, pdfFile, metadata, out var databaseSermon))
    {
        unmatchedFiles.Add(pdfFile);
        continue;
    }

    var pages = extractor.ExtractPages(pdfFile);
    var paragraphs = BranhamParagraphExtractor.Split(pages, metadata);
    var oldParagraphs = await LoadParagraphsAsync(connection, databaseSermon.Id);
    var changed = ParagraphsChanged(oldParagraphs, paragraphs);
    rebuilds.Add(new SermonRebuildPlan(databaseSermon, metadata, pdfFile, oldParagraphs, paragraphs, changed));
}

var newParagraphCount = rebuilds.Sum(plan => plan.NewParagraphs.Count);
var changedSermons = rebuilds.Count(plan => plan.Changed);
var reportPath = apply ? ApplyReportPath : PreviewReportPath;

ApplySummary? applySummary = null;
if (apply)
{
    applySummary = await ApplyRebuildAsync(connection, rebuilds);
    await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(databasePath, Console.WriteLine);
}

await WriteReportAsync(
    reportPath,
    databasePath,
    apply,
    oldDocumentCount,
    baselineDocumentCount,
    pdfFiles.Count,
    oldParagraphCount,
    baselineParagraphCount,
    newParagraphCount,
    changedSermons,
    unmatchedFiles,
    rebuilds,
    applySummary);

Console.WriteLine(apply ? "Branham paragraph rebuild applied." : "Branham paragraph rebuild preview only.");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"PDF files found: {pdfFiles.Count:N0}");
Console.WriteLine($"Matched sermons: {rebuilds.Count:N0}");
Console.WriteLine($"Sermons with changes: {changedSermons:N0}");
Console.WriteLine($"Old Branham paragraph count: {oldParagraphCount:N0}");
Console.WriteLine($"New Branham paragraph count: {newParagraphCount:N0}");
Console.WriteLine($"Report: {reportPath}");

var whyPlan = FindPlan(rebuilds, "63-1214", "Why Little Bethlehem");
if (whyPlan is not null)
{
    Console.WriteLine();
    Console.WriteLine("63-1214 Why Little Bethlehem VGR regression preview");
    Console.WriteLine($"Paragraph 1: {Shorten(whyPlan.NewParagraphs.FirstOrDefault(p => p.ParagraphNumber == 1)?.Text ?? "missing", 220)}");
    Console.WriteLine($"Paragraph 4: {Shorten(whyPlan.NewParagraphs.FirstOrDefault(p => p.ParagraphNumber == 4)?.Text ?? "missing", 300)}");
}

return unmatchedFiles.Count == 0 ? 0 : 2;

static async Task<ApplySummary> ApplyRebuildAsync(
    SqliteConnection connection,
    IReadOnlyList<SermonRebuildPlan> rebuilds)
{
    var summary = new ApplySummary();
    await using var transaction = await connection.BeginTransactionAsync();

    foreach (var plan in rebuilds)
    {
        if (!plan.Changed)
        {
            continue;
        }

        var oldByNumber = plan.OldParagraphs.ToDictionary(
            paragraph => paragraph.ParagraphNumber,
            paragraph => paragraph);
        var newNumbers = plan.NewParagraphs.Select(paragraph => paragraph.ParagraphNumber).ToHashSet();

        foreach (var paragraph in plan.NewParagraphs)
        {
            if (oldByNumber.TryGetValue(paragraph.ParagraphNumber, out var oldParagraph))
            {
                if (!TextsEqual(oldParagraph.Text, paragraph.Text) ||
                    oldParagraph.PageNumber != paragraph.PageNumber)
                {
                    await UpdateParagraphAsync(connection, (SqliteTransaction)transaction, oldParagraph.Id, paragraph);
                    summary.UpdatedParagraphs++;
                }

                continue;
            }

            await InsertParagraphAsync(connection, (SqliteTransaction)transaction, plan.DatabaseSermon.Id, paragraph);
            summary.InsertedParagraphs++;
        }

        foreach (var oldParagraph in plan.OldParagraphs.Where(paragraph => !newNumbers.Contains(paragraph.ParagraphNumber)))
        {
            var favoriteCount = await CountReferencesAsync(
                connection,
                (SqliteTransaction)transaction,
                "FavoriteParagraphs",
                oldParagraph.Id);
            var historyCount = await CountReferencesAsync(
                connection,
                (SqliteTransaction)transaction,
                "ProjectionHistories",
                oldParagraph.Id);

            if (favoriteCount > 0 || historyCount > 0)
            {
                summary.UnmappedReferences.Add(new UnmappedReference(
                    plan.DatabaseSermon.SermonCode,
                    plan.DatabaseSermon.Title,
                    oldParagraph.ParagraphNumber,
                    favoriteCount,
                    historyCount,
                    Shorten(oldParagraph.Text, 160)));
            }

            await DeleteParagraphAsync(connection, (SqliteTransaction)transaction, oldParagraph.Id);
            summary.DeletedParagraphs++;
        }

        summary.RebuiltSermons++;
    }

    await transaction.CommitAsync();
    return summary;
}

static async Task EnableForeignKeysAsync(SqliteConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA foreign_keys = ON;";
    await command.ExecuteNonQueryAsync();
}

static async Task UpdateParagraphAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int paragraphId,
    ParagraphDraft paragraph)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        UPDATE "SermonParagraphs"
        SET "Text" = $text,
            "SearchText" = $searchText,
            "PageNumber" = $pageNumber
        WHERE "Id" = $id;
        """;
    command.Parameters.AddWithValue("$text", paragraph.Text);
    command.Parameters.AddWithValue("$searchText", paragraph.SearchText);
    command.Parameters.AddWithValue("$pageNumber", paragraph.PageNumber is null ? DBNull.Value : paragraph.PageNumber.Value);
    command.Parameters.AddWithValue("$id", paragraphId);
    await command.ExecuteNonQueryAsync();
}

static async Task InsertParagraphAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int sermonId,
    ParagraphDraft paragraph)
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
    command.Parameters.AddWithValue("$paragraphNumber", paragraph.ParagraphNumber);
    command.Parameters.AddWithValue("$text", paragraph.Text);
    command.Parameters.AddWithValue("$searchText", paragraph.SearchText);
    command.Parameters.AddWithValue("$pageNumber", paragraph.PageNumber is null ? DBNull.Value : paragraph.PageNumber.Value);
    await command.ExecuteNonQueryAsync();
}

static async Task DeleteParagraphAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    int paragraphId)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """DELETE FROM "SermonParagraphs" WHERE "Id" = $id;""";
    command.Parameters.AddWithValue("$id", paragraphId);
    await command.ExecuteNonQueryAsync();
}

static async Task<long> CountReferencesAsync(
    SqliteConnection connection,
    SqliteTransaction transaction,
    string tableName,
    int paragraphId)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = $"""
        SELECT COUNT(1)
        FROM "{tableName}"
        WHERE "SermonParagraphId" = $paragraphId;
        """;
    command.Parameters.AddWithValue("$paragraphId", paragraphId);
    var result = await command.ExecuteScalarAsync();
    return result is null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
}

static async Task WriteReportAsync(
    string reportPath,
    string databasePath,
    bool apply,
    long oldDocumentCount,
    long? baselineDocumentCount,
    int pdfFileCount,
    long oldParagraphCount,
    long? baselineParagraphCount,
    int newParagraphCount,
    int changedSermons,
    IReadOnlyCollection<string> unmatchedFiles,
    IReadOnlyList<SermonRebuildPlan> rebuilds,
    ApplySummary? applySummary)
{
    var builder = new StringBuilder();
    builder.AppendLine(apply ? "Brother Branham Sermon Paragraph Rebuild Apply Report" : "Brother Branham Sermon Paragraph Rebuild Preview Report");
    builder.AppendLine(apply ? "======================================================" : "=======================================================");
    builder.AppendLine($"Run time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Database: {databasePath}");
    builder.AppendLine($"PDF root: {BranhamPdfRoot}");
    builder.AppendLine($"Mode: {(apply ? "Apply" : "Preview only")}");
    builder.AppendLine();
    builder.AppendLine($"Brother Branham documents before: {oldDocumentCount:N0}");
    if (baselineDocumentCount is not null && baselineDocumentCount != oldDocumentCount)
    {
        builder.AppendLine($"Brother Branham documents in baseline backup: {baselineDocumentCount.Value:N0}");
    }

    builder.AppendLine($"PDF files found: {pdfFileCount:N0}");
    builder.AppendLine($"Matched sermons: {rebuilds.Count:N0}");
    builder.AppendLine($"Unmatched PDF files: {unmatchedFiles.Count:N0}");
    builder.AppendLine($"Sermons with paragraph changes: {changedSermons:N0}");
    builder.AppendLine($"Old Branham paragraph count: {oldParagraphCount:N0}");
    if (baselineParagraphCount is not null && baselineParagraphCount != oldParagraphCount)
    {
        builder.AppendLine($"Original baseline Branham paragraph count: {baselineParagraphCount.Value:N0}");
    }

    builder.AppendLine($"New Branham paragraph count: {newParagraphCount:N0}");
    builder.AppendLine($"Paragraph count difference: {newParagraphCount - oldParagraphCount:N0}");
    builder.AppendLine("Reason for difference: title/header/footer paragraphs are removed, opening unnumbered body paragraphs are recovered, and page-break continuations are joined to their original paragraph.");
    builder.AppendLine();

    if (applySummary is not null)
    {
        builder.AppendLine("Apply summary");
        builder.AppendLine("-------------");
        builder.AppendLine($"Rebuilt sermons: {applySummary.RebuiltSermons:N0}");
        builder.AppendLine($"Updated paragraphs: {applySummary.UpdatedParagraphs:N0}");
        builder.AppendLine($"Inserted paragraphs: {applySummary.InsertedParagraphs:N0}");
        builder.AppendLine($"Deleted paragraphs: {applySummary.DeletedParagraphs:N0}");
        builder.AppendLine($"Unmapped favorite/history references: {applySummary.UnmappedReferences.Count:N0}");
        builder.AppendLine();
    }

    var whyPlan = FindPlan(rebuilds, "63-1214", "Why Little Bethlehem");
    if (whyPlan is not null)
    {
        builder.AppendLine("Regression: 63-1214 Why Little Bethlehem VGR");
        builder.AppendLine("---------------------------------------------");
        AppendParagraphComparison(builder, whyPlan, 1);
        AppendParagraphComparison(builder, whyPlan, 2);
        AppendParagraphComparison(builder, whyPlan, 4);
        builder.AppendLine();
    }

    foreach (var plan in rebuilds.Where(plan => plan.Changed).Take(50))
    {
        builder.AppendLine($"{plan.DatabaseSermon.SermonCode} {plan.DatabaseSermon.Title}");
        builder.AppendLine($"  PDF: {plan.SourcePdfPath}");
        builder.AppendLine($"  Old paragraphs: {plan.OldParagraphs.Count:N0}");
        builder.AppendLine($"  New paragraphs: {plan.NewParagraphs.Count:N0}");
        builder.AppendLine($"  First new paragraph: {Shorten(plan.NewParagraphs.FirstOrDefault()?.Text ?? string.Empty, 220)}");
        builder.AppendLine();
    }

    if (unmatchedFiles.Count > 0)
    {
        builder.AppendLine("Unmatched PDF files");
        builder.AppendLine("-------------------");
        foreach (var file in unmatchedFiles.Take(100))
        {
            builder.AppendLine(file);
        }

        builder.AppendLine();
    }

    if (applySummary?.UnmappedReferences.Count > 0)
    {
        builder.AppendLine("Unmapped favorite/history references");
        builder.AppendLine("------------------------------------");
        foreach (var reference in applySummary.UnmappedReferences)
        {
            builder.AppendLine($"{reference.SermonCode} {reference.Title} paragraph {reference.ParagraphNumber}: favorites={reference.FavoriteCount}, history={reference.HistoryCount}");
            builder.AppendLine($"  Deleted text: {reference.Snippet}");
        }
    }

    await File.WriteAllTextAsync(reportPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static void AppendParagraphComparison(StringBuilder builder, SermonRebuildPlan plan, int paragraphNumber)
{
    var oldParagraph = plan.OldParagraphs.FirstOrDefault(paragraph => paragraph.ParagraphNumber == paragraphNumber);
    var newParagraph = plan.NewParagraphs.FirstOrDefault(paragraph => paragraph.ParagraphNumber == paragraphNumber);

    builder.AppendLine($"Paragraph {paragraphNumber}");
    builder.AppendLine($"  Database before: {Shorten(oldParagraph?.Text ?? "missing", 280)}");
    builder.AppendLine($"  PDF rebuild    : {Shorten(newParagraph?.Text ?? "missing", 360)}");
}

static async Task<Dictionary<string, DatabaseSermon>> LoadDatabaseSermonsAsync(SqliteConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT s."Id",
               s."SermonCode",
               s."Title",
               s."Year",
               s."SourceFilePath"
        FROM "Sermons" s
        INNER JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham';
        """;

    var sermons = new Dictionary<string, DatabaseSermon>(StringComparer.OrdinalIgnoreCase);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var sermon = new DatabaseSermon(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetString(4));
        sermons[Path.GetFullPath(sermon.SourceFilePath)] = sermon;
        sermons[$"{sermon.SermonCode}|{NormalizeTitle(sermon.Title)}"] = sermon;
    }

    return sermons;
}

static bool TryFindDatabaseSermon(
    IReadOnlyDictionary<string, DatabaseSermon> databaseSermons,
    string pdfFile,
    SermonMetadata metadata,
    out DatabaseSermon sermon)
{
    if (databaseSermons.TryGetValue(Path.GetFullPath(pdfFile), out sermon!))
    {
        return true;
    }

    return databaseSermons.TryGetValue($"{metadata.SermonCode}|{NormalizeTitle(metadata.Title)}", out sermon!);
}

static async Task<List<DatabaseParagraph>> LoadParagraphsAsync(SqliteConnection connection, int sermonId)
{
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT "Id",
               "ParagraphNumber",
               "Text",
               "PageNumber"
        FROM "SermonParagraphs"
        WHERE "SermonId" = $sermonId
        ORDER BY "ParagraphNumber";
        """;
    command.Parameters.AddWithValue("$sermonId", sermonId);

    var paragraphs = new List<DatabaseParagraph>();
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        paragraphs.Add(new DatabaseParagraph(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3)));
    }

    return paragraphs;
}

static bool ParagraphsChanged(
    IReadOnlyList<DatabaseParagraph> oldParagraphs,
    IReadOnlyList<ParagraphDraft> newParagraphs)
{
    if (oldParagraphs.Count != newParagraphs.Count)
    {
        return true;
    }

    var oldByNumber = oldParagraphs.ToDictionary(paragraph => paragraph.ParagraphNumber);
    foreach (var paragraph in newParagraphs)
    {
        if (!oldByNumber.TryGetValue(paragraph.ParagraphNumber, out var oldParagraph) ||
            !TextsEqual(oldParagraph.Text, paragraph.Text) ||
            oldParagraph.PageNumber != paragraph.PageNumber)
        {
            return true;
        }
    }

    return false;
}

static bool TextsEqual(string left, string right)
{
    return string.Equals(NormalizeWhitespace(left), NormalizeWhitespace(right), StringComparison.Ordinal);
}

static string NormalizeWhitespace(string text)
{
    return string.Join(' ', text.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

static string NormalizeTitle(string title)
{
    return NormalizeWhitespace(title)
        .Replace(" VGR", string.Empty, StringComparison.OrdinalIgnoreCase)
        .ToUpperInvariant();
}

static SermonRebuildPlan? FindPlan(IEnumerable<SermonRebuildPlan> plans, string sermonCode, string title)
{
    return plans.FirstOrDefault(plan =>
        string.Equals(plan.DatabaseSermon.SermonCode, sermonCode, StringComparison.OrdinalIgnoreCase) &&
        plan.DatabaseSermon.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
}

static string Shorten(string text, int maxLength)
{
    var normalized = NormalizeWhitespace(text);
    return normalized.Length <= maxLength
        ? normalized
        : $"{normalized[..maxLength].TrimEnd()}...";
}

static async Task<long> CountBranhamDocumentsAsync(SqliteConnection connection)
{
    return await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "Sermons" s
        INNER JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham';
        """);
}

static async Task<long> CountBranhamDocumentsInDatabaseAsync(string databasePath)
{
    await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString());
    await connection.OpenAsync();
    return await CountBranhamDocumentsAsync(connection);
}

static async Task<long> CountBranhamParagraphsAsync(SqliteConnection connection)
{
    return await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "SermonParagraphs" p
        INNER JOIN "Sermons" s ON s."Id" = p."SermonId"
        INNER JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."Name" = 'brother_branham';
        """);
}

static async Task<long> CountBranhamParagraphsInDatabaseAsync(string databasePath)
{
    await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString());
    await connection.OpenAsync();
    return await CountBranhamParagraphsAsync(connection);
}

static async Task<long> ExecuteScalarLongAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    var result = await command.ExecuteScalarAsync();
    return result is null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
}

sealed record DatabaseSermon(
    int Id,
    string SermonCode,
    string Title,
    int Year,
    string SourceFilePath);

sealed record DatabaseParagraph(
    int Id,
    int ParagraphNumber,
    string Text,
    int? PageNumber);

sealed record SermonRebuildPlan(
    DatabaseSermon DatabaseSermon,
    SermonMetadata Metadata,
    string SourcePdfPath,
    IReadOnlyList<DatabaseParagraph> OldParagraphs,
    IReadOnlyList<ParagraphDraft> NewParagraphs,
    bool Changed);

sealed class ApplySummary
{
    public int RebuiltSermons { get; set; }

    public int UpdatedParagraphs { get; set; }

    public int InsertedParagraphs { get; set; }

    public int DeletedParagraphs { get; set; }

    public List<UnmappedReference> UnmappedReferences { get; } = [];
}

sealed record UnmappedReference(
    string SermonCode,
    string Title,
    int ParagraphNumber,
    long FavoriteCount,
    long HistoryCount,
    string Snippet);
