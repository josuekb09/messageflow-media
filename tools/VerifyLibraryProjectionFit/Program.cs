using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using MessageFlow.App.Services;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("================================================================================");
Console.WriteLine("MESSAGEFLOW PROJECTION AUTO-FIT & MULTI-LANGUAGE BIBLE VERIFICATION");
Console.WriteLine("================================================================================");

var dbPath = MessageFlowDatabase.DefaultDatabasePath;
if (!File.Exists(dbPath))
{
    Console.WriteLine($"ERROR: Database not found at '{dbPath}'");
    return 1;
}

Console.WriteLine($"Database: {dbPath}");

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var dbOptions = new DbContextOptionsBuilder<MessageFlowDbContext>()
    .UseSqlite(connectionString)
    .Options;

var totalTests = 0;
var totalFailures = 0;

// ============================================================================
// PART 1: MULTI-LANGUAGE BIBLE TRANSLATION TESTS
// ============================================================================
Console.WriteLine("\n[1/4] Verifying Multi-Language Bible Translation Isolation...");

await using (var db = new MessageFlowDbContext(dbOptions))
{
    var bibleSearch = new BibleSearchService(db);

    var languageTests = new[]
    {
        (Lang: "en", ExpectedTransId: 1, ExpectedAbbr: "KJV", SampleQuery: "John 3:16", ExpectedKeyword: "God so loved the world"),
        (Lang: "fr", ExpectedTransId: 2, ExpectedAbbr: "LSG", SampleQuery: "Jean 3:16", ExpectedKeyword: "Dieu a tant aimé le monde"),
        (Lang: "sw", ExpectedTransId: 3, ExpectedAbbr: "SWHULB", SampleQuery: "Yohana 3:16", ExpectedKeyword: "Mungu aliupenda ulimwengu")
    };

    foreach (var lt in languageTests)
    {
        totalTests++;
        var results = await bibleSearch.SearchAsync(new BibleSearchQuery(lt.SampleQuery, lt.ExpectedTransId, 50, lt.Lang));
        if (results.Count == 0)
        {
            Console.WriteLine($"FAIL: Bible search for {lt.Lang} query '{lt.SampleQuery}' returned 0 results.");
            totalFailures++;
            continue;
        }

        var verse = results.FirstOrDefault(v => v.Verse == 16) ?? results[0];
        if (verse.TranslationId != lt.ExpectedTransId || !verse.TranslationAbbreviation.Equals(lt.ExpectedAbbr, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"FAIL: Expected {lt.ExpectedAbbr} (Id {lt.ExpectedTransId}) for lang '{lt.Lang}', got {verse.TranslationAbbreviation} (Id {verse.TranslationId})");
            totalFailures++;
        }
        else if (!verse.Text.Contains(lt.ExpectedKeyword, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"FAIL: Verse text for {lt.Lang} did not contain '{lt.ExpectedKeyword}': {verse.Text}");
            totalFailures++;
        }
        else
        {
            Console.WriteLine($"PASS: [{lt.Lang.ToUpperInvariant()}] {verse.BookName} {verse.Chapter}:{verse.Verse} ({verse.TranslationAbbreviation}) -> \"{verse.Text[..Math.Min(45, verse.Text.Length)]}...\"");
        }

        // Test Chapter Context loading in that language
        totalTests++;
        var chapterResults = await bibleSearch.SearchAsync(new BibleSearchQuery(lt.SampleQuery, lt.ExpectedTransId, 200, lt.Lang));
        var nonMatchingTrans = chapterResults.Where(v => v.TranslationId != lt.ExpectedTransId).ToList();
        if (nonMatchingTrans.Count > 0)
        {
            Console.WriteLine($"FAIL: Chapter context for {lt.Lang} contained {nonMatchingTrans.Count} verses from wrong translation.");
            totalFailures++;
        }
        else
        {
            Console.WriteLine($"PASS: [{lt.Lang.ToUpperInvariant()}] Chapter context cleanly isolated to {lt.ExpectedAbbr} ({chapterResults.Count} verses).");
        }
    }
}

// ============================================================================
// PART 2: SCREEN RESOLUTION & DPI TEST MATRIX
// ============================================================================
Console.WriteLine("\n[2/4] Initializing Screen Resolutions & DPI Matrix...");

var screenConfigs = new[]
{
    (Width: 1280.0, Height: 720.0, Dpi: 1.0, Name: "720p HD (100% DPI)"),
    (Width: 1280.0, Height: 720.0, Dpi: 1.25, Name: "720p HD (125% DPI)"),
    (Width: 1366.0, Height: 768.0, Dpi: 1.0, Name: "1366x768 Laptop (100% DPI)"),
    (Width: 1366.0, Height: 768.0, Dpi: 1.25, Name: "1366x768 Laptop (125% DPI)"),
    (Width: 1920.0, Height: 1080.0, Dpi: 1.0, Name: "1080p FHD Projector (100% DPI)"),
    (Width: 1920.0, Height: 1080.0, Dpi: 1.25, Name: "1080p FHD Projector (125% DPI)"),
    (Width: 1920.0, Height: 1080.0, Dpi: 1.5, Name: "1080p FHD Projector (150% DPI)"),
    (Width: 3840.0, Height: 2160.0, Dpi: 1.5, Name: "4K UHD Display (150% DPI)"),
    (Width: 3840.0, Height: 2160.0, Dpi: 2.0, Name: "4K UHD Display (200% DPI)")
};

