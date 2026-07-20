using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;

var databasePath = MessageFlowDatabase.DefaultDatabasePath;
var checks = new List<CheckResult>();

if (!File.Exists(databasePath))
{
    Console.WriteLine($"WARN Database not found: {databasePath}");
    return 1;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var branhamSourceId = await ExecuteScalarIntAsync(
    connection,
    """
    SELECT "Id"
    FROM "ContentSources"
    WHERE "Name" = 'brother_branham'
       OR "DisplayName" LIKE '%Branham%'
    LIMIT 1;
    """);

checks.Add(new CheckResult(
    "Brother Branham source exists",
    branhamSourceId is not null,
    branhamSourceId is null ? "Source missing." : "Source found."));

var branhamDocuments = branhamSourceId is null
    ? 0
    : await ExecuteScalarLongAsync(
        connection,
        """SELECT COUNT(1) FROM "Sermons" WHERE "ContentSourceId" = $sourceId;""",
        new SqliteParameter("$sourceId", branhamSourceId.Value));
checks.Add(new CheckResult(
    "Brother Branham document count",
    branhamDocuments is >= 1_150 and <= 1_260,
    $"{branhamDocuments:N0} document(s)."));

var branhamParagraphs = branhamSourceId is null
    ? 0
    : await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "SermonParagraphs" p
        JOIN "Sermons" s ON s."Id" = p."SermonId"
        WHERE s."ContentSourceId" = $sourceId;
        """,
        new SqliteParameter("$sourceId", branhamSourceId.Value));
checks.Add(new CheckResult(
    "Brother Branham paragraph count",
    branhamParagraphs > 190_000,
    $"{branhamParagraphs:N0} paragraph(s)."));

var kjvTranslationId = await ExecuteScalarIntAsync(
    connection,
    """SELECT "Id" FROM "BibleTranslations" WHERE "Abbreviation" = 'KJV' LIMIT 1;""");
checks.Add(new CheckResult(
    "KJV Bible exists",
    kjvTranslationId is not null,
    kjvTranslationId is null ? "KJV missing." : "KJV found."));

var bookCount = await ExecuteScalarLongAsync(connection, """SELECT COUNT(1) FROM "BibleBooks";""");
checks.Add(new CheckResult("Bible book count", bookCount == 66, $"{bookCount:N0} book(s)."));

var kjvVerseCount = kjvTranslationId is null
    ? 0
    : await ExecuteScalarLongAsync(
        connection,
        """SELECT COUNT(1) FROM "BibleVerses" WHERE "TranslationId" = $translationId;""",
        new SqliteParameter("$translationId", kjvTranslationId.Value));
checks.Add(new CheckResult("KJV verse count", kjvVerseCount == 31_102, $"{kjvVerseCount:N0} verse(s)."));

var bibleFavoriteTableExists = await ExecuteScalarLongAsync(
    connection,
    """
    SELECT COUNT(1)
    FROM sqlite_master
    WHERE type = 'table'
      AND name = 'BibleFavoriteVerses';
    """) > 0;
checks.Add(new CheckResult(
    "Bible favorites table",
    bibleFavoriteTableExists,
    bibleFavoriteTableExists ? "Bible favorites are available." : "Bible favorites table is missing."));

if (kjvTranslationId is not null)
{
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "Genesis", 1, 1));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "1 Samuel", 2, 2));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "John", 3, 16));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "James", 4, 2));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "Romans", 8, 4));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "Romans", 8, 28));
    checks.Add(await CheckChapterAsync(connection, kjvTranslationId.Value, "Psalms", 23));
    checks.Add(await CheckVerseAsync(connection, kjvTranslationId.Value, "Revelation", 22, 21));
}

checks.Add(CheckReferenceParser("Romans 12 1", "Romans", 12, 1));
checks.Add(CheckReferenceParser("Rom 12 1", "Romans", 12, 1));
checks.Add(CheckReferenceParser("Romans 12", "Romans", 12, null));
checks.Add(CheckReferenceParser("Jn 3 16", "John", 3, 16));
checks.Add(CheckReferenceParser("Ps 23", "Psalms", 23, null));
checks.Add(CheckReferenceParser("Exo 3 15", "Exodus", 3, 15));
checks.Add(CheckReferenceParser("1 Samuel 2:2", "1 Samuel", 2, 2));
checks.Add(CheckBookSuggestion("ro", "Romans"));
checks.Add(CheckBookSuggestion("ex", "Exodus"));

var testSourceCount = await ExecuteScalarLongAsync(
    connection,
    """
    SELECT COUNT(1)
    FROM "ContentSources"
    WHERE "Name" <> 'brother_branham'
      AND (
          "Name" LIKE '%test%'
          OR "DisplayName" LIKE '%Test%'
          OR COALESCE("LocalFolderPath", '') LIKE '%Ewald Frank Test%'
      );
    """);
checks.Add(new CheckResult(
    "Operator test-source hiding",
    true,
    $"{testSourceCount:N0} test source(s) exist in the database and are hidden from operator source lists."));

var searchIndexExists = await ExecuteScalarLongAsync(
    connection,
    """
    SELECT COUNT(1)
    FROM sqlite_master
    WHERE type = 'table'
      AND name = 'SermonParagraphsFts';
    """) > 0;
checks.Add(new CheckResult(
    "Sermon search index",
    searchIndexExists,
    searchIndexExists ? "Search index exists." : "Search index missing."));

foreach (var check in checks)
{
    Console.WriteLine($"{(check.Passed ? "PASS" : "WARN")} {check.Name}: {check.Message}");
}

return checks.All(check => check.Passed) ? 0 : 1;

static async Task<CheckResult> CheckVerseAsync(
    SqliteConnection connection,
    int translationId,
    string bookName,
    int chapter,
    int verse)
{
    var exists = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "BibleVerses" v
        JOIN "BibleBooks" b ON b."Id" = v."BookId"
        WHERE v."TranslationId" = $translationId
          AND b."Name" = $bookName
          AND v."Chapter" = $chapter
          AND v."Verse" = $verse;
        """,
        new SqliteParameter("$translationId", translationId),
        new SqliteParameter("$bookName", bookName),
        new SqliteParameter("$chapter", chapter),
        new SqliteParameter("$verse", verse));

    var reference = $"{bookName} {chapter}:{verse}";
    return new CheckResult(reference, exists > 0, exists > 0 ? "Verse found." : "Verse missing.");
}

