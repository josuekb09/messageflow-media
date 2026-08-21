using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using MessageFlow.App;
using MessageFlow.App.Controls;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var checks = new List<(string Name, Func<Task<string>> Run)>
{
    ("selected-sermon pattern rules", PatternRules),
    ("operator search highlighting", OperatorSearchHighlighting),
    ("database-backed global sermon search", GlobalSearch),
    ("language-scoped search, favorites, and Bible references", LanguageScopedSearchAndFavorites),
    ("database-backed selected-sermon search", SelectedSermonSearch),
    ("Bible chapter context and verse navigation", BibleChapterContext),
    ("selected-sermon full loading by stable id", FullSelectedSermonLoad),
    ("reading-mode isolation, stale guard, and navigation", StaleGuardAndMatchNavigation),
    ("global sermon search response time", GlobalSearchPerformance),
    ("song public body snapshot", SongBodySnapshot),
    ("song title slide fit rules", SongTitleSlideFitRules),
    ("live navigation identity rules", LiveNavigationRules),
    ("projection display window rules", ProjectionDisplayRules),
    ("sermon projection typography and pagination", SermonProjectionTypography)
};

if (args.Contains("--include-import", StringComparer.OrdinalIgnoreCase))
{
    checks.Add(("temporary managed library import", TemporaryManagedLibraryImport));
}

