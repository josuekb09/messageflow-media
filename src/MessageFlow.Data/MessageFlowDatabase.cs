using Microsoft.Data.Sqlite;

namespace MessageFlow.Data;

public static class MessageFlowDatabase
{
    public const string DatabaseFolderName = "database";
    public const string DatabaseFileName = "messageflow.db";
    public const string AppDataProductFolderName = "MessageFlow Media";
    public const string PreferredDataDriveLetter = "D";
    public const string UserDataFolderName = "MessageFlowMedia";

    public static string DefaultDatabasePath =>
        ResolveDefaultDatabasePath();

    public static string ExecutableDatabasePath =>
        Path.Combine(AppContext.BaseDirectory, DatabaseFolderName, DatabaseFileName);

    /// <summary>
    /// Writable data root on D:. Never %LocalAppData% on C:.
    /// </summary>
    public static string UserDataRoot =>
        Path.Combine(GetPreferredDataDriveRoot(), UserDataFolderName);

    public static string UserDataDatabasePath =>
        Path.Combine(UserDataRoot, DatabaseFolderName, DatabaseFileName);

    public static string UserSettingsDirectory
    {
        get
        {
            var executableSettings = Path.Combine(AppContext.BaseDirectory, "settings");
            if (IsAllowedDataPath(executableSettings) &&
                DirectoryIsWritable(AppContext.BaseDirectory))
            {
                return executableSettings;
            }

            return Path.Combine(UserDataRoot, "settings");
        }
    }

    /// <summary>
    /// Legacy name. Previously %LocalAppData% on C:; now the D: user-data database.
    /// </summary>
    public static string AppDataDatabasePath => UserDataDatabasePath;

    public static string CreateMissingDatabaseMessage(string databasePath)
    {
        return
            $"The MessageFlow database file is missing:{Environment.NewLine}{Environment.NewLine}" +
            $"{databasePath}{Environment.NewLine}{Environment.NewLine}" +
            "Re-run MessageFlowMediaSetup.exe, or copy messageflow.db into the database folder that MessageFlow is using.";
    }

    public static void EnsureDatabaseDirectory(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static bool IsAllowedDataPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return !IsSystemCDrive(Path.GetFullPath(path));
        }
        catch
        {
            return false;
        }
    }

    public static void WriteLibraryInventory(string databasePath, Action<string> log)
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using (var sermonCommand = connection.CreateCommand())
            {
                sermonCommand.CommandText =
                    """
                    SELECT COALESCE(Language, '(null)'), COUNT(*)
                    FROM Sermons
                    GROUP BY Language
                    ORDER BY Language
                    """;
                using var reader = sermonCommand.ExecuteReader();
                var parts = new List<string>();
                while (reader.Read())
                {
                    parts.Add($"{reader.GetString(0)}={reader.GetInt64(1)}");
                }

                log(parts.Count == 0
                    ? "Library inventory: no sermons found."
                    : "Library inventory sermons: " + string.Join(", ", parts));
            }

            using (var bibleCommand = connection.CreateCommand())
            {
                bibleCommand.CommandText =
                    """
                    SELECT COALESCE(Language, '(null)'), COALESCE(Abbreviation, ''), COALESCE(Name, '')
                    FROM BibleTranslations
                    ORDER BY Language, Abbreviation
                    """;
                using var reader = bibleCommand.ExecuteReader();
                var parts = new List<string>();
                while (reader.Read())
                {
                    var abbreviation = reader.GetString(1);
                    var name = reader.GetString(2);
                    parts.Add($"{reader.GetString(0)}:{abbreviation} ({name})");
                }

                log(parts.Count == 0
                    ? "Library inventory: no Bible translations found."
                    : "Library inventory Bibles: " + string.Join(", ", parts));
            }

            using (var songCommand = connection.CreateCommand())
            {
                songCommand.CommandText =
                    """
                    SELECT COALESCE(Language, '(null)'), COUNT(*)
                    FROM Songs
                    WHERE IsActive = 1
                    GROUP BY Language
                    ORDER BY Language
                    """;
                using var reader = songCommand.ExecuteReader();
                var parts = new List<string>();
                while (reader.Read())
                {
                    parts.Add($"{reader.GetString(0)}={reader.GetInt64(1)}");
                }

                log(parts.Count == 0
                    ? "Library inventory: no songs found."
                    : "Library inventory songs: " + string.Join(", ", parts));
            }
        }
        catch (Exception ex)
        {
            log($"Library inventory could not be read: {ex.Message}");
        }
    }

    private static string ResolveDefaultDatabasePath()
    {
        var candidates = EnumerateDatabaseCandidates().ToList();
        var existing = candidates
            .Where(path => File.Exists(path) && IsAllowedDataPath(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new FileInfo(path))
            .Where(info => info.Exists && info.Length > 0)
            .OrderByDescending(info => info.Length)
            .ThenByDescending(info => info.LastWriteTimeUtc)
            .ToList();

        if (existing.Count > 0)
        {
            return existing[0].FullName;
        }

        var executableDatabasePath = ExecutableDatabasePath;
        if (File.Exists(executableDatabasePath) && !IsAllowedDataPath(executableDatabasePath))
        {
            EnsureDatabaseDirectory(UserDataDatabasePath);
            if (!File.Exists(UserDataDatabasePath))
            {
                try
                {
                    File.Copy(executableDatabasePath, UserDataDatabasePath, overwrite: false);
                }
                catch (IOException) when (File.Exists(UserDataDatabasePath))
                {
                }
            }

            return UserDataDatabasePath;
        }

        if (IsAllowedDataPath(executableDatabasePath) &&
            DirectoryIsWritable(Path.GetDirectoryName(executableDatabasePath) ?? AppContext.BaseDirectory))
        {
            return executableDatabasePath;
        }

        return UserDataDatabasePath;
    }

    private static IEnumerable<string> EnumerateDatabaseCandidates()
    {
        yield return ExecutableDatabasePath;

        var solutionRoot = FindSolutionRoot();
        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            yield return Path.Combine(solutionRoot, DatabaseFolderName, DatabaseFileName);
        }

        yield return UserDataDatabasePath;
    }

    private static string GetPreferredDataDriveRoot()
    {
        var preferred = PreferredDataDriveLetter + @":\";
        if (Directory.Exists(preferred))
        {
            return preferred;
        }

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
            {
                continue;
            }

            if (!IsSystemCDrive(drive.RootDirectory.FullName))
            {
                return drive.RootDirectory.FullName;
            }
        }

        return preferred;
    }

    private static bool IsSystemCDrive(string path)
    {
        var root = Path.GetPathRoot(path);
        return string.Equals(root, @"C:\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DirectoryIsWritable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".mf-write-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MessageFlow.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
