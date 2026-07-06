using System.Text;
using MessageFlow.Data;
using Microsoft.Data.Sqlite;

var databasePath = args.FirstOrDefault(arg => !string.IsNullOrWhiteSpace(arg)) ?? MessageFlowDatabase.DefaultDatabasePath;
var checks = new List<CheckResult>();

if (!File.Exists(databasePath))
{
    Console.WriteLine($"WARN Database not found: {databasePath}");
    return 1;
}

Console.WriteLine($"Database: {databasePath}");

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var songsTableExists = await TableExistsAsync(connection, "Songs");
var sectionsTableExists = await TableExistsAsync(connection, "SongSections");
checks.Add(new CheckResult("Songs table", songsTableExists, songsTableExists ? "Songs table exists." : "Songs table is missing."));
checks.Add(new CheckResult("Song sections table", sectionsTableExists, sectionsTableExists ? "SongSections table exists." : "SongSections table is missing."));

if (songsTableExists && sectionsTableExists)
{
    var songCount = await ExecuteScalarLongAsync(connection, """SELECT COUNT(1) FROM "Songs" WHERE "IsActive" = 1;""");
    checks.Add(new CheckResult("Active song count", songCount is >= 340 and <= 380, $"{songCount:N0} active song(s)."));

    var sectionCount = await ExecuteScalarLongAsync(connection, """SELECT COUNT(1) FROM "SongSections";""");
    checks.Add(new CheckResult("Song section count", sectionCount > songCount, $"{sectionCount:N0} section(s)."));

    var zeroTextSongs = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "Songs" s
        WHERE s."IsActive" = 1
          AND NOT EXISTS (
              SELECT 1
              FROM "SongSections" ss
              WHERE ss."SongId" = s."Id"
                AND length(trim(ss."Text")) > 0
          );
        """);
    checks.Add(new CheckResult("Zero-text songs", zeroTextSongs == 0, $"{zeroTextSongs:N0} zero-text song(s)."));

    foreach (var searchText in new[] { "tell", "tell me the story", "calvary", "amazing love", "great deliverer", "holy words" })
    {
        checks.Add(await CheckSongSearchAsync(connection, searchText));
    }
}

foreach (var check in checks)
{
    Console.WriteLine($"{(check.Passed ? "PASS" : "WARN")} {check.Name}: {check.Message}");
}

return checks.All(check => check.Passed) ? 0 : 1;

static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
{
    var count = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM sqlite_master
        WHERE type = 'table'
          AND name = $tableName;
        """,
        new SqliteParameter("$tableName", tableName));
    return count > 0;
}

static async Task<CheckResult> CheckSongSearchAsync(SqliteConnection connection, string searchText)
{
    var normalized = Normalize(searchText);
    var like = $"%{EscapeLike(normalized)}%";
    var count = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(DISTINCT s."Id")
        FROM "Songs" s
        LEFT JOIN "SongSections" ss ON ss."SongId" = s."Id"
        WHERE s."IsActive" = 1
          AND (
              s."NormalizedTitle" LIKE $like ESCAPE '\'
              OR ss."NormalizedText" LIKE $like ESCAPE '\'
          );
        """,
        new SqliteParameter("$like", like));
    return new CheckResult($"Song search {searchText}", count > 0, $"{count:N0} match(es).");
}

static string Normalize(string value)
{
    var builder = new StringBuilder(value.Length);
    var pendingSpace = false;
    foreach (var character in value)
    {
        if (char.IsWhiteSpace(character) || char.IsPunctuation(character) || char.IsSymbol(character))
        {
            pendingSpace = builder.Length > 0;
            continue;
        }

        if (pendingSpace && builder.Length > 0)
        {
            builder.Append(' ');
            pendingSpace = false;
        }

        builder.Append(char.ToUpperInvariant(character));
    }

    return builder.ToString().Trim();
}

static string EscapeLike(string value)
{
    return value
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);
}

static async Task<long> ExecuteScalarLongAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    foreach (var parameter in parameters)
    {
        command.Parameters.Add(parameter);
    }

    var result = await command.ExecuteScalarAsync();
    return result is null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
}

internal sealed record CheckResult(string Name, bool Passed, string Message);