var failures = new List<string>();
foreach (var check in checks)
{
    try
    {
        var detail = await check.Run();
        Console.WriteLine($"PASS {check.Name}: {detail}");
    }
    catch (Exception ex)
    {
        failures.Add($"{check.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {check.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"{failures.Count} church UX hotfix verification check(s) failed.");
    return 1;
}

Console.WriteLine();
Console.WriteLine($"{checks.Count} church UX hotfix verification checks passed.");
return 0;

static Task<string> PatternRules()
{
    var multi = SermonTextSearchPattern.Create("and so we");
    Assert(multi.Match("And, so we may continue.") is not null, "all terms should match");
    Assert(multi.Match("And then the service continued.") is null, "paragraphs containing only 'and' should not qualify");
    Assert(multi.Match("So the service continued.") is null, "missing query terms should not qualify");

    var phrase = Need(multi.Match("And so we may continue."), "complete phrase should match");
    var scattered = Need(multi.Match("And after a pause, so after another pause, we may continue."), "ordered terms should match");
    Assert(phrase.Score < scattered.Score, "complete phrase should rank ahead of scattered terms");

    var exact = SermonTextSearchPattern.Create("\"and so we\"");
    Assert(exact.IsExactPhrase, "quoted query should remain exact");
    Assert(exact.Match("And so we may continue.") is not null, "exact phrase should match");
    Assert(exact.Match("And then so then we may continue.") is null, "exact phrase should reject unrelated prefix-only text");

    const string longPhrase = "this is a complete long phrase with many words that must not be truncated";
    var longExact = SermonTextSearchPattern.Create($"\"{longPhrase}\"");
    Assert(longExact.NormalizedQuery.Contains("MUST NOT BE TRUNCATED", StringComparison.Ordinal), "long phrase tail should be preserved");
    Assert(longExact.Match(longPhrase) is not null, "long phrase should match fully");
    Assert(longExact.Match("this is a complete long phrase") is null, "long phrase should not degrade to prefix-only matching");

    var straight = SermonTextSearchPattern.Create("don't");
    var smart = SermonTextSearchPattern.Create("don\u2019t");
    Assert(straight.Match("We don't know yet.") is not null, "straight apostrophe should match straight text");
    Assert(straight.Match("We don\u2019t know yet.") is not null, "straight query should match smart apostrophe text");
    Assert(smart.Match("We don't know yet.") is not null, "smart query should match straight apostrophe text");

    var smartQuoted = SermonTextSearchPattern.Create("\u201cJesus had said\u201d");
    Assert(smartQuoted.IsExactPhrase, "smart quotes should enter exact mode");
    Assert(smartQuoted.Match("Jesus, had   said.") is not null, "punctuation and whitespace should normalize safely");
    Assert(SermonTextSearchPattern.Create("and    so     we").Match("And so we continue.") is not null, "repeated whitespace should collapse");
    Assert(SermonTextSearchPattern.Create("and, so we!").Match("And so we continue.") is not null, "query punctuation should normalize safely");

    var malformed = SermonTextSearchPattern.Create("\"Jesus had said");
    Assert(!malformed.IsExactPhrase, "unmatched quote should not enter exact mode");
    Assert(malformed.Match("Jesus had said this.") is not null, "unmatched quote should still search safely");
    Assert(SermonTextSearchPattern.Create("\"a nonexistent phrase\"").Match("This paragraph contains other text.") is null, "no-result query should return zero matches");

    return Task.FromResult($"multi-word, exact phrase, long phrase, apostrophes, whitespace, punctuation, malformed quote, and no-result probes passed; phrase score {phrase.Score} < scattered score {scattered.Score}.");
}

static Task<string> OperatorSearchHighlighting()
{
    Exception? failure = null;
    string? detail = null;
    var thread = new Thread(() =>
    {
        try
        {
            var phrase = new HighlightedTextBlock
            {
                SourceText = "Jesus had said this before.",
                HighlightQuery = "Jesus had said"
            };
            Assert(HighlightedText(phrase) == "Jesus had said", "unquoted contiguous phrase should highlight the phrase");

            var terms = new HighlightedTextBlock
            {
                SourceText = "Jesus paused, had mercy, and said it.",
                HighlightQuery = "Jesus had said"
            };
            Assert(HighlightedText(terms) == "Jesus|had|said", "unquoted non-contiguous query should highlight each matched term");

            var exact = new HighlightedTextBlock
            {
                SourceText = "Jesus paused, had mercy, and said it.",
                HighlightQuery = "\"Jesus had said\""
            };
            Assert(HighlightedText(exact).Length == 0, "quoted phrase should not broaden to separated terms");

            var smartApostrophe = new HighlightedTextBlock
            {
                SourceText = "We don\u2019t change punctuation.",
                HighlightQuery = "don't"
            };
            Assert(HighlightedText(smartApostrophe) == "don\u2019t", "highlight should preserve visible smart apostrophe text");

            detail = "phrase, term, exact no-broaden, and smart-apostrophe highlighted runs preserve source text.";
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw new InvalidOperationException(failure.Message, failure);
    }

    return Task.FromResult(detail ?? "operator highlighting verified.");
}

static async Task<string> TemporaryManagedLibraryImport()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"MessageFlowChurchUx-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);
    try
    {
        var intake = Path.Combine(tempRoot, "intake");
        var managed = Path.Combine(tempRoot, "managed");
        Directory.CreateDirectory(intake);
        var songPath = Path.Combine(intake, "Controlled Test Song.txt");
        await File.WriteAllTextAsync(songPath, "CONTROLLED TEST SONG\nExact first line\nRepeated line\nRepeated line\n\nSecond section\nFinal line");

        string sourcePdf;
        await using (var production = CreateDb())
        {
            var paths = await production.Sermons.AsNoTracking().Select(sermon => sermon.SourceFilePath).ToListAsync();
            sourcePdf = paths.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("No local text-based Sermon PDF was available for the temporary import probe.");
        }

        var pdfPath = Path.Combine(intake, "Controlled Brother Frank Sermon.pdf");
        File.Copy(sourcePdf, pdfPath);
        var databasePath = Path.Combine(tempRoot, "custom-test.db");
        var options = new DbContextOptionsBuilder<MessageFlowDbContext>().UseSqlite($"Data Source={databasePath}").Options;

        string sermonTitle;
        await using (var db = new MessageFlowDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var importer = new LocalLibraryImportService(db, managed);
            var sermon = await importer.ScanAsync(pdfPath, LocalLibraryImportService.SermonType);
            Assert(sermon.CanImport && sermon.ItemCount > 0, $"controlled text PDF should scan successfully: {sermon.Status}");
            sermon.Title = "Controlled Brother Frank Import";
            sermon.IsSelected = true;
            sermonTitle = sermon.Title;

            var song = await importer.ScanAsync(songPath, LocalLibraryImportService.SongType);
            Assert(song.CanImport && song.ItemCount == 2, $"structured TXT should scan as two ordered sections: {song.Status}");
            song.Title = "Controlled Test Song";
            song.IsSelected = true;

            var imported = await importer.ImportAsync([sermon, song]);
            Assert(imported == 2, "two controlled items should import transactionally");
            Assert(await db.Sermons.AnyAsync(item =>
                    item.Title == sermonTitle &&
                    item.Author!.FullName == "Ewald Frank" &&
                    item.Author.DisplayName == "Brother Frank" &&
                    item.ContentSource!.DisplayName == "Brother Frank Publications" &&
                    item.ContentSource.SourceType == "Book"),
                "Brother Frank author, source identity, and document type should be stored separately");
            Assert(await db.Songs.AnyAsync(item => item.Title == song.Title && item.SourceFolder == "Custom Song"), "custom Song identity should be stored");
        }

        var movedIntake = Path.Combine(tempRoot, "originals-moved");
        Directory.Move(intake, movedIntake);
        await using (var reopened = new MessageFlowDbContext(options))
        {
            var sermonSearch = new SermonSearchService(reopened);
            var sermonResults = await sermonSearch.SearchAsync(sermonTitle, 20);
            Assert(sermonResults.Any(result => result.SermonTitle == sermonTitle), "managed Sermon should remain searchable after restart and original-folder move");
            var songSearch = new SongSearchService(reopened);
            var songResults = await songSearch.SearchAsync(new SongSearchQuery("Repeated line", 20));
            Assert(songResults.Any(result => result.Title == "Controlled Test Song"), "managed Song should remain lyric-searchable after restart and original-folder move");
            Assert((await reopened.Sermons.SingleAsync(item => item.Title == sermonTitle)).SourceFilePath.StartsWith(managed, StringComparison.OrdinalIgnoreCase), "Sermon should reference managed storage");
        }

        return "Brother Frank PDF and structured Song TXT scanned, imported to an isolated database, reopened, searched, and survived moving the original folder.";
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        var resolved = Path.GetFullPath(tempRoot);
        var tempBase = Path.GetFullPath(Path.GetTempPath());
        if (resolved.StartsWith(tempBase, StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(resolved).StartsWith("MessageFlowChurchUx-", StringComparison.Ordinal))
        {
            Directory.Delete(resolved, recursive: true);
        }
    }
}

static Task<string> SongBodySnapshot()
{
    const string body = "Line one from the imported slide\nLine two from the imported slide";
    var song = new SongResultViewModel(new SongSearchResult(116, 600, "WON'T IT BE WONDERFUL", "Songs", "116.pptx", "songs/116.pptx", string.Empty, "Slide 6", body));
    var section = new SongSectionViewModel(600, 116, 6, "Slide", "Slide 6", body);
    var snapshot = InvokeStatic<ProjectedContentSnapshot>(typeof(MainViewModel), "CreateSongSnapshot", [typeof(SongResultViewModel), typeof(SongSectionViewModel)], song, section);

    Assert(snapshot.ContentType == ProjectionContentType.Song, "song snapshot should be song content");
    Assert(snapshot.BodyText == body, "song snapshot body should be exactly the stored section body");
    Assert(!snapshot.BodyText.Contains(song.Title, StringComparison.Ordinal), "generated song title should not be added to body");
    Assert(!snapshot.BodyText.Contains(section.SectionLabel, StringComparison.Ordinal), "generated section label should not be added to body");
    Assert(!snapshot.BodyText.Contains(song.FileName, StringComparison.Ordinal), "generated filename should not be added to body");
    return Task.FromResult("stored section body is projected exactly; generated title/slide/file metadata stays out of the body.");
}

static Task<string> SongTitleSlideFitRules()
{
    Exception? failure = null;
    string? detail = null;
    var thread = new Thread(() =>
    {
        try
        {
            var provider = CreateProvider();
            var vm = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
            const string song111Body = "111. HOLY, HOLY, HOLY";
            var song111 = new ProjectedContentSnapshot(
                ProjectionContentType.Song,
                "HOLY, HOLY, HOLY",
                "Slide 1",
                song111Body)
            {
                SourceId = 111,
                ItemId = 111001,
                ItemOrder = 1,
                IsTitleSlide = true,
                SourceKey = "HOLY, HOLY, HOLY"
            };

            SetField(vm, "activeProjectionContent", song111);
            var window = new ProjectWindow(vm);
            InvokeVoid(window, "ConfigureContentLayout", Type.EmptyTypes);

            var paragraph = GetField<TextBlock>(window, "ParagraphTextBlock");
            var titleBlock = GetField<TextBlock>(window, "TitleTextBlock");
            var size = new Size(1600, 900);
            var fitted = InvokeInstance<double>(
                window,
                "FindMaximumFittingFontSize",
                [typeof(string), typeof(Size), typeof(double), typeof(double)],
                song111Body,
                size,
                12d,
                360d);

            Assert(InvokeInstance<bool>(window, "IsSongTitleOnlySlide", [typeof(string)], song111Body), "Song 111 first slide should be detected as a title-only source slide");
            Assert(titleBlock.Visibility == Visibility.Collapsed, "song title slide should not add a generated header");
            Assert(paragraph.TextWrapping == TextWrapping.Wrap, "song title slide should allow safe wrapping");
            Assert(paragraph.TextAlignment == TextAlignment.Center, "song title slide should be centered horizontally");
            Assert(fitted > 12, "Song 111 title should have a usable fitted font size");
            Assert(InvokeInstance<bool>(window, "DoesTextFit", [typeof(string), typeof(Size), typeof(double)], song111Body, size, fitted), "Song 111 fitted title should satisfy width and height");

            const string song116Body = "116. WON'T IT BE WONDERFUL";
            var song116 = song111 with
            {
                Title = "WON'T IT BE WONDERFUL",
                BodyText = song116Body,
                SourceId = 116,
                ItemId = 116001,
                SourceKey = "WON'T IT BE WONDERFUL"
            };
            SetField(vm, "activeProjectionContent", song116);
            InvokeVoid(window, "ConfigureContentLayout", Type.EmptyTypes);
            Assert(InvokeInstance<bool>(window, "IsSongTitleOnlySlide", [typeof(string)], song116Body), "Song 116 first slide should be detected as a title-only source slide");
            Assert(paragraph.TextAlignment == TextAlignment.Center, "Song 116 title slide should stay centered");

            const string lyricBody = "Won't it be wonderful there\nHaving no burdens to bear";
            var lyric = song116 with
            {
                Subtitle = "Slide 2",
                BodyText = lyricBody,
                ItemId = 116002,
                ItemOrder = 2,
                IsTitleSlide = false
            };
            SetField(vm, "activeProjectionContent", lyric);
            InvokeVoid(window, "ConfigureContentLayout", Type.EmptyTypes);
            Assert(!InvokeInstance<bool>(window, "IsSongTitleOnlySlide", [typeof(string)], lyricBody), "later Song 116 lyric slide should not be treated as a title slide");
            Assert(titleBlock.Visibility == Visibility.Collapsed, "later song lyric slide should not add metadata header");
            Assert(paragraph.TextAlignment == TextAlignment.Center, "later song lyric slide should be centered within the full stage");
            Assert(paragraph.TextWrapping == TextWrapping.Wrap, "later song lyric slide should still measure wrapped width safely");

            detail = $"Song 111 fitted at {fitted:0.##}px in {size.Width:0}x{size.Height:0}; Song 116 title centered; later lyrics centered in the full stage.";
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw new InvalidOperationException(failure.Message, failure);
    }

    return Task.FromResult(detail ?? "song title slide fit rules verified.");
}

static Task<string> LiveNavigationRules()
{
    var sermon = new ProjectedContentSnapshot(ProjectionContentType.Sermon, "Why Little Bethlehem", "Paragraph 20", "Text") { SourceId = 10, ItemId = 200, ItemOrder = 20, ItemNumber = 20, SourceKey = "58-1228" };
    Assert(ProjectionNavigationRules.ShouldUpdateLiveSermonParagraph(true, sermon, 10, 200), "same live sermon paragraph should update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSermonParagraph(false, sermon, 10, 200), "closed sermon projection should not update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSermonParagraph(true, sermon, 11, 200), "different sermon should stay preview-only");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSermonParagraph(true, sermon, 10, 201), "different paragraph should stay preview-only");

    var song = new ProjectedContentSnapshot(ProjectionContentType.Song, "WON'T IT BE WONDERFUL", "Slide 6", "Lyrics") { SourceId = 116, ItemId = 600, SourceKey = "WON'T IT BE WONDERFUL" };
    Assert(ProjectionNavigationRules.ShouldUpdateLiveSongSection(true, song, 116, "WON'T IT BE WONDERFUL", 600), "same live song section should update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSongSection(false, song, 116, "WON'T IT BE WONDERFUL", 600), "closed song projection should not update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSongSection(true, song, 110, "Another Song", 600), "different song should stay preview-only");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveSongSection(true, song, 116, "WON'T IT BE WONDERFUL", 601), "different song section should stay preview-only");

    var bible = new ProjectedContentSnapshot(ProjectionContentType.Bible, "John 3:16", string.Empty, "Text") { SourceId = 1, ItemId = 316, SourceKey = "KJV" };
    Assert(ProjectionNavigationRules.ShouldUpdateLiveBibleVerse(true, bible, 1, "KJV", 316), "same live Bible verse should update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveBibleVerse(false, bible, 1, "KJV", 316), "closed Bible projection should not update live");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveBibleVerse(true, bible, 1, "KJV", 317), "different Bible verse should stay preview-only");
    Assert(!ProjectionNavigationRules.ShouldUpdateLiveBibleVerse(true, bible, 2, "KJV", 316), "different Bible translation should stay preview-only");

    return Task.FromResult("song, sermon, and Bible identities update only current live content; closed projection and preview divergence remain private.");
}

static async Task<string> GlobalSearch()
{
    var databasePath = RequireDb();
    await using var db = CreateDb();
    ISermonSearchService search = new SermonSearchService(db);

    var wedding = await CheckTitleSearch(db, search, "Wedding Ceremony", "Wedding Ceremony");
    var bethlehem = await CheckTitleSearch(db, search, "Why Little Bethlehem", "Why Little Bethlehem");
    var jesus = await CheckPhraseSearch(db, search, "Jesus had said", "Jesus had said", exact: false, everyResultContainsPhrase: false);
    var quotedJesus = await CheckPhraseSearch(db, search, "\"Jesus had said\"", "Jesus had said", exact: true, everyResultContainsPhrase: true);
    var faith = await CheckTitleSearch(db, search, "Faith Is the Substance", "Faith Is the Substance");
    var longSource = await db.SermonParagraphs.AsNoTracking()
        .Where(paragraph => paragraph.Text.Length > 220)
        .OrderBy(paragraph => paragraph.Id)
        .Select(paragraph => new Row(paragraph.SermonId, paragraph.Id, paragraph.Sermon!.Title, paragraph.Sermon.SermonCode, paragraph.Sermon.Year, paragraph.ParagraphNumber, paragraph.Text))
        .FirstAsync();
    var longPhrase = StoredPhrase(longSource.Text, 14);
    var longUnquoted = await search.SearchAsync(longPhrase, 200);
    var longQuoted = await search.SearchAsync($"\"{longPhrase}\"", 200);
    Assert(longUnquoted.Any(result => result.ParagraphId == longSource.ParagraphId), "global long unquoted sentence should retain and match all terms");
    Assert(longQuoted.Any(result => result.ParagraphId == longSource.ParagraphId), "global long quoted sentence should retain the complete phrase");
    Assert(longQuoted.All(result => ContainsPhrase(result.FullParagraphText, longPhrase)), "quoted global long sentence should not broaden to a prefix");

    var straight = await search.SearchAsync("don't", 100);
    var smart = await search.SearchAsync("don\u2019t", 100);
    Assert(straight.Count > 0, "straight apostrophe query should return at least one result");
    Assert(smart.Count > 0, "smart apostrophe query should return at least one result");
    await CheckIntegrity(db, "don't", straight);
    await CheckIntegrity(db, "don\u2019t", smart);
    Assert(straight.Select(r => r.ParagraphId).SequenceEqual(smart.Select(r => r.ParagraphId)), "straight and smart apostrophe queries should return the same paragraph identities in the same order");

    var none = await search.SearchAsync("phrase-that-does-not-exist-938471", 100);
    Assert(none.Count == 0, "nonexistent phrase should return zero global results");

    var malformed = await search.SearchAsync("\"Jesus had said", 100);
    await CheckIntegrity(db, "malformed quote", malformed);

    return $"DB={databasePath}; Wedding Ceremony={Fmt(wedding)}; Why Little Bethlehem={Fmt(bethlehem)}; Jesus had said={Fmt(jesus)}; quoted Jesus had said={Fmt(quotedJesus)}; Faith Is the Substance={Fmt(faith)}; long sentence tokens=14; apostrophe results={straight.Count}/{smart.Count}; nonexistent={none.Count}; malformedQuote={malformed.Count}.";
}

static async Task<string> LanguageScopedSearchAndFavorites()
{
    Assert(BibleReferenceParser.TryParse("John 3:16", out var john) && john.BookName == "John", "English John 3:16 should parse");
    Assert(BibleReferenceParser.TryParse("Jean 3:16", out var jean) && jean.BookName == "John", "French Jean 3:16 should parse");
    Assert(BibleReferenceParser.TryParse("Yohana 3:16", out var yohana) && yohana.BookName == "John", "Swahili Yohana 3:16 should parse");
    Assert(BibleReferenceParser.TryParse("Genèse 1:1", out var genese) && genese.BookName == "Genesis", "accented French Genèse 1:1 should parse");
    Assert(BibleReferenceParser.TryParse("Mwanzo 1:1", out var mwanzo) && mwanzo.BookName == "Genesis", "Swahili Mwanzo 1:1 should parse");

    await using var db = CreateDb();
    ISermonSearchService search = new SermonSearchService(db);
    var details = new List<string>();

    foreach (var language in new[] { "en", "fr", "sw" })
    {
        var sermonCount = await db.Sermons.AsNoTracking().CountAsync(sermon => sermon.Language == language);
        Assert(sermonCount > 0, $"expected sermons for language {language}");

        var sampleCode = await db.Sermons.AsNoTracking()
            .Where(sermon => sermon.Language == language && sermon.SermonCode != "")
            .OrderBy(sermon => sermon.Id)
            .Select(sermon => sermon.SermonCode)
            .FirstAsync();

        var results = await search.SearchAsync(sampleCode, 50, CancellationToken.None, language);
        Assert(results.Count > 0, $"{language} search for {sampleCode} should return results");

        var sermonIds = results.Select(result => result.SermonId).Distinct().ToArray();
        var languages = await db.Sermons.AsNoTracking()
            .Where(sermon => sermonIds.Contains(sermon.Id))
            .Select(sermon => sermon.Language)
            .Distinct()
            .ToListAsync();
        Assert(languages.Count == 1 && languages[0] == language,
            $"{language} search must not return other languages ({string.Join(",", languages)})");

        var favoriteCount = await db.FavoriteParagraphs.AsNoTracking()
            .CountAsync(favorite => favorite.SermonParagraph != null &&
                                    favorite.SermonParagraph.Sermon != null &&
                                    favorite.SermonParagraph.Sermon.Language == language);
        var historyCount = await db.ProjectionHistories.AsNoTracking()
            .CountAsync(history => history.SermonParagraph != null &&
                                   history.SermonParagraph.Sermon != null &&
                                   history.SermonParagraph.Sermon.Language == language);

        details.Add($"{language}:search={results.Count}/sermons={sermonCount}/fav={favoriteCount}/hist={historyCount}");
    }

    return string.Join("; ", details);
}

static async Task<string> BibleChapterContext()
{
    await using var db = CreateDb();
    var search = new BibleSearchService(db);
    var john4 = await search.SearchAsync(new BibleSearchQuery("John 4:5", null, 200));
    Assert(john4.Count == 54, "John 4:5 should load all 54 verses in John 4");
    Assert(john4.Select(verse => verse.Verse).SequenceEqual(Enumerable.Range(1, 54)), "John 4 should be returned in verse order");
    var selectedIndex = john4.ToList().FindIndex(verse => verse.Verse == 5);
    Assert(selectedIndex == 4 && john4[selectedIndex + 1].Verse == 6 && john4[selectedIndex - 1].Verse == 4, "John 4:5 should have immediate previous/next chapter context");

    var psalm23 = await search.SearchAsync(new BibleSearchQuery("Psalm 23", null, 200));
    Assert(psalm23.Count == 6 && psalm23[0].Verse == 1, "Psalm 23 should load all six verses");

    var firstJohn = await search.SearchAsync(new BibleSearchQuery("1 John 5:3", null, 200));
    Assert(firstJohn.Count > 3 && firstJohn.Any(verse => verse.Verse == 3), "numbered-book reference should load its complete chapter context");

    var keyword = await search.SearchAsync(new BibleSearchQuery("living water", null, 200));
    Assert(keyword.Count > 0 && keyword.Any(verse => verse.Text.Contains("living water", StringComparison.OrdinalIgnoreCase)), "keyword search should remain result mode");

    return "John 4=54 verses with verse 5 selected; Next/Previous=6/5; Psalm 23=6; 1 John 5:3 selected; keyword results preserved.";
}

static async Task<string> GlobalSearchPerformance()
{
    await using var db = CreateDb();
    var search = new SermonSearchService(db);
    string[] queries =
    [
        "Jesus had said",
        "\"Jesus had said\"",
        "and so we",
        "\"and so we\"",
        "Wedding Ceremony",
        "Why Little Bethlehem"
    ];
    var firstRun = new Dictionary<string, long>(StringComparer.Ordinal);
    var warmRun = new Dictionary<string, long>(StringComparer.Ordinal);

    foreach (var query in queries)
    {
        var timer = Stopwatch.StartNew();
        var results = await search.SearchAsync(query, 200);
        timer.Stop();
        Assert(results.Count > 0, $"{query} should return results during performance verification");
        firstRun[query] = timer.ElapsedMilliseconds;
        Assert(
            timer.Elapsed < TimeSpan.FromSeconds(2),
            $"first-run search for {query} exceeded 2 seconds ({timer.Elapsed.TotalMilliseconds:0} ms). {search.LastDiagnostics}");
    }

    foreach (var query in queries)
    {
        var timer = Stopwatch.StartNew();
        var results = await search.SearchAsync(query, 200);
        timer.Stop();
        Assert(results.Count > 0, $"{query} should return warm results");
        warmRun[query] = timer.ElapsedMilliseconds;
        Assert(
            timer.Elapsed < TimeSpan.FromMilliseconds(500),
            $"warm search for {query} exceeded 500 ms ({timer.Elapsed.TotalMilliseconds:0} ms). {search.LastDiagnostics}");
    }

    return string.Join("; ", queries.Select(query => $"{query}={firstRun[query]} ms first/{warmRun[query]} ms warm"));
}

static async Task<string> SelectedSermonSearch()
{
    await using var db = CreateDb();
    var seed = await FindParagraphContaining(db, "AND SO WE");
    var paragraphs = await LoadRows(db, seed.SermonId);
    Assert(paragraphs.Count > 0, "selected sermon should have paragraphs");

    var multi = SermonTextSearchPattern.Create("and so we");
    var multiMatches = Matches(paragraphs, multi);
    Assert(multiMatches.Count > 0, "multi-word query should match stored selected-sermon paragraphs");
    Assert(multiMatches.All(m => ContainsTerms(m.Row.Text, multi.Terms)), "multi-word matches should contain every query term");
    Assert(multiMatches.Any(m => ContainsPhrase(m.Row.Text, "AND SO WE")), "multi-word search should include complete phrase matches");

    var exact = SermonTextSearchPattern.Create("\"and so we\"");
    Assert(exact.IsExactPhrase, "quoted selected-sermon query should be exact");
    var exactMatches = Matches(paragraphs, exact);
    Assert(exactMatches.Count > 0, "exact phrase should match stored selected-sermon paragraphs");
    Assert(exactMatches.All(m => ContainsPhrase(m.Row.Text, "AND SO WE")), "exact selected-sermon results should contain the phrase in order");

    var longSource = paragraphs.FirstOrDefault(p => SermonTextSearchPattern.TokenizeNormalized(SermonTextSearchPattern.NormalizeForSearch(p.Text)).Count >= 14)
        ?? throw new InvalidOperationException("selected sermon has no paragraph long enough for long-phrase probe");
    var longPhrase = StoredPhrase(longSource.Text, 12);
    var longPattern = SermonTextSearchPattern.Create($"\"{longPhrase}\"");
    Assert(longPattern.NormalizedQuery.EndsWith(longPhrase.Split(' ')[^1], StringComparison.Ordinal), "long selected-sermon phrase tail should be preserved");
    var longMatches = Matches(paragraphs, longPattern);
    Assert(longMatches.Any(m => m.Row.ParagraphId == longSource.ParagraphId), "long stored phrase should match its source paragraph");
    Assert(longPattern.Match(string.Join(' ', longPhrase.Split(' ').Take(5))) is null, "long phrase should not degrade to first-token or prefix-only behavior");

    Assert(SameSet(multiMatches, Matches(paragraphs, SermonTextSearchPattern.Create("and    so     we"))), "repeated whitespace should preserve match identities");
    Assert(SameSet(multiMatches, Matches(paragraphs, SermonTextSearchPattern.Create("and, so we!"))), "punctuation should preserve match identities");

    var malformed = SermonTextSearchPattern.Create("\"and so we");
    Assert(!malformed.IsExactPhrase, "unmatched selected-sermon quote should not enter exact mode");
    Assert(Matches(paragraphs, malformed).Count > 0, "unmatched selected-sermon quote should search safely");

    var none = Matches(paragraphs, SermonTextSearchPattern.Create("phrase-that-does-not-exist-938471"));
    Assert(none.Count == 0, "selected-sermon nonexistent query should return zero matches");
    Assert(multiMatches.Concat(exactMatches).Concat(longMatches).All(m => m.Row.SermonId == seed.SermonId), "selected-sermon results should stay scoped to the selected sermon");

    return $"selected={seed.Title} ({seed.Code}); paragraphs={paragraphs.Count}; multi={multiMatches.Count}; exact={exactMatches.Count}; longPhraseParagraph={longSource.Number}; noResult=0.";
}

static async Task<string> FullSelectedSermonLoad()
{
    var provider = CreateProvider();
    var vm = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
    vm.SearchText = "and so we";
    await vm.SearchNowAsync();
    var selected = vm.SelectedSermon ?? throw new InvalidOperationException("global search did not select a Sermon result");
    Assert(!vm.IsSermonReadingMode, "selecting a global result should remain in global search mode");
    Assert(vm.ParagraphResults.Count > 0 && vm.ParagraphResults.All(item => item.SermonId == selected.SermonId), "global result selection should show only matching previews for that result");

    await using var db = CreateDb();
    var expected = await db.SermonParagraphs.AsNoTracking().CountAsync(p => p.SermonId == selected.SermonId);
    await InvokeTaskVoid(vm, "OpenSermonAsync", [typeof(SermonResultViewModel)], selected);
    Assert(vm.IsSermonReadingMode, "Open Sermon should enter explicit selected-sermon reading mode");
    Assert(vm.ParagraphResults.Count == expected, "reading mode should load the full sermon paragraph count");
    Assert(vm.ParagraphResults.All(p => p.SermonId == selected.SermonId), "reading mode should hide unrelated Sermons");

    InvokeVoid(vm, "BackToSermonSearchResults", Type.EmptyTypes);
    Assert(!vm.IsSermonReadingMode, "Back to Search Results should leave reading mode");
    Assert(vm.SearchText == "and so we", "Back to Search Results should preserve the global query");
    Assert(vm.ParagraphResults.Count > 0 && vm.ParagraphResults.Count < expected, "Back should restore the previous global match subset");
    return $"{selected.Title} ({selected.SermonCode}) opened {expected:N0} paragraphs by stable SermonId, hid unrelated Sermons, and restored query/results on Back.";
}

static async Task<SearchResult> CheckTitleSearch(MessageFlowDbContext db, ISermonSearchService search, string query, string expectedTitle)
{
    var results = await search.SearchAsync(query, 200);
    Assert(results.Count > 0, $"{query} should return at least one global result");
    await CheckIntegrity(db, query, results);

    var expected = await db.Sermons.AsNoTracking().Select(s => new SermonInfo(s.Id, s.Title, s.SermonCode, s.Year, 0)).ToListAsync();
    var needle = SermonTextSearchPattern.NormalizeForSearch(expectedTitle);
    var expectedIds = expected.Where(s => SermonTextSearchPattern.NormalizeForSearch(s.Title).Contains(needle, StringComparison.Ordinal)).Select(s => s.SermonId).ToHashSet();
    Assert(expectedIds.Count > 0, $"expected sermon title was not found in database: {expectedTitle}");

    var match = results.FirstOrDefault(r => expectedIds.Contains(r.SermonId));
    Assert(match is not null, $"{query} should include the expected sermon identity");
    return match!;
}

static async Task<SearchResult> CheckPhraseSearch(MessageFlowDbContext db, ISermonSearchService search, string query, string phrase, bool exact, bool everyResultContainsPhrase)
{
    var pattern = SermonTextSearchPattern.Create(query);
    Assert(pattern.IsExactPhrase == exact, $"{query} exact-phrase mode should be {exact}");

    var normalizedPhrase = SermonTextSearchPattern.NormalizeForSearch(phrase);
    var results = await search.SearchAsync(query, 200);
    Assert(results.Count > 0, $"{query} should return at least one global result");
    await CheckIntegrity(db, query, results);

    var phraseMatches = results.Where(r => ContainsPhrase(r.FullParagraphText, normalizedPhrase)).ToList();
    Assert(phraseMatches.Count > 0, $"{query} should include a paragraph containing the complete phrase in order");
    if (everyResultContainsPhrase)
    {
        Assert(phraseMatches.Count == results.Count, $"{query} should not broaden exact phrase results");
    }

    return phraseMatches[0];
}

static async Task CheckIntegrity(MessageFlowDbContext db, string label, IReadOnlyList<SearchResult> results)
{
    var duplicate = results.GroupBy(r => r.ParagraphId).FirstOrDefault(g => g.Count() > 1);
    Assert(duplicate is null, $"{label} returned duplicate paragraph id {duplicate?.Key}");

    var sample = results.Take(Math.Min(25, results.Count)).ToList();
    if (sample.Count == 0)
    {
        return;
    }

    var ids = sample.Select(r => r.ParagraphId).ToArray();
    var rows = await db.SermonParagraphs.AsNoTracking()
        .Where(p => ids.Contains(p.Id))
        .Select(p => new Row(p.SermonId, p.Id, p.Sermon!.Title, p.Sermon.SermonCode, p.Sermon.Year, p.ParagraphNumber, p.Text))
        .ToDictionaryAsync(p => p.ParagraphId);

    foreach (var result in sample)
    {
        Assert(rows.TryGetValue(result.ParagraphId, out var row), $"{label} paragraph id {result.ParagraphId} should exist");
        Assert(row!.SermonId == result.SermonId, $"{label} paragraph {result.ParagraphId} should belong to returned sermon");
        Assert(row.Number == result.ParagraphNumber && row.Number > 0, $"{label} paragraph number should be valid");
        Assert(!string.IsNullOrWhiteSpace(result.SermonTitle), $"{label} title should be populated");
        Assert(!string.IsNullOrWhiteSpace(result.ParagraphTextPreview), $"{label} snippet should be populated");
        Assert(!string.IsNullOrWhiteSpace(result.FullParagraphText), $"{label} full text should be populated");
        Assert(row.Text == result.FullParagraphText, $"{label} full text should match the stored paragraph");
    }
}

static async Task<string> StaleGuardAndMatchNavigation()
{
    await using var db = CreateDb();
    var sermon = await FindSermonWithMatches(db, "JESUS", 2);
    var provider = CreateProvider();
    var vm = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
    var selected = new SermonResultViewModel(sermon.SermonId, sermon.Title, sermon.Code, sermon.Year, sermon.MatchCount);
    var live = new ProjectedContentSnapshot(ProjectionContentType.Sermon, "Existing live sermon", "Paragraph 1", "Existing live paragraph")
    {
        SourceId = 999001,
        ItemId = 999002,
        ItemNumber = 1,
        ItemOrder = 1,
        SourceKey = "existing"
    };

    await InvokeTaskVoid(vm, "OpenSermonAsync", [typeof(SermonResultViewModel)], selected);
    var fullParagraphCount = vm.ParagraphResults.Count;
    Assert(vm.FocusedSermonId == sermon.SermonId, "opening should establish a stable focused Sermon identity");
    Assert(fullParagraphCount > 0 && vm.FocusedSermonParagraphCount == fullParagraphCount, "opening should load the full document exactly once into the focused collection");

    SetField(vm, "sermonWithinSearchText", "Jesus");
    SetField(vm, "sermonWithinSearchRequestVersion", 2);
    SetField(vm, "activeProjectionContent", live);

    await InvokeTaskVoid(vm, "ApplySermonWithinSearchAsync", [typeof(string), typeof(int), typeof(CancellationToken)], "and so we", 1, CancellationToken.None);
    Assert(vm.ParagraphResults.Count == fullParagraphCount, "stale query A should not replace the full focused document");

    await InvokeTaskVoid(vm, "ApplySermonWithinSearchAsync", [typeof(string), typeof(int), typeof(CancellationToken)], "Jesus", 2, CancellationToken.None);
    var matches = GetField<List<ParagraphResultViewModel>>(vm, "sermonWithinMatches");
    Assert(matches.Count >= 2, "current query should produce at least two matches for navigation");
    Assert(vm.ParagraphResults.Count == fullParagraphCount && vm.ParagraphResults.All(p => p.SermonId == sermon.SermonId), "internal matches must not replace or broaden the full focused document");
    Assert(Equals(GetField<ProjectedContentSnapshot?>(vm, "activeProjectionContent"), live), "selecting the first match should not modify active projection content");

    var first = vm.SelectedParagraph?.ParagraphId ?? throw new InvalidOperationException("current query did not select a match");
    await InvokeTaskVoid(vm, "SelectNextSermonWithinMatchAsync", Type.EmptyTypes);
    var second = vm.SelectedParagraph?.ParagraphId ?? throw new InvalidOperationException("next match did not select a match");
    Assert(first != second, "Next Match should advance within the current result set");

    await InvokeTaskVoid(vm, "SelectPreviousSermonWithinMatchAsync", Type.EmptyTypes);
    Assert(vm.SelectedParagraph?.ParagraphId == first, "Previous Match should return to the prior match");

    SetField(vm, "sermonWithinMatchIndex", matches.Count - 1);
    await InvokeTaskVoid(vm, "SelectNextSermonWithinMatchAsync", Type.EmptyTypes);
    Assert(GetField<int>(vm, "sermonWithinMatchIndex") == 0, "Next Match boundary should wrap safely");

    SetField(vm, "sermonWithinMatchIndex", 0);
    await InvokeTaskVoid(vm, "SelectPreviousSermonWithinMatchAsync", Type.EmptyTypes);
    Assert(GetField<int>(vm, "sermonWithinMatchIndex") == matches.Count - 1, "Previous Match boundary should wrap safely");

    var selectedBeforeNoMatch = vm.SelectedParagraph?.ParagraphId;
    SetField(vm, "sermonWithinSearchText", "phrase-that-does-not-exist-938471");
    SetField(vm, "sermonWithinSearchRequestVersion", 3);
    await InvokeTaskVoid(vm, "ApplySermonWithinSearchAsync", [typeof(string), typeof(int), typeof(CancellationToken)], "phrase-that-does-not-exist-938471", 3, CancellationToken.None);
    Assert(vm.FocusedSermonMatchCount == 0, "no-result internal search should publish zero matches");
    Assert(vm.SelectedParagraph?.ParagraphId == selectedBeforeNoMatch, "no-result internal search should preserve the current paragraph");
    Assert(vm.ParagraphResults.Count == fullParagraphCount, "no-result internal search should preserve the full document");

    InvokeVoid(vm, "ClearSermonWithinSearch", Type.EmptyTypes);
    Assert(string.IsNullOrEmpty(vm.SermonWithinSearchText), "clearing selected-sermon search should clear the query text");
    Assert(GetField<List<ParagraphResultViewModel>>(vm, "sermonWithinMatches").Count == 0, "clearing selected-sermon search should clear match state");
    Assert(GetField<int>(vm, "sermonWithinMatchIndex") == -1, "clearing selected-sermon search should reset match index");
    Assert(Equals(GetField<ProjectedContentSnapshot?>(vm, "activeProjectionContent"), live), "match navigation and clearing should not modify active projection content");
    Assert(vm.ParagraphResults.Count == fullParagraphCount && vm.FocusedSermonId == sermon.SermonId, "Clear should preserve focus identity and the full paragraph collection");

    vm.SetSermonWorkspaceActive(false);
    vm.SetBibleMode(true);
    Assert(!vm.IsSermonReadingMode && !vm.IsSermonGlobalSearchMode, "Bible tab should expose no Sermon reading/search state");
    Assert(vm.CanShowSelectionNavigation, "Bible tab should expose Bible navigation");
    Assert(string.IsNullOrEmpty(vm.SermonWithinSearchText) && vm.FocusedSermonMatchCount == 0, "leaving Sermons should clear transient internal-search state");
    Assert(vm.FocusedSermonId == sermon.SermonId && vm.FocusedSermonParagraphCount == fullParagraphCount, "leaving Sermons should retain the focused document for return");

    vm.SetBibleMode(false);
    vm.SetSermonWorkspaceActive(true);
    Assert(vm.IsSermonReadingMode && vm.FocusedSermonId == sermon.SermonId, "returning to Sermons should restore the same focused reading document");

    return $"selected={sermon.Title} ({sermon.Code}); full={fullParagraphCount}; matches={matches.Count}; stale/no-result/clear/tab transitions preserved focus and live identity.";
}

static Task<string> ProjectionDisplayRules()
{
    Exception? failure = null;
    string? detail = null;
    var thread = new Thread(() =>
    {
        try
        {
            var primary = new ProjectionDisplayTarget("primary", "DISPLAY1", "Display 1 - Primary / Operator - 1920x1080", "Display 1 Primary / Operator", true, 1, 1, 0, 0, 1920, 1080, 0, 0, 1920, 1040, 96, 96);
            var windowed = new Window();
            ProjectionDisplayService.ConfigureAdaptiveWindowedProjection(windowed, primary, preserveExistingWindowBounds: false);
            Assert(!ProjectionDisplayService.ShouldUseFullscreenProjection(primary), "single monitor should not use fullscreen live projection");
            Assert(ProjectionDisplayService.ShouldUseWindowedPreview(primary), "single monitor should use windowed preview behavior");
            Assert(windowed.WindowState == WindowState.Normal, "single-monitor window should be normal");
            Assert(windowed.WindowStyle == WindowStyle.SingleBorderWindow, "single-monitor title bar should remain visible");
            Assert(windowed.ResizeMode == ResizeMode.CanResize, "single-monitor window should be resizable");
            Assert(windowed.ShowInTaskbar, "single-monitor window should stay in the taskbar");
            Assert(windowed.ShowActivated, "single-monitor window should activate normally");
            Near(windowed.Width, primary.WorkingAreaWidth * 0.88, "single-monitor width should be 88% of working area");
            Assert(windowed.Height >= primary.WorkingAreaHeight * 0.82 && windowed.Height <= primary.WorkingAreaHeight * 0.88, "single-monitor height should be within expected band");
            Near(windowed.Left, (primary.WorkingAreaWidth - windowed.Width) / 2, "single-monitor window should be centered horizontally");
            Near(windowed.Top, (primary.WorkingAreaHeight - windowed.Height) / 2, "single-monitor window should be centered vertically");
            ProjectionDisplayService.BringWindowedProjectionToFront(windowed);
            Assert(!windowed.Topmost, "single-monitor foreground helper should not leave the window permanently topmost");

            var secondary = new ProjectionDisplayTarget("secondary", "DISPLAY2", "Display 2 - Secondary / Projection - 1920x1080", "Display 2 Secondary / Projection", false, 2, 2, 1920, 0, 1920, 1080, 1920, 0, 1920, 1080, 96, 96);
            var fullscreen = new Window();
            ProjectionDisplayService.PrepareFullscreenWindow(fullscreen, secondary);
            Assert(ProjectionDisplayService.ShouldUseFullscreenProjection(secondary), "secondary display should use fullscreen live projection");
            Assert(fullscreen.WindowState == WindowState.Normal, "fullscreen window should remain Normal before Show");
            Assert(fullscreen.WindowStyle == WindowStyle.None, "fullscreen window should remove title bar");
            Assert(fullscreen.ResizeMode == ResizeMode.NoResize, "fullscreen window should not be resizable");
            Assert(!fullscreen.ShowInTaskbar, "fullscreen window should stay out of taskbar");
            Assert(!fullscreen.ShowActivated, "fullscreen window should not steal activation");
            Near(fullscreen.Left, secondary.Left, "fullscreen left should match selected display");
            Near(fullscreen.Top, secondary.Top, "fullscreen top should match selected display");
            Near(fullscreen.Width, secondary.Width, "fullscreen width should match selected display");
            Near(fullscreen.Height, secondary.Height, "fullscreen height should match selected display");

            detail = $"single={windowed.Width:0}x{windowed.Height:0} centered normal window; fullscreen pre-show state={fullscreen.WindowState}, style={fullscreen.WindowStyle}, activated={fullscreen.ShowActivated}.";
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw new InvalidOperationException(failure.Message, failure);
    }

    return Task.FromResult(detail ?? "projection display rules verified.");
}

static Task<string> SermonProjectionTypography()
{
    Exception? failure = null;
    string? detail = null;
    var thread = new Thread(() =>
    {
        try
        {
            var provider = CreateProvider();
            var vm = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
            var snapshot = new ProjectedContentSnapshot(
                ProjectionContentType.Sermon,
                "Stable Sermon Title",
                "Paragraph 27",
                "A short paragraph for stable projection sizing.")
            {
                SourceId = 42,
                ItemId = 42027,
                ItemNumber = 27,
                ItemOrder = 27,
                SourceKey = "TEST-42"
            };
            SetField(vm, "activeProjectionContent", snapshot);
            Assert(Math.Abs(vm.SermonProjectionFontSize - 60) < 0.1, "Sermon Fit should restore the 60-DIP default");

            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Small");
            Assert(Math.Abs(vm.SermonProjectionFontSize - 48) < 0.1, "Small preset should reduce Sermon typography");
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Large");
            Assert(Math.Abs(vm.SermonProjectionFontSize - 74) < 0.1, "Large preset should increase Sermon typography");
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Extra Large");
            Assert(Math.Abs(vm.SermonProjectionFontSize - 88) < 0.1, "Extra Large preset should increase Sermon typography");
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Medium");

            vm.IncreaseProjectionTextSizeCommand.Execute(null);
            Assert(Math.Abs(vm.SermonProjectionFontSize - 62) < 0.1, "Sermon A+ should add exactly 2 DIPs");
            vm.DecreaseProjectionTextSizeCommand.Execute(null);
            Assert(Math.Abs(vm.SermonProjectionFontSize - 60) < 0.1, "Sermon A- should subtract exactly 2 DIPs");
            vm.ResetProjectionTextSizeCommand.Execute(null);

            var window = new ProjectWindow(vm);
            InvokeVoid(window, "ConfigureContentLayout", Type.EmptyTypes);
            var paragraph = GetField<TextBlock>(window, "ParagraphTextBlock");
            var title = GetField<TextBlock>(window, "TitleTextBlock");
            var number = GetField<TextBlock>(window, "ParagraphNumberTextBlock");
            var available = new Size(1500, 700);

            Assert(Math.Abs(title.FontSize - 38) < 0.1, "Sermon title should remain compact at 38 DIPs");
            Assert(Math.Abs(number.FontSize - 28) < 0.1, "Sermon paragraph label should remain compact at 28 DIPs");
            Assert(Math.Abs(InvokeInstance<double>(window, "CalculateLineHeight", [typeof(double)], 60d) - 68.4) < 0.1, "Sermon body line height should be 1.14");
            Assert(InvokeInstance<bool>(window, "DoesTextFit", [typeof(string), typeof(Size), typeof(double)], snapshot.BodyText, available, 60d), "short Sermon paragraph should fit at the stable default without enlargement");

            var longText = string.Join(' ', Enumerable.Repeat("This sentence verifies safe projection pagination without changing the chosen body size.", 90));
            var pages = InvokeInstance<IReadOnlyList<string>>(window, "SplitIntoProjectionPages", [typeof(string), typeof(Size), typeof(double)], longText, available, 60d);
            Assert(pages.Count > 1, "long Sermon paragraph should paginate");
            Assert(pages.All(page => InvokeInstance<bool>(window, "DoesTextFit", [typeof(string), typeof(Size), typeof(double)], page, available, 60d)), "every Sermon page should fit at the same 60-DIP size");
            Assert(paragraph.LayoutTransform == Transform.Identity && paragraph.RenderTransform == Transform.Identity, "projection should not use scale transforms");

            SetField(vm, "activeProjectionContent", new ProjectedContentSnapshot(ProjectionContentType.Bible, "John 4:5", string.Empty, "Then cometh he to a city of Samaria, which is called Sychar."));
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Small");
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 78d) == 48d, "Bible Small preset should set a safe target size");
            vm.IncreaseProjectionTextSizeCommand.Execute(null);
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 78d) == 50d, "Bible A+ should increase by 2 DIPs when safe");
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Extra Large");
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 78d) == 78d, "Bible effective size should not exceed its safe fit ceiling");
            vm.ResetProjectionTextSizeCommand.Execute(null);
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 78d) == 78d, "Bible Fit should use the safe fit ceiling");

            SetField(vm, "activeProjectionContent", new ProjectedContentSnapshot(ProjectionContentType.Song, "Song", "Verse 1", "A short lyric line"));
            vm.SelectedProjectionFontSize = vm.ProjectionFontSizes.Single(option => option.Label == "Medium");
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 80d) == 62d, "Song Medium preset should set a safe target size");
            vm.DecreaseProjectionTextSizeCommand.Execute(null);
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 80d) == 60d, "Song A- should decrease by 2 DIPs");
            vm.ResetProjectionTextSizeCommand.Execute(null);
            Assert(InvokeInstance<double>(window, "GetSafeSingleScreenFontSize", [typeof(double)], 80d) == 80d, "Song Fit should use the safe fit ceiling");

            var bibleReference = InvokeStatic<double>(typeof(ProjectWindow), "GetBibleReferenceFontSize", [typeof(Size)], new Size(1600, 900));
            Assert(bibleReference >= 38 && bibleReference <= 78, "Bible reference should remain within the compact header band");

            detail = $"default=60 DIP; A-/A+=2 DIP; title=38; label=28; long paragraph={pages.Count} pages at one font size; Bible reference={bibleReference:0.#}.";
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw new InvalidOperationException(failure.Message, failure);
    }

    return Task.FromResult(detail ?? "Sermon projection typography verified.");
}

