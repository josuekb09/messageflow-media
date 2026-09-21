// Verifies the two literature libraries stay separate, complete and correctly ordered.
//
// Runs the real search service against the real database rather than asserting on SQL, so the
// cross-library isolation checks exercise the same code path the operator uses.
//
//   dotnet run --project tools/VerifyBrotherFrankLibrary

using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

const string BranhamSourceName = "brother_branham";
const string FrankSourceName = "brother_frank";

var databasePath = MessageFlowDatabase.DefaultDatabasePath;
if (!File.Exists(databasePath))
{
    Console.WriteLine($"FAIL Database not found: {databasePath}");
    return 1;
}

var checks = new List<CheckResult>();

var services = new ServiceCollection()
    .AddMessageFlowData()
    .AddMessageFlowSearch()
    .BuildServiceProvider();

await using (var scope = services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

    var branhamId = await GetSourceIdAsync(dbContext, BranhamSourceName);
    var frankId = await GetSourceIdAsync(dbContext, FrankSourceName);

    checks.Add(new("Brother Branham library exists", branhamId is not null, $"id {branhamId?.ToString() ?? "(missing)"}"));
    checks.Add(new("Brother Frank library exists", frankId is not null, $"id {frankId?.ToString() ?? "(missing)"}"));

    if (branhamId is null || frankId is null)
    {
        Report(checks);
        return 1;
    }

    // --- Protected production baselines -------------------------------------------------
    var branhamDocuments = await dbContext.Sermons
        .CountAsync(s => s.ContentSourceId == branhamId && s.Language == "en");
    checks.Add(new("Branham English documents", branhamDocuments == 1_203, $"{branhamDocuments:N0} (expected 1,203)"));

    var branhamParagraphs = await dbContext.SermonParagraphs
        .CountAsync(p => p.Sermon!.ContentSourceId == branhamId && p.Sermon.Language == "en");
    checks.Add(new("Branham English paragraphs", branhamParagraphs == 210_061, $"{branhamParagraphs:N0} (expected 210,061)"));

    var bibleVerses = await dbContext.BibleVerses.CountAsync();
    var bibleBooks = await dbContext.BibleBooks.CountAsync();
    checks.Add(new("Bible verses", bibleVerses == 93_373, $"{bibleVerses:N0} (expected 93,373)"));
    checks.Add(new("Bible books", bibleBooks == 66, $"{bibleBooks} (expected 66)"));

    var englishSongs = await dbContext.Songs.CountAsync(s => s.IsActive && s.Language == "en");
    checks.Add(new("English songs", englishSongs == 357, $"{englishSongs:N0} (expected 357)"));

    // --- Brother Frank library is one whole library -------------------------------------
    var strayFrankSources = await dbContext.ContentSources
        .CountAsync(s => s.Id != frankId && (s.Name.Contains("frank") || s.DisplayName.Contains("Frank")));
    checks.Add(new("Brother Frank content lives in exactly one library", strayFrankSources == 0,
        strayFrankSources == 0 ? "No stray Brother Frank sources." : $"{strayFrankSources} extra source(s)."));

    var frankDocuments = await dbContext.Sermons.CountAsync(s => s.ContentSourceId == frankId);
    var frankParagraphs = await dbContext.SermonParagraphs.CountAsync(p => p.Sermon!.ContentSourceId == frankId);
    checks.Add(new("Brother Frank documents present", frankDocuments >= 85, $"{frankDocuments:N0} document(s), {frankParagraphs:N0} paragraph(s)."));

    var frank2020 = await dbContext.Sermons
        .CountAsync(s => s.ContentSourceId == frankId && (s.SermonCode == "CL-2020-04" || s.SermonCode == "CL-2020-12"));
    checks.Add(new("2020 circular letters imported", frank2020 == 2, $"{frank2020} of 2 present."));

    var duplicateCodes = await dbContext.Sermons
        .Where(s => s.ContentSourceId == frankId)
        .GroupBy(s => s.SermonCode)
        .CountAsync(group => group.Count() > 1);
    checks.Add(new("No duplicate publication codes in Brother Frank", duplicateCodes == 0, $"{duplicateCodes} duplicate code(s)."));

    var untypedDocuments = await dbContext.Sermons.CountAsync(s => s.ContentType == null);
    checks.Add(new("Every document has a content type", untypedDocuments == 0, $"{untypedDocuments} untyped document(s)."));

    var frankTypes = await dbContext.Sermons
        .Where(s => s.ContentSourceId == frankId)
        .GroupBy(s => s.ContentType)
        .Select(group => new { Type = group.Key, Count = group.Count() })
        .ToListAsync();
    checks.Add(new("Brother Frank holds several document types", frankTypes.Count >= 3,
        string.Join(", ", frankTypes.OrderBy(t => t.Type).Select(t => $"{t.Type}={t.Count}"))));

    var nonEnglishFrank = await dbContext.Sermons.CountAsync(s => s.ContentSourceId == frankId && s.Language != "en");
    checks.Add(new("Brother Frank content is English only", nonEnglishFrank == 0,
        nonEnglishFrank == 0 ? "All documents are 'en'." : $"{nonEnglishFrank} non-English document(s)."));

    // --- Search index -------------------------------------------------------------------
    var paragraphTotal = await dbContext.SermonParagraphs.CountAsync();
    var ftsTotal = await CountFtsRowsAsync(databasePath);
    checks.Add(new("Search index matches paragraph count", paragraphTotal == ftsTotal,
        $"paragraphs {paragraphTotal:N0}, index {ftsTotal:N0}"));

    // --- Reading order ------------------------------------------------------------------
    var outOfOrder = new List<string>();
    var frankDocs = await dbContext.Sermons
        .Where(s => s.ContentSourceId == frankId)
        .Select(s => new { s.Id, s.Title })
        .ToListAsync();

    foreach (var document in frankDocs)
    {
        var pages = await dbContext.SermonParagraphs
            .Where(p => p.SermonId == document.Id)
            .OrderBy(p => p.ParagraphNumber)
            .Select(p => p.PageNumber)
            .ToListAsync();

        var inversions = pages
            .Zip(pages.Skip(1), (first, second) => first is not null && second is not null && second < first)
            .Count(isInversion => isInversion);

        if (inversions > 0)
        {
            outOfOrder.Add($"{document.Title} ({inversions})");
        }
    }

    checks.Add(new("Brother Frank documents read in page order", outOfOrder.Count == 0,
        outOfOrder.Count == 0
            ? $"{frankDocs.Count} document(s) strictly ordered."
            : $"Out of order: {string.Join("; ", outOfOrder.Take(5))}"));

    var emptyDocuments = await dbContext.Sermons
        .CountAsync(s => s.ContentSourceId == frankId && !s.Paragraphs.Any());
    checks.Add(new("No empty Brother Frank documents", emptyDocuments == 0, $"{emptyDocuments} empty document(s)."));
}

