namespace MessageFlow.Importer;

public sealed record ImportOptions(
    string SourceRoot,
    bool Force,
    bool Reset,
    int? ContentSourceId = null,
    IProgress<ImportProgress>? Progress = null,
    bool ShowHelp = false,
    bool IsValid = true,
    string ErrorMessage = "")
{
    private const string DefaultPdfRoot = @"D:\Br William Marrion Branham\PDF";

    public static string HelpText =>
        """
        Usage:
          dotnet run --project src\MessageFlow.Importer -- "D:\Br William Marrion Branham\PDF"
          dotnet run --project src\MessageFlow.Importer -- "D:\Br William Marrion Branham\PDF" --force
          dotnet run --project src\MessageFlow.Importer -- --reset "D:\Br William Marrion Branham\PDF"

        Options:
          --force    Re-import files that already exist in the database.
          --reset    Clear imported sermons and paragraphs, then import the local PDFs again.
          --help     Show this help text.

        MessageFlow imports local PDF files only. It does not scrape websites or download content.
        """;

    public static ImportOptions Parse(string[] args)
    {
        var sourceRoot = DefaultPdfRoot;
        var force = false;
        var reset = false;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                return new ImportOptions(sourceRoot, force, reset, ShowHelp: true);
            }

            if (string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-f", StringComparison.OrdinalIgnoreCase))
            {
                force = true;
                continue;
            }

            if (string.Equals(arg, "--reset", StringComparison.OrdinalIgnoreCase))
            {
                reset = true;
                continue;
            }

            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                return new ImportOptions(
                    sourceRoot,
                    force,
                    reset,
                    IsValid: false,
                    ErrorMessage: $"Unknown option: {arg}");
            }

            sourceRoot = arg;
        }

        sourceRoot = Path.GetFullPath(sourceRoot);

        if (!Directory.Exists(sourceRoot))
        {
            return new ImportOptions(
                sourceRoot,
                force,
                reset,
                IsValid: false,
                ErrorMessage: $"Source folder does not exist: {sourceRoot}");
        }

        return new ImportOptions(sourceRoot, force, reset);
    }
}
