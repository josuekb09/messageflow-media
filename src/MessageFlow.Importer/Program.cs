using MessageFlow.Data;
using MessageFlow.Importer;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

if (args.Length > 0 && string.Equals(args[0], "search", StringComparison.OrdinalIgnoreCase))
{
    return await SearchCommand.RunAsync(args.Skip(1).ToArray());
}

var options = ImportOptions.Parse(args);

if (options.ShowHelp)
{
    Console.WriteLine(ImportOptions.HelpText);
    return 0;
}

if (!options.IsValid)
{
    Console.Error.WriteLine(options.ErrorMessage);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ImportOptions.HelpText);
    return 1;
}

var services = new ServiceCollection()
    .AddMessageFlowData()
    .AddMessageFlowSearch()
    .BuildServiceProvider();

await using var scope = services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

MessageFlowDatabase.EnsureDatabaseDirectory(MessageFlowDatabase.DefaultDatabasePath);
await dbContext.Database.MigrateAsync();

Console.WriteLine("MessageFlow PDF Importer");
Console.WriteLine($"Source folder: {options.SourceRoot}");
Console.WriteLine($"Database: {MessageFlowDatabase.DefaultDatabasePath}");
Console.WriteLine($"Force re-import: {(options.Force ? "yes" : "no")}");
Console.WriteLine($"Reset before import: {(options.Reset ? "yes" : "no")}");
Console.WriteLine("Mode: local PDFs only; no website scraping or downloads.");
Console.WriteLine();

var importer = new PdfSermonImporter(dbContext);
var summary = await importer.ImportAsync(options);

Console.WriteLine();
Console.WriteLine("Import complete");
Console.WriteLine($"Total PDF files: {summary.TotalFiles}");
Console.WriteLine($"Imported files: {summary.ImportedFiles}");
Console.WriteLine($"Skipped files: {summary.SkippedFiles}");
Console.WriteLine($"Imported paragraphs: {summary.ImportedParagraphs}");
Console.WriteLine($"Errors: {summary.ErrorCount}");

return summary.ErrorCount == 0 ? 0 : 2;