// --- Publication codes are searchable ---------------------------------------------------
// An operator types a code straight from the document. Both libraries use codes containing
// hyphens and trailing digits, which is exactly the shape a paragraph lookup also has.
await using (var scope = services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
    var search = scope.ServiceProvider.GetRequiredService<ISermonSearchService>();

    var codes = await dbContext.Sermons
        .Where(s => s.SermonCode == "CL-2020-04" || s.SermonCode == "CL-1989-02" || s.SermonCode == "47-0412")
        .Select(s => new { s.SermonCode, s.Id })
        .ToListAsync();

    foreach (var code in codes)
    {
        var results = await search.SearchAsync(code.SermonCode, 50, CancellationToken.None, "en");
        var foundDocument = results.Any(r => r.SermonId == code.Id);
        checks.Add(new($"Search by publication code '{code.SermonCode}'", foundDocument,
            foundDocument
                ? $"{results.Count} result(s), including the document itself."
                : $"{results.Count} result(s), but not document {code.Id}."));
    }

    // "<term> <number>" must still mean "find this term, paragraph <number>".
    var lookup = await search.SearchAsync("faith 12", 50, CancellationToken.None, "en");
    var allParagraph12 = lookup.Count > 0 && lookup.All(r => r.ParagraphNumber == 12);
    checks.Add(new("Paragraph lookup 'faith 12' still works", allParagraph12,
        lookup.Count == 0
            ? "No results."
            : $"{lookup.Count} result(s), all paragraph 12: {allParagraph12}."));
}

