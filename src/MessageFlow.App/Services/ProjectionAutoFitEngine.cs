using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App.Services;

public sealed record ProjectionLayoutResult(
    IReadOnlyList<string> Pages,
    double FontSize,
    double LineHeight,
    double TitleFontSize,
    double TitleLineHeight,
    double NumberFontSize,
    double IndicatorFontSize,
    Thickness RootMargin,
    bool IsPaginated,
    double StageWidth,
    double StageHeight,
    bool IsTitleOnlySlide,
    double MinimumReadableFontSize = 0,
    bool IsAtLargestFit = false,
    bool IsAtSmallestReadable = false);

/// <summary>
/// Robust layout engine for projection screens.
/// Guarantees that every projected item (sermon paragraph, Bible verse, song section,
/// Brother Frank publication) fits entirely inside the projection window with safe margins,
/// zero overflow, zero clipping, and zero scrolling.
/// 
/// When text cannot fit on a single screen at the minimum readable font size, it splits
/// at sentence boundaries into consecutive continuation screens ("1 of 2", "2 of 2")
/// preserving the EXACT original text byte-for-byte.
/// </summary>
public static partial class ProjectionAutoFitEngine
{
    private readonly record struct LayoutKey(
        ProjectionContentType ContentType,
        string Title,
        string Subtitle,
        string Text,
        double Width,
        double Height,
        double Scale,
        bool IsTitleSlide);

    private const double EmergencyMinimumFontSize = 14;
    private const double PrecisionStep = 0.5;
    private const double SafetyBuffer = 8.0;

    private static readonly FontFamily DefaultFontFamily = new("Segoe UI");

    private const int LayoutCacheCapacity = 64;
    private const double MinimumWordFitFontSize = 8;
    private const int WordWidthCacheCapacity = 20000;

    [ThreadStatic]
    private static Dictionary<(string Word, int Weight), double>? wordWidthsAt100;

    [ThreadStatic]
    private static TextBlock? wordMeasuringBlock;

    /// <summary>
    /// Width of the widest whitespace-separated word of <paramref name="text"/> laid out on one
    /// line at <paramref name="fontSize"/>. Glyph widths scale with the size, so each word is
    /// measured once at 100 and cached.
    /// </summary>
    public static double WidestWordWidth(string text, FontWeight fontWeight, double fontSize)
    {
        var cache = wordWidthsAt100 ??= [];
        if (cache.Count > WordWidthCacheCapacity)
        {
            cache.Clear();
        }

        var weight = fontWeight.ToOpenTypeWeight();
        var widest = 0.0;
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!cache.TryGetValue((word, weight), out var width))
            {
                var block = wordMeasuringBlock ??= new TextBlock
                {
                    FontFamily = DefaultFontFamily,
                    FontSize = 100,
                    TextWrapping = TextWrapping.NoWrap
                };
                block.Text = word;
                block.FontWeight = fontWeight;
                block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                width = block.DesiredSize.Width;
                cache[(word, weight)] = width;
            }

