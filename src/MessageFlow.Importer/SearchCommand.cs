using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Importer;

public static class SearchCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var options = SearchCommandOptions.Parse(args);

        if (options.ShowHelp)
        {
            Console.WriteLine(SearchCommandOptions.HelpText);
            return 0;
        }

        if (!options.IsValid)
        {
            Console.Error.WriteLine(options.ErrorMessage);
            Console.Error.WriteLine();
            Console.Error.WriteLine(SearchCommandOptions.HelpText);
            return 1;
        }

        var services = new ServiceCollection()
            .AddMessageFlowData()
            .AddMessageFlowSearch()
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var search = scope.ServiceProvider.GetRequiredService<ISermonSearchService>();
        var results = options.IsStructured
            ? await search.SearchAsync(options.Query, cancellationToken)
            : await search.SearchAsync(options.SearchText, options.Query.MaxResults, cancellationToken);

        Console.WriteLine($"Results: {results.Count}");
        Console.WriteLine();

        foreach (var result in results)
        {
            Console.WriteLine($"{result.SermonTitle} [{result.SermonCode}] {result.Year}");
            Console.WriteLine($"Paragraph {result.ParagraphNumber}" +
                              (result.PageNumber is null ? string.Empty : $" | Page {result.PageNumber}"));
            Console.WriteLine(result.ParagraphTextPreview);
            Console.WriteLine(result.SourceFilePath);
            Console.WriteLine();
        }

        return 0;
    }
}