// --- Cross-library isolation, through the real search service ---------------------------
// A common word is used on purpose: it is what makes leakage between libraries visible.
foreach (var term in new[] { "Jesus", "God", "Revelation" })
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
    var search = scope.ServiceProvider.GetRequiredService<ISermonSearchService>();

    var branhamId = (await GetSourceIdAsync(dbContext, BranhamSourceName))!.Value;
    var frankId = (await GetSourceIdAsync(dbContext, FrankSourceName))!.Value;
    var visible = new[] { branhamId, frankId };

    var frankResults = await search.SearchAsync(
        new SermonSearchQuery(
            ContentSourceId: frankId,
            SearchText: term,
            MaxResults: 200,
            Language: "en",
            ContentSourceIds: visible));

    var branhamResults = await search.SearchAsync(
        new SermonSearchQuery(
            ContentSourceId: branhamId,
            SearchText: term,
            MaxResults: 200,
            Language: "en",
            ContentSourceIds: visible));

    var frankSermonIds = frankResults.Select(r => r.SermonId).Distinct().ToList();
    var branhamSermonIds = branhamResults.Select(r => r.SermonId).Distinct().ToList();

    var frankLeak = await dbContext.Sermons
        .CountAsync(s => frankSermonIds.Contains(s.Id) && s.ContentSourceId != frankId);
    var branhamLeak = await dbContext.Sermons
        .CountAsync(s => branhamSermonIds.Contains(s.Id) && s.ContentSourceId != branhamId);

    checks.Add(new($"'{term}' scoped to Brother Frank returns results", frankResults.Count > 0,
        $"{frankResults.Count} paragraph(s) across {frankSermonIds.Count} document(s)."));
    checks.Add(new($"'{term}' scoped to Brother Frank excludes Branham", frankLeak == 0,
        frankLeak == 0 ? "No Branham documents." : $"{frankLeak} Branham document(s) leaked."));
    checks.Add(new($"'{term}' scoped to Brother Branham excludes Frank", branhamLeak == 0,
        branhamLeak == 0 ? "No Brother Frank documents." : $"{branhamLeak} Brother Frank document(s) leaked."));
}

Report(checks);
return checks.All(check => check.Passed) ? 0 : 1;

// ----------------------------------------------------------------------------------------

static async Task<int?> GetSourceIdAsync(MessageFlowDbContext dbContext, string name)
{
    return await dbContext.ContentSources
        .Where(source => source.Name == name)
        .Select(source => (int?)source.Id)
        .FirstOrDefaultAsync();
}

static async Task<long> CountFtsRowsAsync(string databasePath)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = """SELECT COUNT(1) FROM "SermonParagraphsFts";""";
    return Convert.ToInt64(await command.ExecuteScalarAsync());
}

static void Report(List<CheckResult> checks)
{
    Console.WriteLine();
    Console.WriteLine("Brother Frank library verification");
    Console.WriteLine(new string('-', 78));
    foreach (var check in checks)
    {
        Console.WriteLine($"{(check.Passed ? "OK  " : "FAIL")} {check.Name}: {check.Detail}");
    }

    var failed = checks.Count(check => !check.Passed);
    Console.WriteLine(new string('-', 78));
    Console.WriteLine(failed == 0
        ? $"All {checks.Count} checks passed."
        : $"{failed} of {checks.Count} checks FAILED.");
}

internal sealed record CheckResult(string Name, bool Passed, string Detail);
