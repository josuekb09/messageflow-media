using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MessageFlow.App;
using MessageFlow.App.Services;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Proves the projection rules on real library text:
//   * no page overflows its stage, measured by the layout engine AND by the real ProjectWindow
//     rendered at the screen size (in device-independent units, so each DPI is a real case);
//   * no library text is shown below the readable minimum;
//   * the pages hold exactly the original words in order, and song pages never merge lines;
//   * A+ never exceeds the largest fitting size, A- is a 5% step, Auto restores the auto-fit size.
//
// Usage: VerifyLibraryProjectionFit [--database <path>] [--quick]

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("================================================================================");
Console.WriteLine("MESSAGEFLOW PROJECTION AUTO-FIT & MULTI-LANGUAGE BIBLE VERIFICATION");
Console.WriteLine("================================================================================");

var dbPath = GetOption(args, "--database") ?? MessageFlowDatabase.DefaultDatabasePath;
var quick = args.Any(arg => string.Equals(arg, "--quick", StringComparison.OrdinalIgnoreCase));
var only = GetOption(args, "--only");
bool Runs(string group) => only is null || only.Split(',').Contains(group, StringComparer.OrdinalIgnoreCase);
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

// WPF lays out in device-independent units: a 1920x1080 screen at 150% is a 1280x720 stage.
var screenConfigs = new List<(double Width, double Height, double Dpi, string Name)>();
foreach (var (width, height, name) in new[]
         {
             (1280.0, 720.0, "1280x720"),
             (1366.0, 768.0, "1366x768"),
             (1920.0, 1080.0, "1920x1080"),
             (3840.0, 2160.0, "3840x2160 (4K)")
         })
{
    foreach (var dpi in new[] { 1.0, 1.25, 1.5 })
    {
        screenConfigs.Add((width / dpi, height / dpi, dpi, $"{name} @ {dpi * 100:0}%"));
    }
}

// A- six times, A- once, Auto, A+ six times (the operator limits in MainViewModel).
var operatorScales = new[]
{
    1.0 + (MainViewModel.MinProjectionFontScaleStep * MainViewModel.ProjectionFontScaleStepRatio),
    1.0 - MainViewModel.ProjectionFontScaleStepRatio,
    1.0,
    1.0 + (MainViewModel.MaxProjectionFontScaleStep * MainViewModel.ProjectionFontScaleStepRatio)
};

Console.WriteLine($"Configured {screenConfigs.Count} display setups x {operatorScales.Length} operator sizes (A- x6, A-, Auto, A+ x6).");

// ============================================================================
// PART 3: QUERYING WORST CASES & EXTENSIVE DATA FROM DATABASE
// ============================================================================
Console.WriteLine("\n[3/4] Querying Worst-Case Long Texts Across Library...");

var longestSermons = await QueryAsync(
    """
    SELECT s."Title", 'Paragraph ' || p."ParagraphNumber", p."Text"
    FROM "SermonParagraphs" p
    JOIN "Sermons" s ON s."Id" = p."SermonId"
    LEFT JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
    WHERE (s."Language" = 'en' OR s."Language" IS NULL)
      AND COALESCE(cs."DisplayName", '') NOT LIKE '%Frank%'
    ORDER BY LENGTH(p."Text") DESC
    LIMIT 100;
    """);
Console.WriteLine($"Loaded {longestSermons.Count} longest English sermon paragraphs (Max: {longestSermons.Max(item => item.Text.Length):N0} chars).");

var longestVerses = await QueryAsync(
    """
    SELECT b."Name" || ' ' || v."Chapter" || ':' || v."Verse", '', v."Text"
    FROM "BibleVerses" v
    JOIN "BibleBooks" b ON b."Id" = v."BookId"
    WHERE v."TranslationId" = 1
    ORDER BY LENGTH(v."Text") DESC
    LIMIT 100;
    """);
Console.WriteLine($"Loaded {longestVerses.Count} longest English Bible verses (Max: {longestVerses.Max(item => item.Text.Length):N0} chars).");

var englishSongs = await QuerySongsAsync("en");
Console.WriteLine($"Loaded {englishSongs.Count} English song sections (all bundled English songs).");

var otherSongs = (await QuerySongsAsync("sw")).Concat(await QuerySongsAsync("fr")).ToList();
Console.WriteLine($"Loaded {otherSongs.Count} Kiswahili + French song sections (checked at Auto size).");

