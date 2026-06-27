using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace MessageFlow.Data;

public sealed record MessageFlowDatabaseRepairResult(
    string DatabasePath,
    bool FavoriteParagraphsExisted,
    bool ProjectionHistoriesExisted,
    bool FavoriteParagraphsCreated,
    bool ProjectionHistoriesCreated);

public static class MessageFlowDatabaseRepair
{
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

            log?.Invoke($"FavoriteParagraphs exists before repair: {favoriteParagraphsExisted}");
            log?.Invoke($"ProjectionHistories exists before repair: {projectionHistoriesExisted}");

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

            var result = new MessageFlowDatabaseRepairResult(
                databasePath,
                favoriteParagraphsExisted,
                projectionHistoriesExisted,
                !favoriteParagraphsExisted,
                !projectionHistoriesExisted);

            log?.Invoke($"FavoriteParagraphs created by repair: {result.FavoriteParagraphsCreated}");
            log?.Invoke($"ProjectionHistories created by repair: {result.ProjectionHistoriesCreated}");
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