static async Task<Row> FindParagraphContaining(MessageFlowDbContext db, string normalizedPhrase)
{
    var row = await db.SermonParagraphs.AsNoTracking()
        .Where(p => EF.Functions.Like(p.SearchText, $"%{normalizedPhrase}%"))
        .OrderBy(p => p.SermonId)
        .ThenBy(p => p.ParagraphNumber)
        .Select(p => new Row(p.SermonId, p.Id, p.Sermon!.Title, p.Sermon.SermonCode, p.Sermon.Year, p.ParagraphNumber, p.Text))
        .FirstOrDefaultAsync();
    return row ?? throw new InvalidOperationException($"No stored sermon paragraph contains normalized phrase '{normalizedPhrase}'.");
}

static async Task<SermonInfo> FindSermonWithMatches(MessageFlowDbContext db, string normalizedTerm, int minimumMatches)
{
    var candidate = await db.SermonParagraphs.AsNoTracking()
        .Where(p => EF.Functions.Like(p.SearchText, $"%{normalizedTerm}%"))
        .GroupBy(p => p.SermonId)
        .Select(g => new { SermonId = g.Key, Count = g.Count() })
        .Where(g => g.Count >= minimumMatches)
        .OrderByDescending(g => g.Count)
        .FirstOrDefaultAsync();
    if (candidate is null)
    {
        throw new InvalidOperationException($"No sermon has at least {minimumMatches} matches for '{normalizedTerm}'.");
    }

    return await db.Sermons.AsNoTracking()
        .Where(s => s.Id == candidate.SermonId)
        .Select(s => new SermonInfo(s.Id, s.Title, s.SermonCode, s.Year, candidate.Count))
        .SingleAsync();
}

