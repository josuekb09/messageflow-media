using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MessageFlow.Importer;

public sealed class PdfTextExtractor
{
    public IReadOnlyList<ExtractedPage> ExtractPages(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        return document.GetPages()
            .Select(page => new ExtractedPage(page.Number, ExtractPageText(page)))
            .ToList();
    }

    private static string ExtractPageText(Page page)
    {
        var words = page.GetWords()
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .Select(word => new PositionedWord(word, TextCleaner.CleanToken(RebuildWordTextFromLetters(word))))
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .ToList();

        if (words.Count == 0)
        {
            return TextCleaner.CleanExtractedText(page.Text ?? string.Empty, preserveLineBreaks: true);
        }

        var lineTolerance = CalculateLineTolerance(words);
        var lines = GroupIntoLines(words, lineTolerance)
            .OrderByDescending(line => line.CenterY)
            .ToList();

        var averageLineHeight = words
            .Select(word => word.Height)
            .Where(height => height > 0)
            .DefaultIfEmpty(10)
            .Average();
        var paragraphGap = Math.Max(averageLineHeight * 0.95, lineTolerance * 2);

        var builder = new StringBuilder();
        PageLine? previousLine = null;

        foreach (var line in lines)
        {
            var lineText = BuildLineText(line.Words);
            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();

                if (previousLine is not null && previousLine.Bottom - line.Top > paragraphGap)
                {
                    builder.AppendLine();
                }
            }

            builder.Append(lineText);
            previousLine = line;
        }

        return TextCleaner.CleanExtractedText(builder.ToString(), preserveLineBreaks: true);
    }

    private static List<PageLine> GroupIntoLines(IReadOnlyList<PositionedWord> words, double tolerance)
    {
        var lines = new List<PageLine>();

        foreach (var word in words.OrderByDescending(word => word.CenterY).ThenBy(word => word.Left))
        {
            var line = lines.FirstOrDefault(existingLine => Math.Abs(existingLine.CenterY - word.CenterY) <= tolerance);
            if (line is null)
            {
                lines.Add(new PageLine(word));
                continue;
            }

            line.Add(word);
        }

        return lines;
    }

    private static double CalculateLineTolerance(IReadOnlyList<PositionedWord> words)
    {
        var heights = words
            .Select(word => word.Height)
            .Where(height => height > 0)
            .Order()
            .ToList();

        if (heights.Count == 0)
        {
            return 2.5;
        }

        var medianHeight = heights[heights.Count / 2];
        return Math.Max(2.5, medianHeight * 0.55);
    }

    private static string BuildLineText(IEnumerable<PositionedWord> words)
    {
        var builder = new StringBuilder();

        foreach (var word in words.OrderBy(word => word.Left).ThenByDescending(word => word.Top))
        {
            AppendWord(builder, word.Text);
        }

        return TextCleaner.CleanExtractedText(builder.ToString());
    }

    private static string RebuildWordTextFromLetters(Word word)
    {
        var letters = word.Letters
            .Where(letter => !string.IsNullOrEmpty(letter.Value))
            .OrderBy(letter => letter.GlyphRectangle.Left)
            .ThenByDescending(letter => letter.GlyphRectangle.Top)
            .ToList();

        if (letters.Count <= 1)
        {
            return word.Text;
        }

        var medianLetterWidth = CalculateMedianLetterWidth(letters);
        var internalSpaceGap = Math.Max(1.25, medianLetterWidth * 0.55);
        var builder = new StringBuilder(word.Text.Length + 4);
        Letter? previousLetter = null;

        foreach (var letter in letters)
        {
            var value = letter.Value;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (builder.Length > 0 &&
                previousLetter is not null &&
                letter.GlyphRectangle.Left - previousLetter.GlyphRectangle.Right > internalSpaceGap)
            {
                builder.Append(' ');
            }

            builder.Append(value);
            previousLetter = letter;
        }

        return builder.Length == 0 ? word.Text : builder.ToString();
    }

    private static double CalculateMedianLetterWidth(IReadOnlyCollection<Letter> letters)
    {
        var widths = letters
            .Select(letter => letter.GlyphRectangle.Width)
            .Where(width => width > 0)
            .Order()
            .ToList();

        return widths.Count == 0 ? 4 : widths[widths.Count / 2];
    }

    private static void AppendWord(StringBuilder builder, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        if (builder.Length == 0)
        {
            builder.Append(word);
            return;
        }

        var previous = builder[^1];
        var first = word[0];

        if (ShouldAttachToPrevious(previous, first))
        {
            builder.Append(word);
            return;
        }

        builder.Append(' ');
        builder.Append(word);
    }

    private static bool ShouldAttachToPrevious(char previous, char current)
    {
        if (current is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']' or '}' or '%')
        {
            return true;
        }

        if (current == '\'' && char.IsLetterOrDigit(previous))
        {
            return true;
        }

        return previous is '(' or '[' or '{';
    }

    private sealed record PositionedWord(Word Source, string Text)
    {
        public double Left => Source.BoundingBox.Left;

        public double Top => Source.BoundingBox.Top;

        public double Bottom => Source.BoundingBox.Bottom;

        public double Height => Source.BoundingBox.Height;

        public double CenterY => (Top + Bottom) / 2;
    }

    private sealed class PageLine
    {
        private double centerTotal;

        public PageLine(PositionedWord word)
        {
            Add(word);
        }

        public List<PositionedWord> Words { get; } = [];

        public double CenterY { get; private set; }

        public double Top { get; private set; }

        public double Bottom { get; private set; }

        public void Add(PositionedWord word)
        {
            Words.Add(word);

            centerTotal += word.CenterY;
            CenterY = centerTotal / Words.Count;
            Top = Words.Count == 1 ? word.Top : Math.Max(Top, word.Top);
            Bottom = Words.Count == 1 ? word.Bottom : Math.Min(Bottom, word.Bottom);
        }
    }
}
