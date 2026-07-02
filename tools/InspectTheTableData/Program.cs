using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Data.Sqlite;
using SQLitePCL;

const string ShortcutPath = @"D:\My Projects\MessageFlow\The Table.lnk";
const string ReportPath = @"D:\MessageFlow Archive\TheTableInspection\the_table_inspection_report.txt";

var inspector = new TheTableInspector(ShortcutPath, ReportPath);
inspector.RunAndWriteReport();

Console.WriteLine($"The Table inspection report written to: {ReportPath}");

internal sealed class TheTableInspector
{
    private static readonly string[] SearchTerms =
    [
        "The Table",
        "Table",
        "VGR",
        "Voice Of God",
        "VoiceOfGod",
        "Branham",
        "sermon",
        "sermons",
        "message",
        "messages"
    ];

    private static readonly HashSet<string> DataExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".db",
        ".sqlite",
        ".sqlite3",
        ".json",
        ".xml",
        ".txt",
        ".html",
        ".htm",
        ".csv",
        ".dat",
        ".bin",
        ".pak",
        ".asar",
        ".zip",
        ".gz",
        ".mp3",
        ".m4a"
    };

    private static readonly HashSet<string> LuceneExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cfe",
        ".cfs",
        ".doc",
        ".fdt",
        ".fdx",
        ".fnm",
        ".pos",
        ".si",
        ".tim",
        ".tip",
        ".nvd",
        ".nvm",
        ".gen",
        ".proto"
    };

    private static readonly HashSet<string> DatabaseExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".db",
        ".sqlite",
        ".sqlite3"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json",
        ".xml",
        ".txt",
        ".html",
        ".htm",
        ".csv"
    };

    private static readonly Regex SermonCodeRegex = new(@"\b\d{2}-\d{4}[A-Z]?\b", RegexOptions.Compiled);
    private static readonly Regex ParagraphNumberRegex = new(@"(?m)(?:^|[\r\n>\s])(?:paragraph\s*)?\d{1,4}(?:\.|\s{1,3})(?=[A-Z""'])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AudioPathRegex = new(@"\.(?:mp3|m4a)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateYearRegex = new(@"\b(?:19|20)\d{2}\b", RegexOptions.Compiled);

    private readonly string shortcutPath;
    private readonly string reportPath;

    public TheTableInspector(string shortcutPath, string reportPath)
    {
        this.shortcutPath = shortcutPath;
        this.reportPath = reportPath;
    }

    public void RunAndWriteReport()
    {
        var result = Run();
        WriteReport(result);
    }

    private InspectionResult Run()
    {
        Batteries_V2.Init();

        var shortcut = ResolveShortcut(shortcutPath);
        var roots = BuildSearchRoots(shortcut);
        var folders = new List<FolderCandidate>();
        var files = new List<FileCandidate>();
        var searchWarnings = new List<string>();

        SearchRoots(roots, folders, files, searchWarnings);

        var luceneIndexes = InspectLuceneIndexes(roots, searchWarnings);
        var databases = InspectDatabases(files, searchWarnings);
        var textFiles = InspectTextFiles(files, searchWarnings);
        var archives = InspectArchives(files, searchWarnings);
        var conclusion = BuildConclusion(databases, textFiles, files, luceneIndexes);

        return new InspectionResult(
            shortcut,
            roots,
            folders,
            files,
            luceneIndexes,
            databases,
            textFiles,
            archives,
            searchWarnings,
            conclusion);
    }

    private void WriteReport(InspectionResult result)
    {
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        File.WriteAllText(reportPath, BuildReport(result), Encoding.UTF8);
    }

    private ShortcutInspection ResolveShortcut(string path)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            return new ShortcutInspection(path, false, string.Empty, string.Empty, string.Empty, string.Empty, "Shortcut file was not found.");
        }

        object? shellObject = null;
        object? shortcutObject = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return new ShortcutInspection(path, true, string.Empty, string.Empty, string.Empty, string.Empty, "WScript.Shell COM object is not available.");
            }

            shellObject = Activator.CreateInstance(shellType);
            if (shellObject is null)
            {
                return new ShortcutInspection(path, true, string.Empty, string.Empty, string.Empty, string.Empty, "Could not create WScript.Shell COM object.");
            }

            dynamic shell = shellObject;
            shortcutObject = shell.CreateShortcut(path);
            dynamic shortcut = shortcutObject;

            return new ShortcutInspection(
                path,
                true,
                Convert.ToString(shortcut.TargetPath, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(shortcut.WorkingDirectory, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(shortcut.Arguments, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(shortcut.IconLocation, CultureInfo.InvariantCulture) ?? string.Empty,
                string.Empty);
        }
        catch (Exception ex)
        {
            return new ShortcutInspection(path, true, string.Empty, string.Empty, string.Empty, string.Empty, $"Shortcut could not be resolved: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(shortcutObject);
            ReleaseComObject(shellObject);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private static IReadOnlyList<SearchRoot> BuildSearchRoots(ShortcutInspection shortcut)
    {
        var roots = new List<SearchRoot>();

        AddShortcutRoot(roots, "Shortcut target folder", shortcut.TargetPath, isShortcutDerived: true);
        AddShortcutRoot(roots, "Shortcut working directory", shortcut.WorkingDirectory, isShortcutDerived: true);
        AddRoot(roots, "%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), isShortcutDerived: false);
        AddRoot(roots, "%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), isShortcutDerived: false);
        AddRoot(roots, "C:\\ProgramData", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), isShortcutDerived: false);
        AddRoot(roots, "C:\\Program Files", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), isShortcutDerived: false);
        AddRoot(roots, "C:\\Program Files (x86)", Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty, isShortcutDerived: false);
        AddRoot(roots, "%USERPROFILE%\\Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), isShortcutDerived: false);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AddRoot(roots, "%USERPROFILE%\\AppData\\Local\\Packages", Path.Combine(localAppData, "Packages"), isShortcutDerived: false);

        return roots
            .GroupBy(root => NormalizeKey(root.Path), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static void AddShortcutRoot(List<SearchRoot> roots, string label, string path, bool isShortcutDerived)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            roots.Add(new SearchRoot(label, path, Exists: false, IsShortcutDerived: isShortcutDerived, Note: "Shortcut did not provide this path."));
            return;
        }

        var cleanedPath = path.Trim('"');
        if (File.Exists(cleanedPath))
        {
            AddRoot(roots, label, Path.GetDirectoryName(cleanedPath) ?? cleanedPath, isShortcutDerived);
            return;
        }

        AddRoot(roots, label, cleanedPath, isShortcutDerived);
    }

    private static void AddRoot(List<SearchRoot> roots, string label, string path, bool isShortcutDerived)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            roots.Add(new SearchRoot(label, path, Exists: false, IsShortcutDerived: isShortcutDerived, Note: "Path is empty."));
            return;
        }

        var exists = System.IO.Directory.Exists(path);
        roots.Add(new SearchRoot(label, path, exists, isShortcutDerived, exists ? string.Empty : "Directory does not exist or is not accessible."));
    }

    private static void SearchRoots(
        IReadOnlyList<SearchRoot> roots,
        List<FolderCandidate> folders,
        List<FileCandidate> files,
        List<string> warnings)
    {
        var folderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots.Where(root => root.Exists))
        {
            ScanSearchRoot(root, folders, files, warnings, folderKeys, fileKeys);
        }
    }

    private static void ScanSearchRoot(
        SearchRoot root,
        List<FolderCandidate> folders,
        List<FileCandidate> files,
        List<string> warnings,
        HashSet<string> folderKeys,
        HashSet<string> fileKeys)
    {
        var stack = new Stack<DirectorySearchState>();
        var rootInteresting = root.IsShortcutDerived || ContainsSearchTerm(root.Path);
        stack.Push(new DirectorySearchState(root.Path, rootInteresting));

        var scannedDirectories = 0;
        const int maxDirectoriesPerRoot = 120_000;
        const int maxCandidateFiles = 5_000;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            scannedDirectories++;

            if (scannedDirectories > maxDirectoriesPerRoot)
            {
                warnings.Add($"{root.Label}: directory scan stopped after {maxDirectoriesPerRoot:N0} directories.");
                break;
            }

            DirectoryInfo directoryInfo;
            try
            {
                directoryInfo = new DirectoryInfo(current.Path);
            }
            catch (Exception ex)
            {
                warnings.Add($"{current.Path}: could not inspect directory metadata: {ex.Message}");
                continue;
            }

            var directoryMatches = ContainsSearchTerm(directoryInfo.Name) || ContainsSearchTerm(current.Path);
            if (directoryMatches && folderKeys.Add(NormalizeKey(current.Path)))
            {
                folders.Add(new FolderCandidate(current.Path, root.Label, directoryInfo.LastWriteTime, MatchedTerms(current.Path)));
            }

            FileInfo[] childFiles;
            DirectoryInfo[] childDirectories;
            try
            {
                childFiles = directoryInfo.GetFiles();
                childDirectories = directoryInfo.GetDirectories();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
            {
                warnings.Add($"{current.Path}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            foreach (var file in childFiles)
            {
                if (!DataExtensions.Contains(file.Extension) && !LuceneExtensions.Contains(file.Extension))
                {
                    continue;
                }

                var fileMatches = ContainsSearchTerm(file.Name) || ContainsSearchTerm(file.FullName);
                if (!current.UnderInterestingFolder && !fileMatches)
                {
                    continue;
                }

                if (files.Count >= maxCandidateFiles)
                {
                    warnings.Add($"Candidate data file list capped at {maxCandidateFiles:N0} files.");
                    break;
                }

                if (fileKeys.Add(NormalizeKey(file.FullName)))
                {
                    var reason = LuceneExtensions.Contains(file.Extension)
                        ? "Lucene/infobase index file"
                        : fileMatches
                            ? "file/path matched search term"
                            : "under likely The Table folder";

                    files.Add(new FileCandidate(
                        file.FullName,
                        root.Label,
                        file.Extension,
                        file.Length,
                        file.LastWriteTime,
                        reason));
                }
            }

            foreach (var childDirectory in childDirectories)
            {
                if (childDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                var childInteresting = current.UnderInterestingFolder ||
                                       ContainsSearchTerm(childDirectory.Name) ||
                                       ContainsSearchTerm(childDirectory.FullName);
                stack.Push(new DirectorySearchState(childDirectory.FullName, childInteresting));
            }
        }
    }

    private static IReadOnlyList<LuceneIndexInspection> InspectLuceneIndexes(
        IReadOnlyList<SearchRoot> roots,
        List<string> warnings)
    {
        var indexPaths = FindLuceneIndexDirectories(roots, warnings);
        var inspections = new List<LuceneIndexInspection>();

        foreach (var indexPath in indexPaths.Take(20))
        {
            inspections.Add(InspectLuceneIndex(indexPath, warnings));
        }

        return inspections;
    }

    private static IReadOnlyList<string> FindLuceneIndexDirectories(
        IReadOnlyList<SearchRoot> roots,
        List<string> warnings)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots.Where(root => root.Exists))
        {
            var stack = new Stack<string>();
            stack.Push(root.Path);
            var scannedDirectories = 0;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                scannedDirectories++;

                if (scannedDirectories > 120_000)
                {
                    warnings.Add($"{root.Label}: Lucene index scan stopped after 120,000 directories.");
                    break;
                }

                DirectoryInfo directoryInfo;
                try
                {
                    directoryInfo = new DirectoryInfo(current);
                }
                catch (Exception ex)
                {
                    warnings.Add($"{current}: could not inspect Lucene directory metadata: {ex.Message}");
                    continue;
                }

                FileInfo[] files;
                DirectoryInfo[] childDirectories;
                try
                {
                    files = directoryInfo.GetFiles();
                    childDirectories = directoryInfo.GetDirectories();
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
                {
                    continue;
                }

                var hasSegmentsFile = files.Any(file => file.Name.StartsWith("segments", StringComparison.OrdinalIgnoreCase));
                var hasLucenePayload = files.Any(file => LuceneExtensions.Contains(file.Extension));
                if (hasSegmentsFile && hasLucenePayload)
                {
                    paths.Add(directoryInfo.FullName);
                }

                foreach (var childDirectory in childDirectories)
                {
                    if (!childDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        stack.Push(childDirectory.FullName);
                    }
                }
            }
        }

        return paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static LuceneIndexInspection InspectLuceneIndex(string path, List<string> warnings)
    {
        try
        {
            using var directory = FSDirectory.Open(new DirectoryInfo(path));
            using var reader = DirectoryReader.Open(directory);

            var fieldNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var sampleDocuments = new List<IReadOnlyDictionary<string, string>>();
            var inspectedDocuments = 0;
            var liveDocs = MultiFields.GetLiveDocs(reader);

            for (var docId = 0; docId < reader.MaxDoc && inspectedDocuments < 12; docId++)
            {
                if (liveDocs is not null && !liveDocs.Get(docId))
                {
                    continue;
                }

                var document = reader.Document(docId);
                inspectedDocuments++;

                foreach (var field in document.Fields)
                {
                    fieldNames.Add(field.Name);
                }

                if (sampleDocuments.Count < 3)
                {
                    var sample = CreateLuceneSample(document);
                    if (sample.Count > 0)
                    {
                        sampleDocuments.Add(sample);
                    }
                }
            }

            return new LuceneIndexInspection(
                path,
                IsReadable: true,
                Note: string.Empty,
                reader.NumDocs,
                reader.MaxDoc,
                fieldNames.ToList(),
                sampleDocuments);
        }
        catch (Exception ex)
        {
            warnings.Add($"{path}: Lucene read-only inspection failed: {ex.Message}");
            return new LuceneIndexInspection(path, IsReadable: false, ex.Message, 0, 0, [], []);
        }
    }

    private static IReadOnlyDictionary<string, string> CreateLuceneSample(Document document)
    {
        var sample = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in document.Fields
                     .Where(field => ContainsAny(field.Name, ["sermon", "message", "title", "text", "paragraph", "para", "code", "date", "location", "product", "id", "content"]))
                     .Take(10))
        {
            var stringValue = field.GetStringValue();
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                sample[field.Name] = Truncate(stringValue, 160);
                continue;
            }

        }

        return sample;
    }

    private static IReadOnlyList<DatabaseInspection> InspectDatabases(
        IReadOnlyList<FileCandidate> files,
        List<string> warnings)
    {
        var inspections = new List<DatabaseInspection>();

        foreach (var file in files.Where(file => DatabaseExtensions.Contains(file.Extension)).Take(50))
        {
            inspections.Add(InspectDatabase(file.Path, warnings));
        }

        return inspections;
    }

    private static DatabaseInspection InspectDatabase(string path, List<string> warnings)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared
            };

            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            var tableNames = QueryStrings(
                connection,
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name LIMIT 100;");
            var tables = new List<TableInspection>();

            foreach (var tableName in tableNames.Take(60))
            {
                var columns = ReadTableColumns(connection, tableName);
                var rowCount = TryCountRows(connection, tableName);
                var sampleRows = ShouldSampleTable(tableName, columns)
                    ? ReadSampleRows(connection, tableName, columns)
                    : [];

                tables.Add(new TableInspection(tableName, columns, rowCount, sampleRows));
            }

            return new DatabaseInspection(path, IsSqlite: true, string.Empty, tables);
        }
        catch (Exception ex)
        {
            warnings.Add($"{path}: database read-only inspection failed: {ex.Message}");
            return new DatabaseInspection(path, IsSqlite: false, ex.Message, []);
        }
    }

    private static IReadOnlyList<string> QueryStrings(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 4;

        var values = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static IReadOnlyList<ColumnInspection> ReadTableColumns(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteStringLiteral(tableName)});";
        command.CommandTimeout = 4;

        var columns = new List<ColumnInspection>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(new ColumnInspection(
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2)));
        }

        return columns;
    }

    private static long? TryCountRows(SqliteConnection connection, string tableName)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)};";
            command.CommandTimeout = 4;
            var result = command.ExecuteScalar();
            return result is null || result == DBNull.Value
                ? null
                : Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldSampleTable(string tableName, IReadOnlyList<ColumnInspection> columns)
    {
        var evidence = $"{tableName} {string.Join(' ', columns.Select(column => column.Name))}";
        return ContainsAny(evidence, ["sermon", "message", "title", "text", "paragraph", "para", "code", "date", "location"]);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadSampleRows(
        SqliteConnection connection,
        string tableName,
        IReadOnlyList<ColumnInspection> columns)
    {
        var selectedColumns = columns
            .Where(column => ContainsAny(column.Name, ["sermon", "message", "title", "text", "paragraph", "para", "code", "date", "location", "name", "product", "year", "hastext", "hassubtitle"]))
            .Take(6)
            .ToList();

        if (selectedColumns.Count == 0)
        {
            selectedColumns = columns.Take(6).ToList();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT {string.Join(", ", selectedColumns.Select(column => QuoteIdentifier(column.Name)))} FROM {QuoteIdentifier(tableName)} LIMIT 3;";
            command.CommandTimeout = 4;

            var rows = new List<IReadOnlyDictionary<string, string>>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < selectedColumns.Count; index++)
                {
                    row[selectedColumns[index].Name] = reader.IsDBNull(index)
                        ? string.Empty
                        : Truncate(Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty, 140);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<TextFileInspection> InspectTextFiles(
        IReadOnlyList<FileCandidate> files,
        List<string> warnings)
    {
        var inspections = new List<TextFileInspection>();

        foreach (var file in files.Where(file => TextExtensions.Contains(file.Extension)).Take(200))
        {
            inspections.Add(InspectTextFile(file, warnings));
        }

        return inspections;
    }

    private static TextFileInspection InspectTextFile(FileCandidate file, List<string> warnings)
    {
        const long maxSampleBytes = 131_072;

        try
        {
            if (file.SizeBytes > 25_000_000)
            {
                return TextFileInspection.SkippedFile(file.Path, file.Extension, file.SizeBytes, "File is larger than 25 MB; metadata only.");
            }

            using var stream = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var buffer = new byte[Math.Min(maxSampleBytes, stream.Length)];
            var read = stream.Read(buffer, 0, buffer.Length);
            var sample = DecodeText(buffer.AsSpan(0, read));

            var codeMatches = SermonCodeRegex.Matches(sample)
                .Select(match => match.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            return new TextFileInspection(
                file.Path,
                file.Extension,
                file.SizeBytes,
                read,
                Skipped: false,
                SkipReason: string.Empty,
                HasSermonTitleEvidence: ContainsAny(sample, ["title", "sermonTitle", "messageTitle", "Faith Is The Substance", "The Angel Of God"]),
                HasSermonCodeEvidence: codeMatches.Count > 0,
                HasYearOrDateEvidence: DateYearRegex.IsMatch(sample) || ContainsAny(sample, ["date", "year"]),
                HasLocationEvidence: ContainsAny(sample, ["location", "city", "state", "Jeffersonville", "Phoenix", "Oakland"]),
                HasParagraphNumberEvidence: ParagraphNumberRegex.IsMatch(sample) || ContainsAny(sample, ["paragraph", "paragraphNumber"]),
                HasLikelyParagraphTextEvidence: ContainsAny(sample, ["paragraphText", "messageText", "sermonText", "text"]) && sample.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 80,
                HasAudioPathEvidence: AudioPathRegex.IsMatch(sample),
                ExampleCodes: codeMatches);
        }
        catch (Exception ex)
        {
            warnings.Add($"{file.Path}: text sample failed: {ex.Message}");
            return TextFileInspection.SkippedFile(file.Path, file.Extension, file.SizeBytes, ex.Message);
        }
    }

    private static IReadOnlyList<ArchiveInspection> InspectArchives(
        IReadOnlyList<FileCandidate> files,
        List<string> warnings)
    {
        var archives = new List<ArchiveInspection>();

        foreach (var file in files.Where(file => string.Equals(file.Extension, ".zip", StringComparison.OrdinalIgnoreCase)).Take(25))
        {
            try
            {
                using var archive = ZipFile.OpenRead(file.Path);
                var entries = archive.Entries
                    .Take(25)
                    .Select(entry => new ArchiveEntryInspection(entry.FullName, entry.Length))
                    .ToList();
                archives.Add(new ArchiveInspection(file.Path, entries, string.Empty));
            }
            catch (Exception ex)
            {
                warnings.Add($"{file.Path}: zip metadata inspection failed: {ex.Message}");
                archives.Add(new ArchiveInspection(file.Path, [], ex.Message));
            }
        }

        return archives;
    }

    private static Conclusion BuildConclusion(
        IReadOnlyList<DatabaseInspection> databases,
        IReadOnlyList<TextFileInspection> textFiles,
        IReadOnlyList<FileCandidate> files,
        IReadOnlyList<LuceneIndexInspection> luceneIndexes)
    {
        var readableLuceneWithLikelyFields = luceneIndexes.Any(index =>
            index.IsReadable &&
            index.FieldNames.Any(field => ContainsAny(field, ["sermon", "message", "paragraph", "text", "title", "product", "content"])));
        var sqliteWithSermonTables = databases.Any(database =>
            database.IsSqlite &&
            database.Tables.Any(table =>
                ContainsAny(table.Name, ["sermon", "message", "paragraph", "text"]) ||
                table.Columns.Any(column => ContainsAny(column.Name, ["sermon", "message", "paragraph", "text", "title", "code"]))));
        var dbHasRows = databases.Any(database =>
            database.Tables.Any(table => table.RowCount.GetValueOrDefault() > 0));
        var textHasCodes = textFiles.Any(file => file.HasSermonCodeEvidence);
        var textHasParagraphNumbers = textFiles.Any(file => file.HasParagraphNumberEvidence);
        var textHasLikelyText = textFiles.Any(file => file.HasLikelyParagraphTextEvidence);
        var hasAudioOnly = files.Any(file => string.Equals(file.Extension, ".mp3", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(file.Extension, ".m4a", StringComparison.OrdinalIgnoreCase)) &&
                           !sqliteWithSermonTables &&
                           !textHasLikelyText;

        if (readableLuceneWithLikelyFields || (sqliteWithSermonTables && dbHasRows) || (textHasCodes && textHasParagraphNumbers && textHasLikelyText))
        {
            return new Conclusion(
                "A. Import possible",
                "Local structured sermon data appears to be present. The next step should be a separate read-only mapper for the VGR infobase/Lucene files, with writes only to a disposable MessageFlow test database first.");
        }

        if (luceneIndexes.Count > 0 || sqliteWithSermonTables || textHasCodes || textHasParagraphNumbers || textHasLikelyText)
        {
            return new Conclusion(
                "B. Import maybe possible but needs more investigation",
                "Some sermon-like evidence was found, but the inspection did not prove a complete clean mapping of title/code/date/location/paragraph number/paragraph text.");
        }

        if (hasAudioOnly)
        {
            return new Conclusion(
                "C. Import not possible / not recommended",
                "Only audio/binary candidates were found. No clear local sermon text or paragraph data was detected.");
        }

        return new Conclusion(
            "C. Import not possible / not recommended",
            "No clear local sermon text database or readable paragraph data was detected in normally accessible local files.");
    }

    private static string BuildReport(InspectionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MessageFlow - The Table Local Data Inspection");
        builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("Safety");
        builder.AppendLine("- Inspection only. No import was performed.");
        builder.AppendLine("- The Table files were not modified.");
        builder.AppendLine("- MessageFlow database and content data were not modified.");
        builder.AppendLine("- Large binary/audio files were not read fully.");
        builder.AppendLine("- SQLite files were opened with Mode=ReadOnly.");
        builder.AppendLine();

        builder.AppendLine("Shortcut");
        builder.AppendLine($"- Shortcut path: {result.Shortcut.Path}");
        builder.AppendLine($"- Exists: {YesNo(result.Shortcut.Exists)}");
        builder.AppendLine($"- Target path: {Display(result.Shortcut.TargetPath)}");
        builder.AppendLine($"- Working directory: {Display(result.Shortcut.WorkingDirectory)}");
        builder.AppendLine($"- Arguments: {Display(result.Shortcut.Arguments)}");
        builder.AppendLine($"- Icon path: {Display(result.Shortcut.IconPath)}");
        if (!string.IsNullOrWhiteSpace(result.Shortcut.Note))
        {
            builder.AppendLine($"- Note: {result.Shortcut.Note}");
        }

        builder.AppendLine();
        builder.AppendLine("Search Roots");
        foreach (var root in result.SearchRoots)
        {
            builder.AppendLine($"- {root.Label}: {Display(root.Path)} | exists: {YesNo(root.Exists)}{FormatNote(root.Note)}");
        }

        builder.AppendLine();
        builder.AppendLine("Likely Data Folders");
        if (result.Folders.Count == 0)
        {
            builder.AppendLine("- None found.");
        }
        else
        {
            foreach (var folder in result.Folders.OrderBy(folder => folder.Path).Take(120))
            {
                builder.AppendLine($"- {folder.Path}");
                builder.AppendLine($"  Source root: {folder.SourceRoot}; modified: {folder.LastModified:yyyy-MM-dd HH:mm:ss}; matched: {string.Join(", ", folder.MatchedTerms)}");
            }

            if (result.Folders.Count > 120)
            {
                builder.AppendLine($"- Folder list truncated. Total likely folders: {result.Folders.Count:N0}");
            }
        }

        AppendFileSummary(builder, result.Files);
        AppendLuceneSummary(builder, result.LuceneIndexes);
        AppendDatabaseSummary(builder, result.Databases);
        AppendTextSummary(builder, result.TextFiles);
        AppendArchiveSummary(builder, result.Archives);
        AppendEvidenceSummary(builder, result);

        builder.AppendLine();
        builder.AppendLine("Final Recommendation");
        builder.AppendLine($"- Conclusion: {result.Conclusion.Category}");
        builder.AppendLine($"- Recommendation: {result.Conclusion.Recommendation}");
        builder.AppendLine("- Next safe step: review this report, then create a separate prototype mapper that reads only the identified source files and writes to a disposable MessageFlow test database backup.");

        if (result.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Warnings / Inaccessible Paths");
            foreach (var warning in result.Warnings.Take(250))
            {
                builder.AppendLine($"- {warning}");
            }

            if (result.Warnings.Count > 250)
            {
                builder.AppendLine($"- Warning list truncated. Total warnings: {result.Warnings.Count:N0}");
            }
        }

        return builder.ToString();
    }

    private static void AppendLuceneSummary(StringBuilder builder, IReadOnlyList<LuceneIndexInspection> luceneIndexes)
    {
        builder.AppendLine();
        builder.AppendLine("Lucene / VGR Infobase Indexes");
        if (luceneIndexes.Count == 0)
        {
            builder.AppendLine("- None found.");
            return;
        }

        foreach (var index in luceneIndexes)
        {
            builder.AppendLine($"- Index path: {index.Path}");
            builder.AppendLine($"  Readable with Lucene.NET: {YesNo(index.IsReadable)}");
            if (!index.IsReadable)
            {
                builder.AppendLine($"  Note: {index.Note}");
                continue;
            }

            builder.AppendLine($"  Documents: {index.DocumentCount:N0}; max doc id count: {index.MaxDocumentCount:N0}");
            builder.AppendLine($"  Stored/index field names sampled: {string.Join(", ", index.FieldNames.Take(80))}");

            foreach (var sample in index.SampleDocuments)
            {
                builder.AppendLine("  Sample document fields (truncated):");
                foreach (var pair in sample)
                {
                    builder.AppendLine($"    {pair.Key}: {pair.Value}");
                }
            }
        }
    }

    private static void AppendFileSummary(StringBuilder builder, IReadOnlyList<FileCandidate> files)
    {
        builder.AppendLine();
        builder.AppendLine("Possible Data Files");
        if (files.Count == 0)
        {
            builder.AppendLine("- None found.");
            return;
        }

        builder.AppendLine("- Extension counts:");
        foreach (var group in files.GroupBy(file => file.Extension, StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key))
        {
            builder.AppendLine($"  {group.Key}: {group.Count():N0}");
        }

        builder.AppendLine("- Candidate files (non-audio first, capped):");
        foreach (var file in files
                     .OrderBy(file => IsAudio(file.Extension))
                     .ThenBy(file => file.Path)
                     .Take(220))
        {
            builder.AppendLine($"  {file.Path}");
            builder.AppendLine($"    Extension: {file.Extension}; size: {FormatBytes(file.SizeBytes)}; modified: {file.LastModified:yyyy-MM-dd HH:mm:ss}; reason: {file.Reason}; source root: {file.SourceRoot}");
        }

        if (files.Count > 220)
        {
            builder.AppendLine($"- Candidate file list truncated. Total candidate files: {files.Count:N0}");
        }
    }

    private static void AppendDatabaseSummary(StringBuilder builder, IReadOnlyList<DatabaseInspection> databases)
    {
        builder.AppendLine();
        builder.AppendLine("Possible Database Files");
        if (databases.Count == 0)
        {
            builder.AppendLine("- None found.");
            return;
        }

        foreach (var database in databases)
        {
            builder.AppendLine($"- Database path: {database.Path}");
            builder.AppendLine($"  SQLite readable: {YesNo(database.IsSqlite)}");
            if (!database.IsSqlite)
            {
                builder.AppendLine($"  Note: {database.Note}");
                continue;
            }

            builder.AppendLine($"  Tables inspected: {database.Tables.Count:N0}");
            foreach (var table in database.Tables)
            {
                builder.AppendLine($"  - Table: {table.Name}; rows: {FormatCount(table.RowCount)}");
                builder.AppendLine($"    Columns: {string.Join(", ", table.Columns.Select(column => $"{column.Name} {column.Type}".Trim()))}");

                foreach (var sample in table.SampleRows)
                {
                    builder.AppendLine("    Sample row (truncated):");
                    foreach (var pair in sample)
                    {
                        builder.AppendLine($"      {pair.Key}: {pair.Value}");
                    }
                }
            }
        }
    }

    private static void AppendTextSummary(StringBuilder builder, IReadOnlyList<TextFileInspection> textFiles)
    {
        builder.AppendLine();
        builder.AppendLine("Possible Text/Data Files");
        if (textFiles.Count == 0)
        {
            builder.AppendLine("- None inspected.");
            return;
        }

        foreach (var file in textFiles.Take(120))
        {
            builder.AppendLine($"- {file.Path}");
            builder.AppendLine($"  Extension: {file.Extension}; size: {FormatBytes(file.SizeBytes)}; sampled bytes: {file.SampledBytes:N0}");
            if (file.Skipped)
            {
                builder.AppendLine($"  Skipped content sample: {file.SkipReason}");
                continue;
            }

            builder.AppendLine($"  Sermon title evidence: {YesNo(file.HasSermonTitleEvidence)}");
            builder.AppendLine($"  Sermon code evidence: {YesNo(file.HasSermonCodeEvidence)}{FormatExamples(file.ExampleCodes)}");
            builder.AppendLine($"  Date/year evidence: {YesNo(file.HasYearOrDateEvidence)}");
            builder.AppendLine($"  Location evidence: {YesNo(file.HasLocationEvidence)}");
            builder.AppendLine($"  Paragraph number evidence: {YesNo(file.HasParagraphNumberEvidence)}");
            builder.AppendLine($"  Likely paragraph text evidence: {YesNo(file.HasLikelyParagraphTextEvidence)}");
            builder.AppendLine($"  Audio path evidence: {YesNo(file.HasAudioPathEvidence)}");
        }

        if (textFiles.Count > 120)
        {
            builder.AppendLine($"- Text inspection list truncated. Total inspected text/data files: {textFiles.Count:N0}");
        }
    }

    private static void AppendArchiveSummary(StringBuilder builder, IReadOnlyList<ArchiveInspection> archives)
    {
        builder.AppendLine();
        builder.AppendLine("Zip Archive Metadata");
        if (archives.Count == 0)
        {
            builder.AppendLine("- No .zip files inspected.");
            return;
        }

        foreach (var archive in archives)
        {
            builder.AppendLine($"- {archive.Path}");
            if (!string.IsNullOrWhiteSpace(archive.Note))
            {
                builder.AppendLine($"  Note: {archive.Note}");
                continue;
            }

            foreach (var entry in archive.Entries)
            {
                builder.AppendLine($"  {entry.Name} ({FormatBytes(entry.SizeBytes)})");
            }
        }
    }

    private static void AppendEvidenceSummary(StringBuilder builder, InspectionResult result)
    {
        var readableDatabases = result.Databases.Where(database => database.IsSqlite).ToList();
        var readableLuceneIndexes = result.LuceneIndexes.Where(index => index.IsReadable).ToList();
        var dbTextColumns = readableDatabases
            .SelectMany(database => database.Tables)
            .Where(table => table.Columns.Any(column => ContainsAny(column.Name, ["paragraph", "text", "message", "sermon"])))
            .Select(table => table.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        builder.AppendLine();
        builder.AppendLine("Import Evidence Summary");
        builder.AppendLine($"- Readable Lucene/VGR infobase indexes found: {readableLuceneIndexes.Count:N0}");
        builder.AppendLine($"- Readable SQLite databases found: {readableDatabases.Count:N0}");
        builder.AppendLine($"- Tables with sermon/message/paragraph/text-like columns: {dbTextColumns.Count:N0}{FormatExamples(dbTextColumns.Take(8).ToList())}");
        builder.AppendLine($"- Sermon text found: {YesNo(readableLuceneIndexes.Any(index => index.FieldNames.Any(field => ContainsAny(field, ["text", "content", "paragraph"]))) || dbTextColumns.Count > 0 || result.TextFiles.Any(file => file.HasLikelyParagraphTextEvidence))}");
        builder.AppendLine($"- Paragraph numbers found: {YesNo(readableLuceneIndexes.Any(index => index.FieldNames.Any(field => ContainsAny(field, ["paragraph", "para"]))) || result.TextFiles.Any(file => file.HasParagraphNumberEvidence) || result.Databases.Any(database => database.Tables.Any(table => table.Columns.Any(column => ContainsAny(column.Name, ["paragraph", "paragraphnumber", "para"]))))) }");
        builder.AppendLine($"- Sermon codes like 47-0412 found: {YesNo(readableLuceneIndexes.Any(index => index.SampleDocuments.Any(row => row.Values.Any(value => SermonCodeRegex.IsMatch(value)))) || result.TextFiles.Any(file => file.HasSermonCodeEvidence) || result.Databases.Any(database => database.Tables.Any(table => table.SampleRows.Any(row => row.Values.Any(value => SermonCodeRegex.IsMatch(value))))))}");
        builder.AppendLine($"- Audio files found: {YesNo(result.Files.Any(file => IsAudio(file.Extension)))}");
    }

    private static string DecodeText(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes[3..]);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static bool ContainsSearchTerm(string value)
    {
        return SearchTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> MatchedTerms(string value)
    {
        return SearchTerms
            .Where(term => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string QuoteStringLiteral(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static string NormalizeKey(string path)
    {
        return path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string Display(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
    }

    private static string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatNote(string note)
    {
        return string.IsNullOrWhiteSpace(note) ? string.Empty : $" | note: {note}";
    }

    private static string FormatExamples(IReadOnlyList<string> examples)
    {
        return examples.Count == 0 ? string.Empty : $" | examples: {string.Join(", ", examples)}";
    }

    private static string FormatCount(long? count)
    {
        return count is null ? "unknown" : count.Value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private static bool IsAudio(string extension)
    {
        return string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".m4a", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int maxLength)
    {
        var normalized = string.Join(' ', value.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }

    private sealed record DirectorySearchState(string Path, bool UnderInterestingFolder);

    private sealed record ShortcutInspection(
        string Path,
        bool Exists,
        string TargetPath,
        string WorkingDirectory,
        string Arguments,
        string IconPath,
        string Note);

    private sealed record SearchRoot(
        string Label,
        string Path,
        bool Exists,
        bool IsShortcutDerived,
        string Note);

    private sealed record FolderCandidate(
        string Path,
        string SourceRoot,
        DateTime LastModified,
        IReadOnlyList<string> MatchedTerms);

    private sealed record FileCandidate(
        string Path,
        string SourceRoot,
        string Extension,
        long SizeBytes,
        DateTime LastModified,
        string Reason);

    private sealed record LuceneIndexInspection(
        string Path,
        bool IsReadable,
        string Note,
        int DocumentCount,
        int MaxDocumentCount,
        IReadOnlyList<string> FieldNames,
        IReadOnlyList<IReadOnlyDictionary<string, string>> SampleDocuments);

    private sealed record DatabaseInspection(
        string Path,
        bool IsSqlite,
        string Note,
        IReadOnlyList<TableInspection> Tables);

    private sealed record TableInspection(
        string Name,
        IReadOnlyList<ColumnInspection> Columns,
        long? RowCount,
        IReadOnlyList<IReadOnlyDictionary<string, string>> SampleRows);

    private sealed record ColumnInspection(string Name, string Type);

    private sealed record TextFileInspection(
        string Path,
        string Extension,
        long SizeBytes,
        long SampledBytes,
        bool Skipped,
        string SkipReason,
        bool HasSermonTitleEvidence,
        bool HasSermonCodeEvidence,
        bool HasYearOrDateEvidence,
        bool HasLocationEvidence,
        bool HasParagraphNumberEvidence,
        bool HasLikelyParagraphTextEvidence,
        bool HasAudioPathEvidence,
        IReadOnlyList<string> ExampleCodes)
    {
        public static TextFileInspection SkippedFile(string path, string extension, long sizeBytes, string reason)
        {
            return new TextFileInspection(path, extension, sizeBytes, 0, true, reason, false, false, false, false, false, false, false, []);
        }
    }

    private sealed record ArchiveInspection(
        string Path,
        IReadOnlyList<ArchiveEntryInspection> Entries,
        string Note);

    private sealed record ArchiveEntryInspection(string Name, long SizeBytes);

    private sealed record Conclusion(string Category, string Recommendation);

    private sealed record InspectionResult(
        ShortcutInspection Shortcut,
        IReadOnlyList<SearchRoot> SearchRoots,
        IReadOnlyList<FolderCandidate> Folders,
        IReadOnlyList<FileCandidate> Files,
        IReadOnlyList<LuceneIndexInspection> LuceneIndexes,
        IReadOnlyList<DatabaseInspection> Databases,
        IReadOnlyList<TextFileInspection> TextFiles,
        IReadOnlyList<ArchiveInspection> Archives,
        IReadOnlyList<string> Warnings,
        Conclusion Conclusion);
}