static async Task<List<Row>> LoadRows(MessageFlowDbContext db, int sermonId)
{
    return await db.SermonParagraphs.AsNoTracking()
        .Where(p => p.SermonId == sermonId)
        .OrderBy(p => p.ParagraphNumber)
        .Select(p => new Row(p.SermonId, p.Id, p.Sermon!.Title, p.Sermon.SermonCode, p.Sermon.Year, p.ParagraphNumber, p.Text))
        .ToListAsync();
}

static List<Hit> Matches(IEnumerable<Row> rows, SermonTextSearchPattern query)
{
    return rows.Select(row => new Hit(row, query.Match(row.Text)))
        .Where(hit => hit.Match is not null)
        .OrderBy(hit => hit.Match!.Value.Score)
        .ThenBy(hit => hit.Row.Number)
        .Select(hit => new Hit(hit.Row, hit.Match!.Value))
        .ToList();
}

static string StoredPhrase(string text, int count)
{
    var tokens = SermonTextSearchPattern.TokenizeNormalized(SermonTextSearchPattern.NormalizeForSearch(text));
    Assert(tokens.Count >= count, "stored paragraph should have enough tokens for the long phrase probe");
    return string.Join(' ', tokens.Skip(Math.Min(4, tokens.Count - count)).Take(count));
}

