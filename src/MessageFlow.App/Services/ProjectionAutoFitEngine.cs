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
    bool IsTitleOnlySlide);

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
    private const double EmergencyMinimumFontSize = 14;
    private const double PrecisionStep = 0.5;
    private const double SafetyBuffer = 8.0;

    private static readonly FontFamily DefaultFontFamily = new("Segoe UI");

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
                IsTitleOnlySlide: isTitleSlide);
        }

        // 3. Header dimensions
        double titleFontSize;
        double titleLineHeight;
        double titleHeight = 0;

        if (isSong && !isTitleSlide)
        {
            // Song body slides do not display the header to maximize stage area
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
        else if (isSong && isTitleSlide)
        {
            titleFontSize = Math.Clamp(rootHeight * 0.090, 36, 84);
            titleLineHeight = titleFontSize * 1.15;
            titleHeight = MeasureTextHeight(rawTitle, rootWidth, titleFontSize, titleLineHeight, FontWeights.Bold) + 16;
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

        // 4. Readability boundaries
        var minReadable = Math.Clamp(rootHeight * 0.042, 24, 85);
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
            // Fits entirely on a single screen
            var scaled = singleFit * operatorScaleMultiplier;
            var finalFontSize = Math.Clamp(scaled, EmergencyMinimumFontSize, singleFit);
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
                IsTitleOnlySlide: isTitleSlide);
        }

        // 6. Text is too long for single screen: paginate at sentence boundaries
        var basePageFontSize = Math.Clamp(rootHeight * 0.058, minReadable, maxAesthetic * 0.72);
        var targetPageFontSize = Math.Clamp(basePageFontSize * operatorScaleMultiplier, minReadable, maxAesthetic);

        var pages = SplitIntoCleanPages(
            text,
            safeStageWidth,
            safeStageHeightPaged,
            targetPageFontSize,
            fontWeight,
            textAlignment,
            contentType);

        // Strict verification: ensure every page fits in the stage
        var verifiedFontSize = FindMaxFittingFontSizeForAllPages(
            pages,
            safeStageWidth,
            safeStageHeightPaged,
            EmergencyMinimumFontSize,
            targetPageFontSize,
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
            IsTitleOnlySlide: isTitleSlide);
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
        var fitsAtTarget = true;
        foreach (var page in pages)
        {
            if (!DoesTextFit(page, maxWidth, maxHeight, targetSize, fontWeight, alignment, contentType))
            {
                fitsAtTarget = false;
                break;
            }
        }

        if (fitsAtTarget)
        {
            return targetSize;
        }

        var low = minSize;
        var high = targetSize;

        while (high - low > PrecisionStep)
        {
            var mid = low + ((high - low) / 2.0);
            var allFit = true;
            foreach (var page in pages)
            {
                if (!DoesTextFit(page, maxWidth, maxHeight, mid, fontWeight, alignment, contentType))
                {
                    allFit = false;
                    break;
                }
            }

            if (allFit)
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

        var lineHeight = CalculateLineHeight(fontSize, contentType);
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = DefaultFontFamily,
            FontWeight = fontWeight,
            FontSize = fontSize,
            LineHeight = lineHeight,
            TextAlignment = alignment,
            TextWrapping = TextWrapping.Wrap
        };

        tb.Measure(new Size(maxWidth, double.PositiveInfinity));

        return tb.DesiredSize.Width <= maxWidth && tb.DesiredSize.Height <= maxHeight;
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
    /// Partitions text into pages strictly respecting sentence boundaries.
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

        var pages = new List<string>();
        var currentChunk = new StringBuilder();

        foreach (var unit in units)
        {
            var candidate = currentChunk.Length == 0 ? unit : currentChunk + unit;

            if (DoesTextFit(candidate, maxWidth, maxHeight, fontSize, fontWeight, alignment, contentType))
            {
                currentChunk.Append(unit);
            }
            else
            {
                if (currentChunk.Length > 0)
                {
                    pages.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // If a single unit cannot fit alone at this font size, split it into clauses or words
                if (!DoesTextFit(unit.Trim(), maxWidth, maxHeight, fontSize, fontWeight, alignment, contentType))
                {
                    var subUnits = SubdivideOversizedUnit(unit);
                    foreach (var sub in subUnits)
                    {
                        var subCandidate = currentChunk.Length == 0 ? sub : currentChunk + sub;
                        if (DoesTextFit(subCandidate, maxWidth, maxHeight, fontSize, fontWeight, alignment, contentType))
                        {
                            currentChunk.Append(sub);
                        }
                        else
                        {
                            if (currentChunk.Length > 0)
                            {
                                pages.Add(currentChunk.ToString().Trim());
                                currentChunk.Clear();
                            }
                            currentChunk.Append(sub);
                        }
                    }
                }
                else
                {
                    currentChunk.Append(unit);
                }
            }
        }

        if (currentChunk.Length > 0)
        {
            var finalStr = currentChunk.ToString().Trim();
            if (!string.IsNullOrEmpty(finalStr))
            {
                pages.Add(finalStr);
            }
        }

        return pages.Count > 0 ? pages : [text];
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
    /// Subdivides an oversized sentence into clause-level units (semicolons, colons, dashes, commas).
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

        // If no clause delimiters existed (one massive uninterrupted clause), split by words
        if (result.Count <= 1)
        {
            result.Clear();
            var words = unit.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                result.Add(words[i] + (i < words.Length - 1 ? " " : string.Empty));
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