var frankParagraphs = await QueryAsync(
    """
    SELECT s."Title", 'Paragraph ' || p."ParagraphNumber", p."Text"
    FROM "SermonParagraphs" p
    JOIN "Sermons" s ON s."Id" = p."SermonId"
    JOIN "ContentSources" cs ON cs."Id" = s."ContentSourceId"
    WHERE cs."DisplayName" LIKE '%Frank%'
    ORDER BY s."Id", p."ParagraphNumber";
    """);
Console.WriteLine($"Loaded {frankParagraphs.Count} Brother Frank publication paragraphs (all publications, max {frankParagraphs.Max(item => item.Text.Length):N0} chars).");

var syntheticWorstCases = new List<(string Title, string Subtitle, string Text)>
{
    ("Ultra-long words", "Edge Case", "Supercalifragilisticexpialidocious pneumonoultramicroscopicsilicovolcanoconiosis antidisestablishmentarianism incomprehensibilities and honorificabilitudinitatibus across the congregation without break."),
    ("Dense punctuation & dashes", "Edge Case", "“And—he—said: ‘Behold, I come quickly!’ (Amen; even so, come, Lord Jesus!)—Is that right? ‘Yes!’—they cried; ‘Amen!’—we believe.”"),
    ("Multi-sentence massive paragraph", "Edge Case", string.Join(" ", Enumerable.Repeat("This is a faithful saying, and worthy of all acceptation, that Christ Jesus came into the world to save sinners; of whom I am chief.", 40))),
    ("One sentence without any punctuation", "Edge Case", string.Join(" ", Enumerable.Repeat("and the word of the Lord came again", 120))),
    ("A very long sermon title that keeps going well past what a single projected header line can hold on a small screen", "Paragraph 999", "Short text under a long title."),
    ("Single character", "Edge Case", "A")
};

// ============================================================================
// PART 4: RUNNING FULL AUTO-FIT PROJECTION ENGINE VERIFICATION
// ============================================================================
Console.WriteLine("\n[4/4] Executing Zero-Overflow Auto-Fit Verification Across Matrix...");

var timer = Stopwatch.StartNew();
var layoutCount = 0;
var pagedCount = 0;
var overflowFailures = 0;
var unreadableFailures = 0;
var textFailures = 0;
var operatorFailures = 0;
var windowFailures = 0;
var windowChecks = 0;
var slowestLayoutMs = 0.0;
var slowestLayoutLabel = string.Empty;
var layoutMilliseconds = new List<double>();