static bool ContainsTerms(string text, IReadOnlyCollection<string> terms)
{
    var tokens = SermonTextSearchPattern.TokenizeNormalized(SermonTextSearchPattern.NormalizeForSearch(text)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    return terms.All(tokens.Contains);
}

static bool ContainsPhrase(string text, string normalizedPhrase)
{
    return SermonTextSearchPattern.NormalizeForSearch(text).Contains(normalizedPhrase, StringComparison.OrdinalIgnoreCase);
}

static bool SameSet(IEnumerable<Hit> left, IEnumerable<Hit> right)
{
    return left.Select(h => h.Row.ParagraphId).Order().SequenceEqual(right.Select(h => h.Row.ParagraphId).Order());
}

static string Fmt(SearchResult result)
{
    return $"{result.SermonTitle} [{result.SermonCode}] paragraph {result.ParagraphNumber} (SermonId={result.SermonId}, ParagraphId={result.ParagraphId})";
}

static string RequireDb()
{
    var path = MessageFlowDatabase.DefaultDatabasePath;
    if (!File.Exists(path))
    {
        throw new FileNotFoundException(MessageFlowDatabase.CreateMissingDatabaseMessage(path), path);
    }

    return path;
}

static string ReadOnlyConnectionString()
{
    return new SqliteConnectionStringBuilder
    {
        DataSource = RequireDb(),
        Mode = SqliteOpenMode.ReadOnly
    }.ToString();
}

static MessageFlowDbContext CreateDb()
{
    var options = new DbContextOptionsBuilder<MessageFlowDbContext>()
        .UseSqlite(ReadOnlyConnectionString())
        .Options;
    return new MessageFlowDbContext(options);
}

static ServiceProvider CreateProvider()
{
    return new ServiceCollection()
        .AddDbContext<MessageFlowDbContext>(options => options.UseSqlite(ReadOnlyConnectionString()))
        .AddScoped<ISermonSearchService, SermonSearchService>()
        .BuildServiceProvider();
}

static async Task InvokeTaskVoid(object instance, string methodName, Type[] parameterTypes, params object?[] args)
{
    var result = PrivateMethod(instance.GetType(), methodName, parameterTypes).Invoke(instance, args);
    if (result is Task task)
    {
        await task;
        return;
    }

    throw new InvalidOperationException($"Private method {methodName} did not return Task.");
}

static void InvokeVoid(object instance, string methodName, Type[] parameterTypes, params object?[] args)
{
    var result = PrivateMethod(instance.GetType(), methodName, parameterTypes).Invoke(instance, args);
    Assert(result is null, $"Private method {methodName} should not return a value");
}

static T InvokeStatic<T>(Type type, string methodName, Type[] parameterTypes, params object?[] args)
{
    var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic, binder: null, types: parameterTypes, modifiers: null)
        ?? throw new MissingMethodException(type.FullName, methodName);
    var result = method.Invoke(null, args);
    return result is T typed
        ? typed
        : throw new InvalidOperationException($"Private static method {methodName} did not return {typeof(T).Name}.");
}

