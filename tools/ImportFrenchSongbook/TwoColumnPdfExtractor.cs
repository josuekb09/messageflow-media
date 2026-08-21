using System.Text;
using MessageFlow.Importer;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ImportFrenchSongbook;

internal static class TwoColumnPdfExtractor
{
    public static IReadOnlyList<ExtractedPage> ExtractPages(string pdfPath)
    {
        using var document = PdfDocument.Open(pdfPath);
        return document.GetPages()
            .Select(page => new ExtractedPage(page.Number, ExtractPageText(page)))
            .ToList();
    }

    private static string ExtractPageText(Page page)
    {
        var words = page.GetWords()
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .Select(word => new PositionedWord(word, TextCleaner.CleanToken(RebuildWord(word))))
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .ToList();

        if (words.Count == 0)
        {
            return string.Empty;
        }

        var columns = SplitColumns(words, page.Width);
        var builder = new StringBuilder();
        foreach (var column in columns)
        {
            var columnText = BuildColumnText(column);
            if (string.IsNullOrWhiteSpace(columnText))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(columnText);
        }

        return builder.ToString().Trim();
    }

    private static IReadOnlyList<IReadOnlyList<PositionedWord>> SplitColumns(
        IReadOnlyList<PositionedWord> words,
        double pageWidth)
    {
        var splitX = FindGutter(words, pageWidth);
        if (splitX is null)
        {
            return [words];
        }

        var left = words.Where(word => word.CenterX < splitX.Value).ToList();
        var right = words.Where(word => word.CenterX >= splitX.Value).ToList();
        if (left.Count < 8 || right.Count < 8)
        {
            return [words];
        }

        return [left, right];
    }

    private static double? FindGutter(IReadOnlyList<PositionedWord> words, double pageWidth)
    {
        var mid = pageWidth / 2;
        var centers = words.Select(word => word.CenterX).OrderBy(x => x).ToList();
        if (centers.Count < 20)
        {
            return mid;
        }

        double bestGap = 0;
        double bestSplit = mid;
        for (var i = 1; i < centers.Count; i++)
        {
            var gap = centers[i] - centers[i - 1];
            var split = (centers[i] + centers[i - 1]) / 2;
            if (gap > bestGap && split > pageWidth * 0.28 && split < pageWidth * 0.72)
            {
                bestGap = gap;
                bestSplit = split;
            }
        }

        var medianWidth = words
            .Select(word => word.Width)
            .Where(width => width > 0)
            .Order()
            .DefaultIfEmpty(8)
            .ElementAt(Math.Min(words.Count / 2, words.Count - 1));

        return bestGap >= Math.Max(10, medianWidth * 1.6) ? bestSplit : mid;
    }

    private static string BuildColumnText(IReadOnlyList<PositionedWord> words)
    {
        var tolerance = CalculateLineTolerance(words);
        var lines = GroupIntoLines(words, tolerance)
            .OrderByDescending(line => line.CenterY)
            .ToList();

        var builder = new StringBuilder();
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
            }

            builder.Append(lineText);
        }

        return builder.ToString();
    }

    private static List<PageLine> GroupIntoLines(IReadOnlyList<PositionedWord> words, double tolerance)
    {
        var lines = new List<PageLine>();
        foreach (var word in words.OrderByDescending(word => word.CenterY).ThenBy(word => word.Left))
        {
            var line = lines.FirstOrDefault(existing => Math.Abs(existing.CenterY - word.CenterY) <= tolerance);
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

        return Math.Max(2.5, heights[heights.Count / 2] * 0.55);
    }

    private static string BuildLineText(IEnumerable<PositionedWord> words)
    {
        var builder = new StringBuilder();
        foreach (var word in words.OrderBy(word => word.Left))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(word.Text);
        }

        return TextCleaner.CleanExtractedText(builder.ToString(), preserveLineBreaks: true);
    }

    private static string RebuildWord(Word word)
    {
        var letters = word.Letters
            .Where(letter => !string.IsNullOrEmpty(letter.Value))
            .OrderBy(letter => letter.GlyphRectangle.Left)
            .ToList();
        if (letters.Count <= 1)
        {
            return word.Text;
        }

        return string.Concat(letters.Select(letter => letter.Value));
    }

    private sealed record PositionedWord(Word Source, string Text)
    {
        public double Left => Source.BoundingBox.Left;

        public double CenterX => (Source.BoundingBox.Left + Source.BoundingBox.Right) / 2;

        public double Width => Source.BoundingBox.Width;

        public double Top => Source.BoundingBox.Top;

        public double Bottom => Source.BoundingBox.Bottom;

        public double Height => Source.BoundingBox.Height;

        public double CenterY => (Top + Bottom) / 2;
    }

    private sealed class PageLine
    {
        private double centerTotal;

        public PageLine(PositionedWord word) => Add(word);

        public List<PositionedWord> Words { get; } = [];

        public double CenterY { get; private set; }

        public void Add(PositionedWord word)
        {
            Words.Add(word);
            centerTotal += word.CenterY;
            CenterY = centerTotal / Words.Count;
        }
    }
}