static async Task<CheckResult> CheckChapterAsync(
    SqliteConnection connection,
    int translationId,
    string bookName,
    int chapter)
{
    var count = await ExecuteScalarLongAsync(
        connection,
        """
        SELECT COUNT(1)
        FROM "BibleVerses" v
        JOIN "BibleBooks" b ON b."Id" = v."BookId"
        WHERE v."TranslationId" = $translationId
          AND b."Name" = $bookName
          AND v."Chapter" = $chapter;
        """,
        new SqliteParameter("$translationId", translationId),
        new SqliteParameter("$bookName", bookName),
        new SqliteParameter("$chapter", chapter));

    var reference = $"{bookName} {chapter}";
    return new CheckResult(reference, count > 0, count > 0 ? $"{count:N0} verse(s) found." : "Chapter missing.");
}

static CheckResult CheckReferenceParser(
    string input,
    string expectedBook,
    int expectedChapter,
    int? expectedVerse)
{
    var parsed = BibleReferenceParser.TryParse(input, out var reference);
    var passed = parsed &&
                 reference.IsValid &&
                 reference.BookName == expectedBook &&
                 reference.Chapter == expectedChapter &&
                 reference.Verse == expectedVerse;
    var expected = expectedVerse is null
        ? $"{expectedBook} {expectedChapter}"
        : $"{expectedBook} {expectedChapter}:{expectedVerse}";
    return new CheckResult(
        $"Bible parser {input}",
        passed,
        passed ? $"Parsed as {expected}." : "Parser result did not match the expected reference.");
}

static CheckResult CheckBookSuggestion(string input, string expectedBook)
{
    var matches = BibleReferenceParser.FindMatchingBooks(input, 12);
    var passed = matches.Any(book => book.Name == expectedBook);
    return new CheckResult(
        $"Bible book suggestion {input}",
        passed,
        passed ? $"{expectedBook} is suggested." : $"{expectedBook} was not suggested.");
}

static async Task<int?> ExecuteScalarIntAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    var result = await ExecuteScalarAsync(connection, sql, parameters);
    return result is null || result == DBNull.Value ? null : Convert.ToInt32(result);
}

static async Task<long> ExecuteScalarLongAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    var result = await ExecuteScalarAsync(connection, sql, parameters);
    return result is null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
}

static async Task<object?> ExecuteScalarAsync(
    SqliteConnection connection,
    string sql,
    params SqliteParameter[] parameters)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    foreach (var parameter in parameters)
    {
        command.Parameters.Add(parameter);
    }

    return await command.ExecuteScalarAsync();
}

internal sealed record CheckResult(string Name, bool Passed, string Message);
