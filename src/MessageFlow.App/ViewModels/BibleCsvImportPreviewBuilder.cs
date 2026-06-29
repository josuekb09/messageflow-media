using System.IO;
using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public static class BibleCsvImportPreviewBuilder
{
    public static BibleImportPreviewSummary Build(
        string translationName,
        string abbreviation,
        string language,
        string description,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("Select an existing local Bible CSV file.", filePath);
        }

        var lines = File.ReadAllLines(filePath);
        var verses = new List<BibleCsvVerseRow>();
        var invalidRows = new List<string>();

        if (lines.Length == 0)
        {
            invalidRows.Add("The CSV file is empty.");
            return CreateSummary(translationName, abbreviation, language, description, filePath, verses, invalidRows);
        }

        var header = ParseCsvLine(lines[0]);
        var columns = CreateColumnMap(header);
        if (!columns.TryGetValue("book", out var bookIndex) ||
            !columns.TryGetValue("chapter", out var chapterIndex) ||
            !columns.TryGetValue("verse", out var verseIndex) ||
            !columns.TryGetValue("text", out var textIndex))
        {
            invalidRows.Add("Header must contain book, chapter, verse, and text columns.");
            return CreateSummary(translationName, abbreviation, language, description, filePath, verses, invalidRows);
        }

        var maxIndex = new[] { bookIndex, chapterIndex, verseIndex, textIndex }.Max();
        for (var index = 1; index < lines.Length; index++)
        {
            var line = lines[index];
            var rowNumber = index + 1;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count <= maxIndex)
            {
                invalidRows.Add($"Row {rowNumber}: expected book, chapter, verse, and text.");
                continue;
            }

            var book = fields[bookIndex].Trim();
            var chapterText = fields[chapterIndex].Trim();
            var verseText = fields[verseIndex].Trim();
            var text = fields[textIndex].Trim();

            if (!BibleReferenceParser.TryNormalizeBookName(book, out var bookName))
            {
                invalidRows.Add($"Row {rowNumber}: unknown Bible book '{book}'.");
                continue;
            }

            if (!int.TryParse(chapterText, out var chapter) || chapter <= 0)
            {
                invalidRows.Add($"Row {rowNumber}: invalid chapter '{chapterText}'.");
                continue;
            }

            if (!int.TryParse(verseText, out var verse) || verse <= 0)
            {
                invalidRows.Add($"Row {rowNumber}: invalid verse '{verseText}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                invalidRows.Add($"Row {rowNumber}: verse text is empty.");
                continue;
            }

            verses.Add(new BibleCsvVerseRow(rowNumber, bookName, chapter, verse, text));
        }

        return CreateSummary(translationName, abbreviation, language, description, filePath, verses, invalidRows);
    }

    private static BibleImportPreviewSummary CreateSummary(
        string translationName,
        string abbreviation,
        string language,
        string description,
        string filePath,
        IReadOnlyList<BibleCsvVerseRow> verses,
        IReadOnlyList<string> invalidRows)
    {
        return new BibleImportPreviewSummary(
            translationName.Trim(),
            abbreviation.Trim().ToUpperInvariant(),
            language.Trim(),
            description.Trim(),
            filePath,
            verses,
            invalidRows);
    }

    private static Dictionary<string, int> CreateColumnMap(IReadOnlyList<string> header)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < header.Count; index++)
        {
            var name = header[index].Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                columns[name] = index;
            }
        }

        return columns;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
                continue;
            }

            field.Append(character);
        }

        fields.Add(field.ToString());
        return fields;
    }
}