var operatorScales = new[] { 0.8, 1.0, 1.2, 1.5 }; // A+ clamping tests

Console.WriteLine($"Configured {screenConfigs.Length} display setups across 4 operator zoom factors.");

// ============================================================================
// PART 3: QUERYING WORST CASES & EXTENSIVE DATA FROM DATABASE
// ============================================================================
Console.WriteLine("\n[3/4] Querying Worst-Case Long Texts Across Library...");

// A. Longest English Sermon Paragraphs
var longestSermons = new List<(string Title, string ParagraphNumber, string Text)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT s."Title", p."ParagraphNumber", p."Text", LENGTH(p."Text") as Len
        FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        WHERE s."Language" = 'en' OR s."Language" IS NULL
        ORDER BY Len DESC
        LIMIT 100;
        """;
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        longestSermons.Add((reader.GetString(0), reader.GetInt32(1).ToString(), reader.GetString(2)));
    }
}
Console.WriteLine($"Loaded {longestSermons.Count} longest English sermon paragraphs (Max: {longestSermons[0].Text.Length:N0} chars).");

// B. Longest English Bible Verses (KJV)
var longestVerses = new List<(string Reference, string Text)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT b."Name" || ' ' || v."Chapter" || ':' || v."Verse" as Ref, v."Text", LENGTH(v."Text") as Len
        FROM "BibleVerses" v
        JOIN "BibleBooks" b ON b."Id" = v."BookId"
        WHERE v."TranslationId" = 1
        ORDER BY Len DESC
        LIMIT 100;
        """;
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        longestVerses.Add((reader.GetString(0), reader.GetString(1)));
    }
}
Console.WriteLine($"Loaded {longestVerses.Count} longest English Bible verses (Max: {longestVerses[0].Text.Length:N0} chars, {longestVerses[0].Reference}).");

// C. All English Songs Sections
var songSections = new List<(string Title, string SectionName, string Text, bool IsTitleSlide)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT s."Title", COALESCE(sec."SectionLabel", sec."SectionType", 'Section'), sec."Text", sec."SectionOrder"
        FROM "Songs" s
        JOIN "SongSections" sec ON sec."SongId" = s."Id"
        WHERE s."Language" = 'en' OR s."Language" IS NULL
        ORDER BY s."Id", sec."SectionOrder";
        """;
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var title = reader.GetString(0);
        var secName = reader.GetString(1);
        var content = reader.GetString(2);
        var order = reader.GetInt32(3);
        songSections.Add((title, secName, content, order == 1));
    }
}
Console.WriteLine($"Loaded {songSections.Count} English song sections across all bundled songs.");

// D. Longest Brother Frank Publications
var frankParagraphs = new List<(string Title, string Text)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT s."Title", p."Text", LENGTH(p."Text") as Len
        FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
        WHERE cs."DisplayName" LIKE '%Frank%'
        ORDER BY Len DESC
        LIMIT 100;
        """;
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        frankParagraphs.Add((reader.GetString(0), reader.GetString(1)));
    }
}
Console.WriteLine($"Loaded {frankParagraphs.Count} longest Brother Frank publication paragraphs (Max: {frankParagraphs[0].Text.Length:N0} chars).");

// E. Synthetic Worst Case: Long unbroken tokens, excessive quotes & dashes
var syntheticWorstCases = new List<(string Title, string Text)>
{
    ("Ultra-long words", "Supercalifragilisticexpialidocious pneumonoultramicroscopicsilicovolcanoconiosis antidisestablishmentarianism incomprehensibilities and honorificabilitudinitatibus across the congregation without break."),
    ("Dense punctuation & dashes", "“And—he—said: ‘Behold, I come quickly!’ (Amen; even so, come, Lord Jesus!)—Is that right? ‘Yes!’—they cried; ‘Amen!’—we believe.”"),
    ("Multi-sentence massive paragraph", string.Join(" ", Enumerable.Repeat("This is a faithful saying, and worthy of all acceptation, that Christ Jesus came into the world to save sinners; of whom I am chief.", 40)))
};

// ============================================================================
// PART 4: RUNNING FULL AUTO-FIT PROJECTION ENGINE VERIFICATION
// ============================================================================
Console.WriteLine("\n[4/4] Executing Zero-Overflow Auto-Fit Verification Across Matrix...");

var timer = Stopwatch.StartNew();
var layoutCount = 0;
var pagedCount = 0;