Exception? verifierCrash = null;
var staThread = new Thread(() =>
{
  try
  {
    // The real projection window, rendered off-screen at each stage size.
    var provider = new ServiceCollection()
        .AddDbContext<MessageFlowDbContext>(options => options.UseSqlite(connectionString))
        .BuildServiceProvider();
    var viewModel = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
    var window = new ProjectWindow(viewModel)
    {
        Left = -20000,
        Top = -20000,
        ShowActivated = false,
        Topmost = false
    };
    var activeContentProperty = typeof(MainViewModel).GetProperty(nameof(MainViewModel.ActiveProjectionContent))!;
    var updateLayout = typeof(ProjectWindow).GetMethod("UpdateProjectionLayout", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var showNextPage = typeof(ProjectWindow).GetMethod("ShowNextProjectionPage", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var pagesField = typeof(ProjectWindow).GetField("projectionPages", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var root = (Grid)window.FindName("ProjectionRoot");
    var stage = (Grid)window.FindName("ParagraphStage");
    var body = (TextBlock)window.FindName("ParagraphTextBlock");
    var indicator = (TextBlock)window.FindName("PageIndicatorTextBlock");
    window.Show();

    // Each resize posts window messages; without a message loop the queue overflows
    // (Win32 error 1816) after a few thousand checks.
    static void PumpDispatcher()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    void Fail(ref int counter, string message)
    {
        counter++;
        totalFailures++;
        if (counter <= 25)
        {
            Console.WriteLine(message);
        }
    }

    void CheckRealWindow(
        (double Width, double Height, double Dpi, string Name) screen,
        ProjectedContentSnapshot snapshot,
        string label)
    {
        window.Width = screen.Width;
        window.Height = screen.Height;
        // Clearing first makes the window start at page 1, as it does for each new projection.
        activeContentProperty.SetValue(viewModel, null);
        activeContentProperty.SetValue(viewModel, snapshot);
        window.UpdateLayout();
        updateLayout.Invoke(window, null);
        window.UpdateLayout();

        // Walk every continuation page the operator can reach with the arrow keys.
        var pageCount = Math.Max(1, ((List<string>)pagesField.GetValue(window)!).Count);
        for (var page = 0; page < pageCount; page++)
        {
            if (page > 0)
            {
                showNextPage.Invoke(window, null);
                window.UpdateLayout();
            }

            totalTests++;
            windowChecks++;
            var bodyFits = body.DesiredSize.Height <= stage.ActualHeight + 0.5 &&
                           body.DesiredSize.Width <= stage.ActualWidth + 0.5;
            var rowsFit = root.DesiredSize.Height <= window.ActualHeight + 0.5 &&
                          window.ActualHeight <= screen.Height + 0.5 &&
                          window.ActualWidth <= screen.Width + 0.5;
            if (!bodyFits || !rowsFit)
            {
                Fail(ref windowFailures,
                    $"FAIL [{label}] REAL WINDOW OVERFLOW on {screen.Name}, page {page + 1}/{pageCount}: " +
                    $"text {body.DesiredSize.Width:0}x{body.DesiredSize.Height:0} in stage {stage.ActualWidth:0}x{stage.ActualHeight:0}, " +
                    $"root {root.DesiredSize.Height:0} in window {window.ActualWidth:0}x{window.ActualHeight:0} on screen {screen.Width:0}x{screen.Height:0}.");
            }

            if (pageCount > 1 && indicator.Text != $"{page + 1} of {pageCount}")
            {
                Fail(ref windowFailures, $"FAIL [{label}] page indicator shows '{indicator.Text}' on page {page + 1} of {pageCount}.");
            }
        }
    }

    void VerifyItem(
        ProjectionContentType contentType,
        string title,
        string subtitle,
        string text,
        bool isTitleSlide,
        string label,
        bool isLibraryText,
        bool allOperatorSizes)
    {
        var snapshot = new ProjectedContentSnapshot(contentType, title, subtitle, text) { IsTitleSlide = isTitleSlide };
        var fontWeight = contentType is ProjectionContentType.Bible or ProjectionContentType.Song ? FontWeights.Bold : FontWeights.SemiBold;
        var textAlignment = contentType == ProjectionContentType.Song ? TextAlignment.Center : TextAlignment.Left;
        var windowSubtitle = contentType == ProjectionContentType.Sermon ? subtitle : string.Empty;

        foreach (var screen in screenConfigs)
        {
            ProjectionLayoutResult? autoLayout = null;
            foreach (var opScale in allOperatorSizes ? operatorScales : [1.0])
            {
                totalTests++;
                layoutCount++;

                var watch = Stopwatch.StartNew();
                var layout = ProjectionAutoFitEngine.CalculateLayout(
                    contentType,
                    title,
                    windowSubtitle,
                    text,
                    screen.Width,
                    screen.Height,
                    screen.Dpi,
                    screen.Dpi,
                    opScale,
                    isTitleSlide);
                watch.Stop();
                layoutMilliseconds.Add(watch.Elapsed.TotalMilliseconds);
                if (watch.Elapsed.TotalMilliseconds > slowestLayoutMs)
                {
                    slowestLayoutMs = watch.Elapsed.TotalMilliseconds;
                    slowestLayoutLabel = $"{label} on {screen.Name}";
                }

                if (Math.Abs(opScale - 1.0) < 0.001)
                {
                    autoLayout = layout;
                }

                if (layout.Pages.Count > 1)
                {
                    pagedCount++;
                }

                // 1. Exact text: same words, same order, nothing added or dropped.
                if (!SameWords(text, layout.Pages))
                {
                    Fail(ref textFailures, $"FAIL [{label}] TEXT CHANGED on {screen.Name} (scale {opScale:0.00}): pages do not hold exactly the original words.");
                }

                if (contentType == ProjectionContentType.Song && !SongLinesPreserved(text, layout.Pages))
                {
                    Fail(ref textFailures, $"FAIL [{label}] SONG LINES MERGED on {screen.Name}: a projected line joins two source lines.");
                }

                // 2. Every page fits the stage the window gives it.
                for (var p = 0; p < layout.Pages.Count; p++)
                {
                    if (!ProjectionAutoFitEngine.DoesTextFit(layout.Pages[p], layout.StageWidth, layout.StageHeight, layout.FontSize, fontWeight, textAlignment, contentType))
                    {
                        Fail(ref overflowFailures, $"FAIL [{label}] OVERFLOW on {screen.Name} (scale {opScale:0.00}): page {p + 1}/{layout.Pages.Count} does not fit {layout.StageWidth:0}x{layout.StageHeight:0} at {layout.FontSize:0.#}.");
                    }
                }

                // 3. Never below the readable minimum for real library text.
                if (isLibraryText && layout.FontSize < layout.MinimumReadableFontSize - 0.01)
                {
                    Fail(ref unreadableFailures, $"FAIL [{label}] UNREADABLE on {screen.Name} (scale {opScale:0.00}): {layout.FontSize:0.#} < minimum {layout.MinimumReadableFontSize:0.#}.");
                }
            }

            // 4. Operator controls: A+ never grows past the auto-fit size of a single screen.
            if (allOperatorSizes && autoLayout is not null && !autoLayout.IsPaginated)
            {
                totalTests++;
                var larger = ProjectionAutoFitEngine.CalculateLayout(contentType, title, windowSubtitle, text, screen.Width, screen.Height, screen.Dpi, screen.Dpi, operatorScales[^1], isTitleSlide);
                var smaller = ProjectionAutoFitEngine.CalculateLayout(contentType, title, windowSubtitle, text, screen.Width, screen.Height, screen.Dpi, screen.Dpi, operatorScales[1], isTitleSlide);
                var expectedSmaller = Math.Max(autoLayout.MinimumReadableFontSize, autoLayout.FontSize * operatorScales[1]);
                if (larger.FontSize > autoLayout.FontSize + 0.01 || !autoLayout.IsAtLargestFit)
                {
                    Fail(ref operatorFailures, $"FAIL [{label}] A+ grew past the auto-fit size on {screen.Name}: {larger.FontSize:0.#} > {autoLayout.FontSize:0.#}.");
                }

                if (Math.Abs(smaller.FontSize - expectedSmaller) > 0.6)
                {
                    Fail(ref operatorFailures, $"FAIL [{label}] A- is not a 5% step on {screen.Name}: {smaller.FontSize:0.#} vs expected {expectedSmaller:0.#}.");
                }
            }

            if (autoLayout is not null && (!quick || isLibraryText))
            {
                CheckRealWindow(screen, snapshot, label);
                PumpDispatcher();
            }
        }
    }

    if (Runs("sermons"))
    {
        Console.WriteLine("-> Testing Top 100 Longest English Sermon Paragraphs...");
        foreach (var (title, subtitle, text) in longestSermons)
        {
            VerifyItem(ProjectionContentType.Sermon, title, subtitle, text, false, $"Sermon '{title}' {subtitle}", true, true);
        }
    }

    if (Runs("bible"))
    {
        Console.WriteLine("-> Testing Top 100 Longest English Bible Verses...");
        foreach (var (reference, _, text) in longestVerses)
        {
            VerifyItem(ProjectionContentType.Bible, reference, string.Empty, text, false, $"Bible {reference}", true, true);
        }
    }

    if (Runs("songs"))
    {
        Console.WriteLine("-> Testing All English Song Sections...");
        foreach (var (title, section, text, isTitleSlide) in englishSongs)
        {
            VerifyItem(ProjectionContentType.Song, title, section, text, isTitleSlide, $"Song '{title}' - {section}", true, !quick);
        }
    }

    if (Runs("othersongs"))
    {
        Console.WriteLine("-> Testing All Kiswahili and French Song Sections (Auto size)...");
        foreach (var (title, section, text, isTitleSlide) in otherSongs)
        {
            VerifyItem(ProjectionContentType.Song, title, section, text, isTitleSlide, $"Song '{title}' - {section}", true, false);
        }
    }

    if (Runs("frank"))
    {
        Console.WriteLine("-> Testing All Brother Frank Publication Paragraphs...");
        // Every paragraph at Auto on every screen; the 100 longest also at every operator size.
        var longestFrank = frankParagraphs.OrderByDescending(item => item.Text.Length).Take(100).ToHashSet();
        foreach (var item in frankParagraphs)
        {
            var (title, subtitle, text) = item;
            VerifyItem(ProjectionContentType.Sermon, title, subtitle, text, false, $"Brother Frank '{title}' {subtitle}", true, !quick && longestFrank.Contains(item));
        }
    }

    if (Runs("synthetic"))
    {
        Console.WriteLine("-> Testing Synthetic Edge Cases...");
        foreach (var (title, subtitle, text) in syntheticWorstCases)
        {
            // Synthetic words longer than a screen line are allowed to go under the minimum: that is
            // the only way such a word can be shown whole. They must still never overflow.
            var isReadableCase = title != "Ultra-long words";
            VerifyItem(ProjectionContentType.Sermon, title, subtitle, text, false, $"Synthetic '{title}'", isReadableCase, true);
            VerifyItem(ProjectionContentType.Song, title, subtitle, text, false, $"Synthetic song '{title}'", isReadableCase, false);
            VerifyItem(ProjectionContentType.Bible, title, subtitle, text, false, $"Synthetic verse '{title}'", isReadableCase, false);
        }
    }

    window.Close();
  }
  catch (Exception ex)
  {
      verifierCrash = ex;
  }
});

staThread.SetApartmentState(ApartmentState.STA);
staThread.Start();
staThread.Join();

timer.Stop();

layoutMilliseconds.Sort();
var median = layoutMilliseconds.Count == 0 ? 0 : layoutMilliseconds[layoutMilliseconds.Count / 2];
var p99 = layoutMilliseconds.Count == 0 ? 0 : layoutMilliseconds[(int)(layoutMilliseconds.Count * 0.99)];

// ============================================================================
// SUMMARY & VERDICT
// ============================================================================
Console.WriteLine("\n================================================================================");
Console.WriteLine("VERIFICATION RESULTS SUMMARY");
Console.WriteLine("================================================================================");
Console.WriteLine($"Total Layout Configurations Evaluated: {layoutCount:N0}");
Console.WriteLine($"Real ProjectWindow page checks:        {windowChecks:N0}");
Console.WriteLine($"Total Test Assertions:                 {totalTests:N0}");
Console.WriteLine($"Continuation Pages Generated:          {pagedCount:N0}");
Console.WriteLine($"Overflow failures (engine):            {overflowFailures:N0}");
Console.WriteLine($"Overflow failures (real window):       {windowFailures:N0}");
Console.WriteLine($"Unreadable (below minimum) failures:   {unreadableFailures:N0}");
Console.WriteLine($"Text changed / lines merged failures:  {textFailures:N0}");
Console.WriteLine($"A+ / A- / Auto failures:               {operatorFailures:N0}");
Console.WriteLine($"Layout time median / p99 / max (ms):   {median:0.0} / {p99:0.0} / {slowestLayoutMs:0.0} ({slowestLayoutLabel})");
Console.WriteLine($"Execution Time:                        {timer.Elapsed.TotalSeconds:F2} seconds");
Console.WriteLine("================================================================================");

if (verifierCrash is not null)
{
    Console.WriteLine($"FAILURE: the verification itself crashed: {verifierCrash}");
    return 2;
}

if (totalFailures == 0)
{
    Console.WriteLine("SUCCESS: 0 OVERFLOW, 0 UNREADABLE, 0 TEXT CHANGES ACROSS THE LIBRARY AT ALL RESOLUTIONS & DPIS!");
    return 0;
}

Console.WriteLine($"FAILURE: {totalFailures} CHECKS FAILED!");
return 1;

async Task<List<(string Title, string Subtitle, string Text)>> QueryAsync(string sql)
{
    var items = new List<(string, string, string)>();
    await using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        items.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
    }

    return items;
}

async Task<List<(string Title, string Section, string Text, bool IsTitleSlide)>> QuerySongsAsync(string language)
{
    var items = new List<(string, string, string, bool)>();
    await using var cmd = connection.CreateCommand();
    cmd.CommandText =
        """
        SELECT s."Title", COALESCE(sec."SectionLabel", sec."SectionType", 'Section'), sec."Text", sec."SectionOrder"
        FROM "Songs" s
        JOIN "SongSections" sec ON sec."SongId" = s."Id"
        WHERE s."IsActive" = 1 AND s."Language" = $language
        ORDER BY s."Id", sec."SectionOrder";
        """;
    cmd.Parameters.AddWithValue("$language", language);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        // The app treats a section with order <= 1 as the song's title slide.
        items.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3) <= 1));
    }

    return items;
}

static bool SameWords(string original, IReadOnlyList<string> pages)
{
    var expected = Regex.Split(original.Trim(), @"\s+").Where(word => word.Length > 0);
    var actual = pages.SelectMany(page => Regex.Split(page.Trim(), @"\s+")).Where(word => word.Length > 0);
    return expected.SequenceEqual(actual, StringComparer.Ordinal);
}

static bool SongLinesPreserved(string original, IReadOnlyList<string> pages)
{
    var sourceLines = ProjectionAutoFitEngine.NormalizeSongText(original).Split('\n');
    return pages
        .SelectMany(page => page.Split('\n'))
        .Select(line => line.Trim())
        .Where(line => line.Length > 0)
        .All(line => sourceLines.Any(source => source.Contains(line, StringComparison.Ordinal)));
}

static string? GetOption(string[] values, string name)
{
    var index = Array.FindIndex(values, value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
}
