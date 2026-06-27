using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace MessageFlow.Data;

public sealed record MessageFlowDatabaseRepairResult(
    string DatabasePath,
    bool FavoriteParagraphsExisted,
    bool ProjectionHistoriesExisted,
    bool ContentSourcesExisted,
    bool SermonsContentSourceIdExisted,
    bool FavoriteParagraphsCreated,
    bool ProjectionHistoriesCreated,
    bool ContentSourcesCreated,
    bool SermonsContentSourceIdCreated);

public static class MessageFlowDatabaseRepair
{
    private const string AddContentSourcesMigrationId = "20260627094500_AddContentSources";
    private const string AddSearchPerformanceMigrationId = "20260627224500_AddSearchPerformanceIndexesAndFts";
    private const string ProductVersion = "10.0.9";

    public static async Task<MessageFlowDatabaseRepairResult> RepairAsync(
        string databasePath,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            MessageFlowDatabase.EnsureDatabaseDirectory(databasePath);
            Batteries_V2.Init();

            log?.Invoke($"Database repair starting. Database path: {databasePath}");

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var favoriteParagraphsExisted = await TableExistsAsync(
                connection,
                "FavoriteParagraphs",
                cancellationToken);

            var projectionHistoriesExisted = await TableExistsAsync(
                connection,
                "ProjectionHistories",
                cancellationToken);

            var contentSourcesExisted = await TableExistsAsync(
                connection,
                "ContentSources",
                cancellationToken);
            var sermonsTableExists = await TableExistsAsync(
                connection,
                "Sermons",
                cancellationToken);
            var sermonParagraphsTableExists = await TableExistsAsync(
                connection,
                "SermonParagraphs",
                cancellationToken);
            var sermonsContentSourceIdExisted = sermonsTableExists &&
                                                await ColumnExistsAsync(
                                                    connection,
                                                    "Sermons",
                                                    "ContentSourceId",
                                                    cancellationToken);

            log?.Invoke($"FavoriteParagraphs exists before repair: {favoriteParagraphsExisted}");
            log?.Invoke($"ProjectionHistories exists before repair: {projectionHistoriesExisted}");
            log?.Invoke($"ContentSources exists before repair: {contentSourcesExisted}");
            log?.Invoke($"Sermons.ContentSourceId exists before repair: {sermonsContentSourceIdExisted}");

            if (!favoriteParagraphsExisted)
            {
                await ExecuteAsync(
                    connection,
                    """
                    CREATE TABLE "FavoriteParagraphs" (
                        "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        "SermonParagraphId" INTEGER NOT NULL,
                        "CreatedAt" TEXT NOT NULL,
                        "Notes" TEXT NULL,
                        FOREIGN KEY ("SermonParagraphId") REFERENCES "SermonParagraphs" ("Id") ON DELETE CASCADE
                    );
                    """,
                    cancellationToken);
            }

            if (!projectionHistoriesExisted)
            {
                await ExecuteAsync(
                    connection,
                    """
                    CREATE TABLE "ProjectionHistories" (
                        "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        "SermonParagraphId" INTEGER NOT NULL,
                        "ProjectedAt" TEXT NOT NULL,
                        "SearchQuery" TEXT NULL,
                        FOREIGN KEY ("SermonParagraphId") REFERENCES "SermonParagraphs" ("Id") ON DELETE CASCADE
                    );
                    """,
                    cancellationToken);
            }

            if (!contentSourcesExisted)
            {
                await ExecuteAsync(
                    connection,
                    """
                    CREATE TABLE "ContentSources" (
                        "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        "Name" TEXT NOT NULL,
                        "DisplayName" TEXT NOT NULL,
                        "SourceType" TEXT NOT NULL,
                        "Description" TEXT NOT NULL,
                        "LocalFolderPath" TEXT NULL,
                        "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    """,
                    cancellationToken);
            }

            await ExecuteAsync(
                connection,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ContentSources_Name"
                ON "ContentSources" ("Name");
                """,
                cancellationToken);

            await ExecuteAsync(
                connection,
                """
                INSERT INTO "ContentSources" (
                    "Id",
                    "Name",
                    "DisplayName",
                    "SourceType",
                    "Description",
                    "LocalFolderPath",
                    "CreatedAt"
                )
                VALUES (
                    1,
                    'brother_branham',
                    'Brother Branham',
                    'SermonPdfCollection',
                    'Local Brother William Marrion Branham sermon PDF library.',
                    'D:\Br William Marrion Branham\PDF',
                    CURRENT_TIMESTAMP
                )
                ON CONFLICT("Name") DO UPDATE SET
                    "DisplayName" = excluded."DisplayName",
                    "SourceType" = excluded."SourceType",
                    "Description" = excluded."Description",
                    "LocalFolderPath" = excluded."LocalFolderPath";
                """,
                cancellationToken);

            if (sermonsTableExists && !sermonsContentSourceIdExisted)
            {
                await ExecuteAsync(
                    connection,
                    """
                    ALTER TABLE "Sermons"
                    ADD COLUMN "ContentSourceId" INTEGER NULL;
                    """,
                    cancellationToken);
            }

            if (sermonsTableExists)
            {
                await ExecuteAsync(
                    connection,
                    """
                    UPDATE "Sermons"
                    SET "ContentSourceId" = (
                        SELECT "Id"
                        FROM "ContentSources"
                        WHERE "Name" = 'brother_branham'
                        LIMIT 1
                    )
                    WHERE "ContentSourceId" IS NULL;
                    """,
                    cancellationToken);

                await ExecuteAsync(
                    connection,
                    """
                    CREATE INDEX IF NOT EXISTS "IX_Sermons_ContentSourceId"
                    ON "Sermons" ("ContentSourceId");
                    """,
                    cancellationToken);
            }

            await MarkMigrationAppliedIfHistoryExistsAsync(
                connection,
                AddContentSourcesMigrationId,
                cancellationToken);

            await ExecuteAsync(
                connection,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FavoriteParagraphs_SermonParagraphId"
                ON "FavoriteParagraphs" ("SermonParagraphId");
                """,
                cancellationToken);

            await ExecuteAsync(
                connection,
                """
                CREATE INDEX IF NOT EXISTS "IX_ProjectionHistories_SermonParagraphId"
                ON "ProjectionHistories" ("SermonParagraphId");
                """,
                cancellationToken);

            await ExecuteAsync(
                connection,
                """
                CREATE INDEX IF NOT EXISTS "IX_ProjectionHistories_ProjectedAt"
                ON "ProjectionHistories" ("ProjectedAt");
                """,
                cancellationToken);

            if (sermonsTableExists)
            {
                await EnsureSearchPerformanceIndexesAsync(
                    connection,
                    sermonsContentSourceIdExisted ||
                    await ColumnExistsAsync(connection, "Sermons", "ContentSourceId", cancellationToken),
                    sermonParagraphsTableExists,
                    log,
                    cancellationToken);
            }

            if (sermonsTableExists && sermonParagraphsTableExists)
            {
                await TryEnsureSermonParagraphsFtsAsync(connection, log, cancellationToken);
            }

            await MarkMigrationAppliedIfHistoryExistsAsync(
                connection,
                AddSearchPerformanceMigrationId,
                cancellationToken);

            var result = new MessageFlowDatabaseRepairResult(
                databasePath,
                favoriteParagraphsExisted,
                projectionHistoriesExisted,
                contentSourcesExisted,
                sermonsContentSourceIdExisted,
                !favoriteParagraphsExisted,
                !projectionHistoriesExisted,
                !contentSourcesExisted,
                sermonsTableExists && !sermonsContentSourceIdExisted);

            log?.Invoke($"FavoriteParagraphs created by repair: {result.FavoriteParagraphsCreated}");
            log?.Invoke($"ProjectionHistories created by repair: {result.ProjectionHistoriesCreated}");
            log?.Invoke($"ContentSources created by repair: {result.ContentSourcesCreated}");
            log?.Invoke($"Sermons.ContentSourceId created by repair: {result.SermonsContentSourceIdCreated}");
            log?.Invoke("Database repair completed.");

            return result;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Database repair failed: {ex}");
            throw;
        }
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = $tableName;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""PRAGMA table_info("{tableName}");""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task MarkMigrationAppliedIfHistoryExistsAsync(
        SqliteConnection connection,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ($migrationId, $productVersion);
            """;
        command.Parameters.AddWithValue("$migrationId", migrationId);
        command.Parameters.AddWithValue("$productVersion", ProductVersion);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureSearchPerformanceIndexesAsync(
        SqliteConnection connection,
        bool hasContentSourceId,
        bool hasSermonParagraphs,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        log?.Invoke("Ensuring search performance indexes.");

        await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_Title" ON "Sermons" ("Title");""", cancellationToken);
        await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_SermonCode" ON "Sermons" ("SermonCode");""", cancellationToken);
        await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_Year" ON "Sermons" ("Year");""", cancellationToken);
        await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_AuthorId" ON "Sermons" ("AuthorId");""", cancellationToken);
        await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_SermonCode_Year" ON "Sermons" ("SermonCode", "Year");""", cancellationToken);

        if (hasContentSourceId)
        {
            await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_Sermons_ContentSourceId" ON "Sermons" ("ContentSourceId");""", cancellationToken);
        }

        if (hasSermonParagraphs)
        {
            await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_SermonId" ON "SermonParagraphs" ("SermonId");""", cancellationToken);
            await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_ParagraphNumber" ON "SermonParagraphs" ("ParagraphNumber");""", cancellationToken);
            await ExecuteAsync(connection, """CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_SearchText" ON "SermonParagraphs" ("SearchText");""", cancellationToken);
            await ExecuteAsync(connection, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_SermonParagraphs_SermonId_ParagraphNumber" ON "SermonParagraphs" ("SermonId", "ParagraphNumber");""", cancellationToken);
        }
    }

    private static async Task TryEnsureSermonParagraphsFtsAsync(
        SqliteConnection connection,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(
                connection,
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS "SermonParagraphsFts"
                USING fts5(
                    ParagraphId UNINDEXED,
                    SermonId UNINDEXED,
                    Title,
                    SermonCode,
                    ParagraphNumber UNINDEXED,
                    SearchText,
                    tokenize='unicode61'
                );
                """,
                cancellationToken);

            await EnsureSermonParagraphsFtsTriggersAsync(connection, cancellationToken);

            var paragraphCount = await ExecuteScalarLongAsync(
                connection,
                """SELECT COUNT(1) FROM "SermonParagraphs";""",
                cancellationToken);
            var ftsCount = await ExecuteScalarLongAsync(
                connection,
                """SELECT COUNT(1) FROM "SermonParagraphsFts";""",
                cancellationToken);

            if (paragraphCount != ftsCount)
            {
                log?.Invoke($"Rebuilding SermonParagraphsFts. Paragraphs: {paragraphCount}. FTS rows: {ftsCount}.");
                await RebuildSermonParagraphsFtsAsync(connection, cancellationToken);
                log?.Invoke("SermonParagraphsFts rebuild completed.");
                return;
            }

            log?.Invoke($"SermonParagraphsFts is ready. Rows: {ftsCount}.");
        }
        catch (SqliteException ex)
        {
            log?.Invoke($"SQLite FTS5 is unavailable. Search will use indexed LIKE fallback. Details: {ex.Message}");
        }
    }

    private static async Task EnsureSermonParagraphsFtsTriggersAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection,
            """
            CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_ai"
            AFTER INSERT ON "SermonParagraphs"
            BEGIN
                INSERT INTO "SermonParagraphsFts" (
                    rowid,
                    ParagraphId,
                    SermonId,
                    Title,
                    SermonCode,
                    ParagraphNumber,
                    SearchText
                )
                SELECT
                    new."Id",
                    new."Id",
                    new."SermonId",
                    s."Title",
                    s."SermonCode",
                    new."ParagraphNumber",
                    new."SearchText"
                FROM "Sermons" s
                WHERE s."Id" = new."SermonId";
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            connection,
            """
            CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_ad"
            AFTER DELETE ON "SermonParagraphs"
            BEGIN
                DELETE FROM "SermonParagraphsFts"
                WHERE rowid = old."Id";
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            connection,
            """
            CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_au"
            AFTER UPDATE ON "SermonParagraphs"
            BEGIN
                DELETE FROM "SermonParagraphsFts"
                WHERE rowid = old."Id";

                INSERT INTO "SermonParagraphsFts" (
                    rowid,
                    ParagraphId,
                    SermonId,
                    Title,
                    SermonCode,
                    ParagraphNumber,
                    SearchText
                )
                SELECT
                    new."Id",
                    new."Id",
                    new."SermonId",
                    s."Title",
                    s."SermonCode",
                    new."ParagraphNumber",
                    new."SearchText"
                FROM "Sermons" s
                WHERE s."Id" = new."SermonId";
            END;
            """,
            cancellationToken);
    }

    private static async Task RebuildSermonParagraphsFtsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, """DELETE FROM "SermonParagraphsFts";""", cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO "SermonParagraphsFts" (
                rowid,
                ParagraphId,
                SermonId,
                Title,
                SermonCode,
                ParagraphNumber,
                SearchText
            )
            SELECT
                p."Id",
                p."Id",
                p."SermonId",
                s."Title",
                s."SermonCode",
                p."ParagraphNumber",
                p."SearchText"
            FROM "SermonParagraphs" p
            JOIN "Sermons" s ON s."Id" = p."SermonId";
            """,
            cancellationToken);
    }

    private static async Task<long> ExecuteScalarLongAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