static T InvokeInstance<T>(object instance, string methodName, Type[] parameterTypes, params object?[] args)
{
    var result = PrivateMethod(instance.GetType(), methodName, parameterTypes).Invoke(instance, args);
    return result is T typed
        ? typed
        : throw new InvalidOperationException($"Private method {methodName} did not return {typeof(T).Name}.");
}

static MethodInfo PrivateMethod(Type type, string methodName, Type[] parameterTypes)
{
    return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, binder: null, types: parameterTypes, modifiers: null)
        ?? throw new MissingMethodException(type.FullName, methodName);
}

static T GetField<T>(object instance, string name)
{
    var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(instance.GetType().FullName, name);
    return field.GetValue(instance) is T value ? value : default!;
}

static void SetField<T>(object instance, string name, T value)
{
    var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(instance.GetType().FullName, name);
    field.SetValue(instance, value);
}

static string HighlightedText(HighlightedTextBlock textBlock)
{
    return string.Join(
        "|",
        textBlock.Inlines
            .OfType<Run>()
            .Where(run => run.Background is not null)
            .Select(run => run.Text));
}

static SermonTextSearchMatch Need(SermonTextSearchMatch? match, string message)
{
    if (match is null)
    {
        throw new InvalidOperationException(message);
    }

    return match.Value;
}

static void Near(double actual, double expected, string message)
{
    Assert(Math.Abs(actual - expected) <= 0.01, $"{message}. Expected {expected:0.##}, actual {actual:0.##}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed record Row(int SermonId, int ParagraphId, string Title, string Code, int Year, int Number, string Text);
sealed record SermonInfo(int SermonId, string Title, string Code, int Year, int MatchCount);
sealed record Hit(Row Row, SermonTextSearchMatch? Match);