            widest = Math.Max(widest, width);
        }

        return widest * fontSize / 100;
    }

    [ThreadStatic]
    private static TextBlock? measuringBlock;

    [ThreadStatic]
    private static Dictionary<LayoutKey, ProjectionLayoutResult>? layoutCacheStorage;

    [ThreadStatic]
    private static Queue<LayoutKey>? layoutCacheOrderStorage;

    private static Dictionary<LayoutKey, ProjectionLayoutResult> layoutCache => layoutCacheStorage ??= [];

    private static Queue<LayoutKey> layoutCacheOrder => layoutCacheOrderStorage ??= new Queue<LayoutKey>();

    public static ProjectionLayoutResult CalculateLayout(
        ProjectionContentType contentType,
        string rawTitle,
        string rawSubtitle,
        string rawText,
        double windowWidth,
        double windowHeight,
        double dpiScaleX = 1.0,
        double dpiScaleY = 1.0,
        double operatorScaleMultiplier = 1.0,
        bool isTitleSlide = false)
    {
        // Going back to a paragraph, or re-projecting it, reuses its layout instead of measuring
        // the text again. The layout depends only on these inputs.
        var key = new LayoutKey(contentType, rawTitle, rawSubtitle, rawText, windowWidth, windowHeight, operatorScaleMultiplier, isTitleSlide);
        if (layoutCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var layout = ComputeLayout(contentType, rawTitle, rawSubtitle, rawText, windowWidth, windowHeight, operatorScaleMultiplier, isTitleSlide);
        if (layoutCacheOrder.Count >= LayoutCacheCapacity)
        {
            layoutCache.Remove(layoutCacheOrder.Dequeue());
        }

        layoutCache[key] = layout;
        layoutCacheOrder.Enqueue(key);
        return layout;
    }

    private static ProjectionLayoutResult ComputeLayout(
        ProjectionContentType contentType,
        string rawTitle,
        string rawSubtitle,
        string rawText,
        double windowWidth,
        double windowHeight,
        double operatorScaleMultiplier,
        bool isTitleSlide)
    {
        var isSong = contentType == ProjectionContentType.Song;
        var isBible = contentType == ProjectionContentType.Bible;
        var isSermon = contentType == ProjectionContentType.Sermon;

        // 1. Calculate safe root margins
        var horizontalMargin = Math.Clamp(windowWidth * 0.032, 24, 58);
        var verticalMargin = Math.Clamp(windowHeight * 0.030, 20, 52);
        var rootMargin = new Thickness(horizontalMargin, verticalMargin, horizontalMargin, verticalMargin);

        var rootWidth = Math.Max(100, windowWidth - (horizontalMargin * 2));
        var rootHeight = Math.Max(100, windowHeight - (verticalMargin * 2));

        // 2. Normalize body text
        var text = isSong ? NormalizeSongText(rawText) : NormalizeProseText(rawText);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new ProjectionLayoutResult(
                Pages: [string.Empty],
                FontSize: 36,
                LineHeight: 44,
                TitleFontSize: 32,
                TitleLineHeight: 38,
                NumberFontSize: 22,
                IndicatorFontSize: 16,
                RootMargin: rootMargin,
                IsPaginated: false,
                StageWidth: rootWidth,
                StageHeight: rootHeight,
                IsTitleOnlySlide: isTitleSlide,
                IsAtLargestFit: true,
                IsAtSmallestReadable: true);
        }

        // 3. Header dimensions
        double titleFontSize;
        double titleLineHeight;
        double titleHeight = 0;

        if (isSong)
        {
            // Songs show only their own slide text: the first slide already carries the title,
            // so no generated header is added and the whole stage is left to the lyrics.
            titleFontSize = 0;
            titleLineHeight = 0;
            titleHeight = 0;
        }
        else if (isBible)
        {
            titleFontSize = Math.Clamp(rootHeight * 0.078, 30, 68);
            titleLineHeight = titleFontSize * 1.10;
            titleHeight = MeasureTextHeight(rawTitle, rootWidth, titleFontSize, titleLineHeight, FontWeights.Bold) + 12;
        }
        else
        {
            // Sermon or Brother Frank publication
            titleFontSize = Math.Clamp(rootHeight * 0.046, 22, 38);
            titleLineHeight = titleFontSize * 1.14;
            titleHeight = string.IsNullOrWhiteSpace(rawTitle)
                ? 0
                : MeasureTextHeight(rawTitle, rootWidth, titleFontSize, titleLineHeight, FontWeights.SemiBold) + 8;
        }

        double numberFontSize = 0;
        double numberHeight = 0;
        if (isSermon && !string.IsNullOrWhiteSpace(rawSubtitle))
        {
            numberFontSize = Math.Clamp(titleFontSize * 0.72, 18, 26);
            numberHeight = MeasureTextHeight(rawSubtitle, rootWidth, numberFontSize, numberFontSize * 1.15, FontWeights.SemiBold) + 16;
        }

        // Page indicator dimensions (reserved whenever paged)
        var indicatorFontSize = Math.Clamp(rootHeight * 0.026, 14, 20);
        var indicatorHeight = MeasureTextHeight("Page 99 of 99", rootWidth, indicatorFontSize, indicatorFontSize * 1.2, FontWeights.SemiBold) + 16;

        // Stage available dimensions
        var stageWidth = rootWidth;
        var safeStageWidth = Math.Max(50, stageWidth - SafetyBuffer);

        var stageHeightSingle = Math.Max(50, rootHeight - titleHeight - numberHeight);
        var safeStageHeightSingle = Math.Max(50, stageHeightSingle - SafetyBuffer);

        var stageHeightPaged = Math.Max(50, rootHeight - titleHeight - numberHeight - indicatorHeight);
        var safeStageHeightPaged = Math.Max(50, stageHeightPaged - SafetyBuffer);

        // 4. Readability boundaries. A word wider than the stage would be broken in two by WPF,
        // so when even the readable minimum is too large for the longest word, the minimum
        // becomes the largest size at which that word fits whole.
        var fontWeightForWords = isBible || isSong ? FontWeights.Bold : FontWeights.SemiBold;
        var widestWordAt100 = WidestWordWidth(text, fontWeightForWords, 100);
        var wordLimitedSize = widestWordAt100 > 0
            ? Math.Floor(safeStageWidth * 100 / widestWordAt100 / PrecisionStep) * PrecisionStep
            : double.MaxValue;
        var minReadable = Math.Max(
            MinimumWordFitFontSize,
            Math.Min(Math.Clamp(rootHeight * 0.042, 24, 85), wordLimitedSize));
        var maxAesthetic = isSong && isTitleSlide
            ? Math.Clamp(rootHeight * 0.14, 54, 180)
            : isBible
                ? Math.Clamp(rootHeight * 0.11, 48, 160)
                : isSong
                    ? Math.Clamp(rootHeight * 0.10, 44, 150)
                    : Math.Clamp(rootHeight * 0.09, 44, 130);

        var fontWeight = isBible || isSong ? FontWeights.Bold : FontWeights.SemiBold;
        var textAlignment = isSong ? TextAlignment.Center : TextAlignment.Left;

        // 5. Try single-screen fit first
        var singleFit = FindMaxFittingFontSize(
            text,
            safeStageWidth,
            safeStageHeightSingle,
            minReadable,
            maxAesthetic,
            fontWeight,
            textAlignment,
            contentType);

        if (singleFit >= minReadable)
        {
            // Fits entirely on a single screen. A+ can never go past the largest size that fits
            // and A- never below the readable minimum.
            var scaled = singleFit * operatorScaleMultiplier;
            var finalFontSize = Math.Clamp(scaled, minReadable, singleFit);
            var lineHeight = CalculateLineHeight(finalFontSize, contentType);

            return new ProjectionLayoutResult(
                Pages: [text],
                FontSize: finalFontSize,
                LineHeight: lineHeight,
                TitleFontSize: titleFontSize,
                TitleLineHeight: titleLineHeight,
                NumberFontSize: numberFontSize,
                IndicatorFontSize: indicatorFontSize,
                RootMargin: rootMargin,
                IsPaginated: false,
                StageWidth: stageWidth,
                StageHeight: stageHeightSingle,
                IsTitleOnlySlide: isTitleSlide,
                MinimumReadableFontSize: minReadable,
                IsAtLargestFit: finalFontSize >= singleFit - 0.01,
                IsAtSmallestReadable: finalFontSize <= minReadable + 0.01);
        }

        // 6. Text is too long for single screen: paginate at sentence boundaries
        var basePageFontSize = Math.Clamp(rootHeight * 0.058, minReadable, maxAesthetic * 0.72);
        var targetPageFontSize = Math.Clamp(basePageFontSize * operatorScaleMultiplier, minReadable, maxAesthetic);

        // Every page is cut at the size it is shown at. When a page cannot be cut to fit (a single
        // unbreakable run longer than the stage), cut again at a smaller size, but never below the
        // readable minimum; only text that cannot fit even word by word goes under it.
        var pageFontSize = targetPageFontSize;
        List<string> pages;
        bool pagesFit;
        while (true)
        {
            pages = SplitIntoCleanPages(
                text,
                safeStageWidth,
                safeStageHeightPaged,
                pageFontSize,
                fontWeight,
                textAlignment,
                contentType);

            pagesFit = AllPagesFit(pages, safeStageWidth, safeStageHeightPaged, pageFontSize, fontWeight, textAlignment, contentType);
            if (pagesFit || pageFontSize <= minReadable)
            {
                break;
            }

            pageFontSize = Math.Max(minReadable, Math.Floor(pageFontSize * 0.92 / PrecisionStep) * PrecisionStep);
        }

        // Strict verification: ensure every page fits in the stage
        var verifiedFontSize = pagesFit ? pageFontSize : FindMaxFittingFontSizeForAllPages(
            pages,
            safeStageWidth,
            safeStageHeightPaged,
            Math.Min(EmergencyMinimumFontSize, minReadable),
            pageFontSize,
            fontWeight,
            textAlignment,
            contentType);

        var pagedLineHeight = CalculateLineHeight(verifiedFontSize, contentType);

        return new ProjectionLayoutResult(
            Pages: pages,
            FontSize: verifiedFontSize,
            LineHeight: pagedLineHeight,
            TitleFontSize: titleFontSize,
            TitleLineHeight: titleLineHeight,
            NumberFontSize: numberFontSize,
            IndicatorFontSize: indicatorFontSize,
            RootMargin: rootMargin,
            IsPaginated: pages.Count > 1,
            StageWidth: stageWidth,
            StageHeight: stageHeightPaged,
            IsTitleOnlySlide: isTitleSlide,
            MinimumReadableFontSize: minReadable,
            IsAtLargestFit: targetPageFontSize >= maxAesthetic - 0.01,
            IsAtSmallestReadable: verifiedFontSize <= minReadable + 0.01);
    }

    private static bool AllPagesFit(
        IReadOnlyList<string> pages,
        double maxWidth,
        double maxHeight,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        ProjectionContentType contentType)
    {
        foreach (var page in pages)
        {
            if (!DoesTextFit(page, maxWidth, maxHeight, fontSize, fontWeight, alignment, contentType))
            {
                return false;
            }
        }

        return true;
    }

    public static double FindMaxFittingFontSize(
        string text,
        double maxWidth,
        double maxHeight,
        double minSize,
        double maxSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        ProjectionContentType contentType)
    {
        if (!DoesTextFit(text, maxWidth, maxHeight, minSize, fontWeight, alignment, contentType))
        {
            return 0;
        }

        var low = minSize;
        var high = maxSize;

        while (high - low > PrecisionStep)
        {
            var mid = low + ((high - low) / 2.0);
            if (DoesTextFit(text, maxWidth, maxHeight, mid, fontWeight, alignment, contentType))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return Math.Floor(low / PrecisionStep) * PrecisionStep;
    }

    private static double FindMaxFittingFontSizeForAllPages(
        IReadOnlyList<string> pages,
        double maxWidth,
        double maxHeight,
        double minSize,
        double targetSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        ProjectionContentType contentType)
    {
        if (AllPagesFit(pages, maxWidth, maxHeight, targetSize, fontWeight, alignment, contentType))
        {
            return targetSize;
        }

        var low = minSize;
        var high = targetSize;

        while (high - low > PrecisionStep)
        {
            var mid = low + ((high - low) / 2.0);
            if (AllPagesFit(pages, maxWidth, maxHeight, mid, fontWeight, alignment, contentType))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return Math.Floor(low / PrecisionStep) * PrecisionStep;
    }

    public static bool DoesTextFit(
        string text,
        double maxWidth,
        double maxHeight,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        ProjectionContentType contentType)
    {
        if (maxWidth <= 0 || maxHeight <= 0 || fontSize <= 0)
        {
            return false;
        }

        // One measuring block per thread: pagination measures hundreds of candidates and a new
        // TextBlock each time was most of the cost of switching paragraphs.
        var tb = measuringBlock ??= new TextBlock
        {
            FontFamily = DefaultFontFamily,
            TextWrapping = TextWrapping.Wrap
        };
        tb.Text = text;
        tb.FontWeight = fontWeight;
        tb.FontSize = fontSize;
        tb.LineHeight = CalculateLineHeight(fontSize, contentType);
        tb.TextAlignment = alignment;
        tb.Measure(new Size(maxWidth, double.PositiveInfinity));

        // A word wider than the line is still "inside" the wrapped block, broken in two;
        // only text whose every word fits whole on a line counts as fitting.
        return tb.DesiredSize.Width <= maxWidth &&
               tb.DesiredSize.Height <= maxHeight &&
               WidestWordWidth(text, fontWeight, fontSize) <= maxWidth;
    }

    private static double MeasureTextHeight(
        string text,
        double maxWidth,
        double fontSize,
        double lineHeight,
        FontWeight fontWeight)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0 || fontSize <= 0)
        {
            return 0;
        }

        var tb = new TextBlock
        {
            Text = text,
            FontFamily = DefaultFontFamily,
            FontWeight = fontWeight,
            FontSize = fontSize,
            LineHeight = lineHeight,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        tb.Measure(new Size(maxWidth, double.PositiveInfinity));
        return tb.DesiredSize.Height;
    }

    public static double CalculateLineHeight(double fontSize, ProjectionContentType contentType)
    {
        return contentType switch
        {
            ProjectionContentType.Bible => fontSize * 1.10,
            ProjectionContentType.Song => fontSize * 1.12,
            ProjectionContentType.Sermon => fontSize * 1.14,
            _ => fontSize * 1.14
        };
    }

    /// <summary>
    /// Partitions text into pages at sentence boundaries (song lines for songs), using clause and
    /// then word boundaries only for a sentence too long for a page on its own.
    /// Preserves exact characters without alteration or omission.
    /// </summary>
    public static List<string> SplitIntoCleanPages(
        string text,
        double maxWidth,
        double maxHeight,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        ProjectionContentType contentType)
    {
        var units = contentType == ProjectionContentType.Song
            ? ExtractSongUnits(text)
            : ExtractSentenceUnits(text);

        bool Fits(int start, int count) =>
            DoesTextFit(Join(units, start, count).Trim(), maxWidth, maxHeight, fontSize, fontWeight, alignment, contentType);

        var pages = new List<string>();
        var index = 0;
        while (index < units.Count)
        {
            if (!Fits(index, 1))
            {
                // One unit alone is too tall or wide: replace it by its clauses (or words) and retry.
                var pieces = SubdivideOversizedUnit(units[index]);
                if (pieces.Count > 1)
                {
                    units.RemoveAt(index);
                    units.InsertRange(index, pieces);
                    continue;
                }

                // A single word longer than the stage: it gets a page of its own and the final
                // font-size verification shrinks it until it fits.
                AddPage(pages, units[index]);
                index++;
                continue;
            }

            // Largest run of whole units that fits: grow exponentially, then binary-search.
            // Taller text never fits where shorter text did not, so the search is exact.
            var fitting = 1;
            var step = 1;
            while (index + fitting < units.Count && Fits(index, Math.Min(units.Count - index, fitting + step)))
            {
                fitting = Math.Min(units.Count - index, fitting + step);
                step *= 2;
            }

            var low = fitting;
            var high = Math.Min(units.Count - index, fitting + step);
            while (high - low > 1)
            {
                var mid = (low + high) / 2;
                if (Fits(index, mid))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            AddPage(pages, Join(units, index, low));
            index += low;
        }

        return pages.Count > 0 ? pages : [text];
    }

    private static string Join(List<string> units, int start, int count)
    {
        if (count == 1)
        {
            return units[start];
        }

        var builder = new StringBuilder();
        for (var i = start; i < start + count; i++)
        {
            builder.Append(units[i]);
        }

        return builder.ToString();
    }

    private static void AddPage(List<string> pages, string page)
    {
        var trimmed = page.Trim();
        if (trimmed.Length > 0)
        {
            pages.Add(trimmed);
        }
    }

    /// <summary>
    /// Extracts sentence-level units from prose text.
    /// Each unit retains its trailing punctuation and whitespace.
    /// </summary>
    private static List<string> ExtractSentenceUnits(string text)
    {
        var units = new List<string>();
        var pattern = SentenceDelimiterRegex();
        var matches = pattern.Matches(text);

        var lastIndex = 0;
        foreach (Match match in matches)
        {
            var endIndex = match.Index + match.Length;
            var sentence = text[lastIndex..endIndex];
            if (!string.IsNullOrEmpty(sentence))
            {
                units.Add(sentence);
            }
            lastIndex = endIndex;
        }

        if (lastIndex < text.Length)
        {
            var remaining = text[lastIndex..];
            if (!string.IsNullOrEmpty(remaining))
            {
                units.Add(remaining);
            }
        }

        return units;
    }

    /// <summary>
    /// Extracts lines/stanzas for songs.
    /// </summary>
    private static List<string> ExtractSongUnits(string text)
    {
        var units = new List<string>();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var suffix = i < lines.Length - 1 ? "\n" : string.Empty;
            units.Add(line + suffix);
        }
        return units;
    }

    /// <summary>
    /// Subdivides an oversized sentence into clause-level units (semicolons, colons, dashes, commas),
    /// or into words when it has no clause boundary.
    /// </summary>
    private static List<string> SubdivideOversizedUnit(string unit)
    {
        var result = new List<string>();
        var pattern = ClauseDelimiterRegex();
        var matches = pattern.Matches(unit);

        var lastIndex = 0;
        foreach (Match match in matches)
        {
            var endIndex = match.Index + match.Length;
            var clause = unit[lastIndex..endIndex];
            if (!string.IsNullOrEmpty(clause))
            {
                result.Add(clause);
            }
            lastIndex = endIndex;
        }

        if (lastIndex < unit.Length)
        {
            var remaining = unit[lastIndex..];
            if (!string.IsNullOrEmpty(remaining))
            {
                result.Add(remaining);
            }
        }

        // If no clause delimiters existed (one massive uninterrupted clause), split by words.
        // A clause that is still too long is subdivided again by the caller, down to words.
        if (result.Count <= 1)
        {
            result.Clear();
            var trailing = unit[unit.TrimEnd().Length..];
            var words = unit.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                // The last word keeps the unit's own trailing space or line break, so it is not
                // glued to the first word of the next unit.
                result.Add(i < words.Length - 1 ? words[i] + " " : words[i].TrimEnd() + trailing);
            }
        }

        return result;
    }

    public static string NormalizeProseText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        return MultipleWhitespaceRegex().Replace(normalized.Trim(), " ");
    }

    public static string NormalizeSongText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var lines = normalized
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join("\n", lines);
    }

    [GeneratedRegex("""(?<=[.!?]["'\)\]}]?)\s+""")]
    private static partial Regex SentenceDelimiterRegex();

    [GeneratedRegex(@"(?<=[;:,—]|--)\s+")]
    private static partial Regex ClauseDelimiterRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();
}
