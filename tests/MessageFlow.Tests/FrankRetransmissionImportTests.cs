using System.IO;
using Microsoft.Data.Sqlite;

namespace MessageFlow.Tests;

/// <summary>
/// Data checks for the Brother Frank broadcast transcriptions imported by
/// tools\ImportFrankRetransmissions. They read the development database read-only and return
/// early when it is not on this machine, so a clean checkout still passes.
/// </summary>
public sealed class FrankRetransmissionImportTests
{
    // Imported documents carry the code pattern EF-<date>-<PLACE>-RETR|REUN[-BR]-<LANG><nnn>.
    private const string ImportedFilter =
        "(s.SermonCode LIKE 'EF-%-RETR-%' OR s.SermonCode LIKE 'EF-%-REUN-%' OR s.SermonCode LIKE 'EF-%-RETR-BR-%' OR s.SermonCode LIKE 'EF-%-REUN-BR-%')";

    private static string? FindDatabase()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "database", "messageflow.db");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static SqliteConnection? Open()
    {
        var path = FindDatabase();
        if (path is null)
        {
            return null;
        }

        var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        connection.Open();
        using var probe = connection.CreateCommand();
        probe.CommandText = $"SELECT COUNT(1) FROM Sermons s WHERE {ImportedFilter}";
        if (Convert.ToInt32(probe.ExecuteScalar()) == 0)
        {
            connection.Dispose();
            return null; // The transcriptions are not imported into this database.
        }

        return connection;
    }

    private static long Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    [Fact]
    public void CountsPerLanguageAndSource()
    {
        using var connection = Open();
        if (connection is null) return;

        long Count(string language, string source) => Scalar(connection,
            $"SELECT COUNT(1) FROM Sermons s JOIN ContentSources c ON c.Id = s.ContentSourceId WHERE {ImportedFilter} AND s.Language = '{language}' AND c.Name = '{source}'");

        Assert.Equal(63, Count("en", "brother_frank"));
        Assert.Equal(3, Count("en", "brother_branham"));
        Assert.Equal(700, Count("fr", "brother_frank"));
        Assert.Equal(32, Count("fr", "brother_branham"));
        Assert.Equal(80608, Scalar(connection,
            $"SELECT COUNT(1) FROM SermonParagraphs p JOIN Sermons s ON s.Id = p.SermonId WHERE {ImportedFilter}"));
    }

    [Fact]
    public void NoDuplicateDocuments()
    {
        using var connection = Open();
        if (connection is null) return;

        Assert.Equal(0, Scalar(connection,
            $"SELECT COUNT(1) FROM (SELECT 1 FROM Sermons s WHERE {ImportedFilter} GROUP BY s.Language, s.Date, s.Title HAVING COUNT(1) > 1)"));
        Assert.Equal(0, Scalar(connection,
            $"SELECT COUNT(1) FROM (SELECT 1 FROM Sermons s WHERE {ImportedFilter} GROUP BY s.SermonCode HAVING COUNT(1) > 1)"));
    }

    [Fact]
    public void NoEmptyParagraphReplacementCharacterOrUntitledDocument()
    {
        using var connection = Open();
        if (connection is null) return;

        Assert.Equal(0, Scalar(connection,
            $"SELECT COUNT(1) FROM SermonParagraphs p JOIN Sermons s ON s.Id = p.SermonId WHERE {ImportedFilter} AND TRIM(p.Text) = ''"));
        Assert.Equal(0, Scalar(connection,
            $"SELECT COUNT(1) FROM SermonParagraphs p JOIN Sermons s ON s.Id = p.SermonId WHERE {ImportedFilter} AND INSTR(p.Text, char(65533)) > 0"));
        Assert.Equal(0, Scalar(connection,
            $"SELECT COUNT(1) FROM Sermons s WHERE {ImportedFilter} AND (TRIM(s.Title) = '' OR s.Title LIKE '%Sans titre%')"));
    }

    [Fact]
    public void ParagraphNumbersAreContinuousFromOne()
    {
        using var connection = Open();
        if (connection is null) return;

        Assert.Equal(0, Scalar(connection, $"""
            SELECT COUNT(1) FROM (
                SELECT p.SermonId FROM SermonParagraphs p JOIN Sermons s ON s.Id = p.SermonId
                WHERE {ImportedFilter}
                GROUP BY p.SermonId
                HAVING MIN(p.ParagraphNumber) <> 1 OR MAX(p.ParagraphNumber) <> COUNT(1))
            """));
    }
}
