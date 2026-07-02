namespace MessageFlow.Data;

public static class MessageFlowDatabase
{
    public const string DatabaseFolderName = "database";
    public const string DatabaseFileName = "messageflow.db";

    public static string DefaultDatabasePath =>
        ResolveDefaultDatabasePath();

    public static string ExecutableDatabasePath =>
        Path.Combine(AppContext.BaseDirectory, DatabaseFolderName, DatabaseFileName);

    public static string CreateMissingDatabaseMessage(string databasePath)
    {
        return
            $"The MessageFlow database file is missing:{Environment.NewLine}{Environment.NewLine}" +
            $"{databasePath}{Environment.NewLine}{Environment.NewLine}" +
            "For a church release, copy the whole MessageFlow folder together and make sure the database folder contains messageflow.db.";
    }

    public static void EnsureDatabaseDirectory(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string ResolveDefaultDatabasePath()
    {
        var executableDatabasePath = ExecutableDatabasePath;
        if (File.Exists(executableDatabasePath))
        {
            return executableDatabasePath;
        }

        var solutionRoot = FindSolutionRoot();
        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            return Path.Combine(solutionRoot, DatabaseFolderName, DatabaseFileName);
        }

        return executableDatabasePath;
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