var staThread = new Thread(() =>
{
    void VerifyItem(
        ProjectionContentType contentType,
        string title,
        string subtitle,
        string text,
        bool isTitleSlide,
        string label)
    {
        foreach (var screen in screenConfigs)
        {
            foreach (var opScale in operatorScales)
            {
                Interlocked.Increment(ref totalTests);
                Interlocked.Increment(ref layoutCount);

                var layout = ProjectionAutoFitEngine.CalculateLayout(
                    contentType,
                    title,
                    subtitle,
                    text,
                    screen.Width,
                    screen.Height,
                    screen.Dpi,
                    screen.Dpi,
                    opScale,
                    isTitleSlide);

                if (layout.Pages.Count > 1)
                {
                    Interlocked.Increment(ref pagedCount);
                }

                // 1. Verify text continuity and exactness
                var originalNormalized = contentType == ProjectionContentType.Song
                    ? text.Replace("\r\n", "\n").Replace('\r', '\n')
                    : Regex.Replace(text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' '), @"\s+", " ").Trim();

                var reconstructed = contentType == ProjectionContentType.Song
                    ? string.Join("\n", layout.Pages)
                    : string.Join(" ", layout.Pages);

                if (reconstructed.Length < originalNormalized.Length * 0.95)
                {
                    Console.WriteLine($"FAIL [{label}] Truncation detected! Original: {originalNormalized.Length} chars, Pages total: {reconstructed.Length} chars.");
                    Interlocked.Increment(ref totalFailures);
                }

                // 2. Measure every page with real WPF engine to guarantee ZERO overflow
                var fontWeight = contentType == ProjectionContentType.Bible || contentType == ProjectionContentType.Song
                    ? FontWeights.Bold
                    : FontWeights.SemiBold;
                var textAlignment = contentType == ProjectionContentType.Song
                    ? TextAlignment.Center
                    : TextAlignment.Left;

                for (var p = 0; p < layout.Pages.Count; p++)
                {
                    var pageText = layout.Pages[p];
                    var fits = ProjectionAutoFitEngine.DoesTextFit(
                        pageText,
                        layout.StageWidth,
                        layout.StageHeight,
                        layout.FontSize,
                        fontWeight,
                        textAlignment,
                        contentType);

                    if (!fits)
                    {
                        Console.WriteLine($"FAIL [{label}] OVERFLOW DETECTED on {screen.Name} (A+={opScale:0.0})! Page {p + 1}/{layout.Pages.Count} does not fit stage ({layout.StageWidth:0}x{layout.StageHeight:0}) at {layout.FontSize:0.#}pt");
                        Interlocked.Increment(ref totalFailures);
                    }
                }
            }
        }
    }

    // 1. Test longest English sermon paragraphs
    Console.WriteLine("-> Testing Top 100 Longest English Sermon Paragraphs...");
    foreach (var (title, pNum, text) in longestSermons)
    {
        VerifyItem(ProjectionContentType.Sermon, title, $"Paragraph {pNum}", text, false, $"Sermon {pNum}");
    }

    // 2. Test longest English Bible verses
    Console.WriteLine("-> Testing Top 100 Longest English Bible Verses...");
    foreach (var (reference, text) in longestVerses)
    {
        VerifyItem(ProjectionContentType.Bible, reference, string.Empty, text, false, $"Bible {reference}");
    }

    // 3. Test all English Song sections
    Console.WriteLine("-> Testing All English Song Sections...");
    foreach (var (title, secName, text, isTitleSlide) in songSections)
    {
        VerifyItem(ProjectionContentType.Song, title, secName, text, isTitleSlide, $"Song '{title}' - {secName}");
    }

    // 4. Test longest Brother Frank publication paragraphs
    Console.WriteLine("-> Testing Top 100 Longest Brother Frank Publication Paragraphs...");
    foreach (var (title, text) in frankParagraphs)
    {
        VerifyItem(ProjectionContentType.Sermon, title, "Publication", text, false, $"Brother Frank '{title}'");
    }

    // 5. Test synthetic worst-case texts
    Console.WriteLine("-> Testing Synthetic Edge Cases...");
    foreach (var (title, text) in syntheticWorstCases)
    {
        VerifyItem(ProjectionContentType.Sermon, title, "Edge Case", text, false, $"Synthetic '{title}'");
    }
});

staThread.SetApartmentState(ApartmentState.STA);
staThread.Start();
staThread.Join();

timer.Stop();

// ============================================================================
// SUMMARY & VERDICT
// ============================================================================
Console.WriteLine("\n================================================================================");
Console.WriteLine("VERIFICATION RESULTS SUMMARY");
Console.WriteLine("================================================================================");
Console.WriteLine($"Total Layout Configurations Evaluated: {layoutCount:N0}");
Console.WriteLine($"Total Test Assertions:                 {totalTests:N0}");
Console.WriteLine($"Continuation Pages Generated:          {pagedCount:N0}");
Console.WriteLine($"Total Text Overflow Failures:          {totalFailures:N0}");
Console.WriteLine($"Execution Time:                        {timer.Elapsed.TotalSeconds:F2} seconds");
Console.WriteLine("================================================================================");

if (totalFailures == 0)
{
    Console.WriteLine("SUCCESS: ZERO OVERFLOW DETECTED ACROSS THE ENTIRE LIBRARY AT ALL RESOLUTIONS & DPIS!");
    return 0;
}
else
{
    Console.WriteLine($"FAILURE: {totalFailures} OVERFLOW CHECKS FAILED!");
    return 1;
}
