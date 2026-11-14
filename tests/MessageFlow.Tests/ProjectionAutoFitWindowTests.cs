using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MessageFlow.App;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Tests;

/// <summary>
/// Bug B (docs/BUGS_UX_2026-11-07.md): the Auto fit must keep the text inside the projection stage
/// and centred, on both axes. Unlike ProjectionAutoFitEngineTests, which only ask the engine, this
/// builds the real ProjectWindow, goes through the real Auto path (UpdateProjectionLayout with the
/// font multiplier at 1.0) at the real window size, and measures where WPF actually drew each
/// character of every page. The texts are test constants, never the library.
///
/// Tolerances:
///   inside     : every drawn character within the stage, 0.5 px of rounding allowed;
///   vertical   : |space above the text - space below it| &lt;= 1 px;
///   horizontal : |space left of the widest line - space right of it| &lt;= 2 % of the stage width.
/// </summary>
public sealed class ProjectionAutoFitWindowTests : IDisposable
{
    private const double InsidePixels = 0.5;
    private const double VerticalCentrePixels = 1.0;
    private const double HorizontalCentreFraction = 0.02;

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"mf-tests-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider provider;

    public ProjectionAutoFitWindowTests()
    {
        provider = new ServiceCollection()
            .AddDbContext<MessageFlowDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"))
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        provider.Dispose();
        File.Delete(databasePath);
    }

    private const string LongParagraph =
        "When the morning came the whole village gathered at the edge of the river, because word had gone " +
        "round that the old bridge would finally be opened. The children ran ahead, the elders walked slowly " +
        "behind them, and everyone talked about how long the work had taken and how many hands had helped. " +
        "By noon the first cart crossed, and a quiet cheer went down the bank. Nobody wanted to leave, so " +
        "they sat in the grass, shared what they had brought, and watched the water go by until the evening " +
        "light turned the planks gold and the first lamps were lit along the road.";

    private const string ShortVerse = "A short line of text for the screen.";

    private const string SongSection = "First line of a song section\nSecond line a little longer than the first\nThird line\nFourth line of the section";

    public static TheoryData<string, double, double> Cases()
    {
        var data = new TheoryData<string, double, double>();
        foreach (var (width, height) in new[] { (1920.0, 1080.0), (1280.0, 720.0) })
        {
            foreach (var kind in new[] { "Sermon", "Bible", "Song" })
            {
                data.Add(kind, width, height);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Auto_fit_keeps_text_inside_and_centred_on_both_axes_in_the_real_window(string kind, double windowWidth, double windowHeight)
    {
        Sta.Run(() =>
        {
            using (var scope = provider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>().Database.EnsureCreated();
            }

            var viewModel = new MainViewModel(provider.GetRequiredService<IServiceScopeFactory>());
            var window = new ProjectWindow(viewModel)
            {
                Left = -20000,
                Top = -20000,
                ShowActivated = false,
                Topmost = false
            };
            window.Show();
            var projectionRoot = (Grid)window.FindName("ProjectionRoot");
            // The test machine's monitor is smaller than a 1920x1080 projector, and WPF caps a window
            // at the monitor, so the window's own content tree is hosted in a grid of the exact
            // projector size. Same XAML, same UpdateProjectionLayout, no window re-layout in between.
            window.Content = null;
            var root = new Grid { Width = windowWidth, Height = windowHeight };
            root.Children.Add(projectionRoot);

            var stage = (Grid)window.FindName("ParagraphStage");
            var body = (TextBlock)window.FindName("ParagraphTextBlock");

            var snapshot = kind switch
            {
                "Sermon" => new ProjectedContentSnapshot(ProjectionContentType.Sermon, "Test Sermon Title", string.Empty, LongParagraph),
                "Bible" => new ProjectedContentSnapshot(ProjectionContentType.Bible, "Test Book 1:1", string.Empty, ShortVerse),
                _ => new ProjectedContentSnapshot(ProjectionContentType.Song, "1. TEST SONG", "Verse 1", SongSection),
            };
            typeof(MainViewModel).GetProperty(nameof(MainViewModel.ActiveProjectionContent))!.SetValue(viewModel, null);
            typeof(MainViewModel).GetProperty(nameof(MainViewModel.ActiveProjectionContent))!.SetValue(viewModel, snapshot);

            Assert.Equal(1.0, viewModel.ProjectionFontScaleMultiplier); // Auto, no A+/A- applied.

            var showNextPage = typeof(ProjectWindow).GetMethod("ShowNextProjectionPage", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pagesField = typeof(ProjectWindow).GetField("projectionPages", BindingFlags.Instance | BindingFlags.NonPublic)!;

            // As the fit verifier does: the test machine's monitor may be smaller than the projector,
            // so the window content is laid out at the projector size directly.
            window.UpdateProjectionLayout(windowWidth, windowHeight);
            Arrange(root, windowWidth, windowHeight);

            var pageCount = Math.Max(1, ((List<string>)pagesField.GetValue(window)!).Count);
            var report = new StringBuilder();
            for (var page = 0; page < pageCount; page++)
            {
                if (page > 0)
                {
                    showNextPage.Invoke(window, null);
                    Arrange(root, windowWidth, windowHeight);
                }

                var ink = MeasureInk(body, stage);
                var stageWidth = stage.ActualWidth;
                var stageHeight = stage.ActualHeight;
                var left = ink.Left;
                var right = stageWidth - ink.Right;
                var top = ink.Top;
                var bottom = stageHeight - ink.Bottom;

                var inside = left >= -InsidePixels && right >= -InsidePixels && top >= -InsidePixels && bottom >= -InsidePixels;
                var verticalCentred = Math.Abs(top - bottom) <= VerticalCentrePixels;
                var horizontalCentred = Math.Abs(left - right) <= HorizontalCentreFraction * stageWidth;

                if (!inside || !verticalCentred || !horizontalCentred)
                {
                    report.AppendLine(
                        $"[{kind} {windowWidth:0}x{windowHeight:0}] page {page + 1}/{pageCount}: " +
                        $"text {ink.Width:0.#} x {ink.Height:0.#} in stage {stageWidth:0.#} x {stageHeight:0.#}; " +
                        $"margins left {left:0.#}, right {right:0.#}, top {top:0.#}, bottom {bottom:0.#}; " +
                        $"inside={inside}, vertical centred={verticalCentred} (|top-bottom|={Math.Abs(top - bottom):0.#} px, max {VerticalCentrePixels}), " +
                        $"horizontal centred={horizontalCentred} (|left-right|={Math.Abs(left - right):0.#} px, max {HorizontalCentreFraction * stageWidth:0.#}); " +
                        $"font {body.FontSize:0.#} px, align {body.TextAlignment}");
                }
            }

            window.Close();
            Assert.True(report.Length == 0, Environment.NewLine + report);
        });
    }

    private static void Arrange(FrameworkElement root, double width, double height)
    {
        // Force a full pass: a page change only dirties the text block, and a detached root would
        // otherwise keep the previous page's arrangement.
        root.InvalidateMeasure();
        root.Measure(new Size(width, height));
        root.InvalidateArrange();
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
    }

    /// <summary>Bounding box, in stage coordinates, of every non-blank character WPF drew in the text block.</summary>
    private static Rect MeasureInk(TextBlock body, FrameworkElement stage)
    {
        var toStage = body.TransformToAncestor(stage);
        var ink = Rect.Empty;
        var pointer = body.ContentStart;
        while (pointer is not null && pointer.CompareTo(body.ContentEnd) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var text = pointer.GetTextInRun(LogicalDirection.Forward);
                if (text.Length > 0 && !char.IsWhiteSpace(text[0]))
                {
                    var rect = pointer.GetCharacterRect(LogicalDirection.Forward);
                    if (!rect.IsEmpty)
                    {
                        // The rect of a character is only its left edge (zero width), so the glyph's right
                        // edge is the left edge of the caret placed right after it, on the same line.
                        var after = pointer.GetNextInsertionPosition(LogicalDirection.Forward);
                        var end = after?.GetCharacterRect(LogicalDirection.Backward) ?? Rect.Empty;
                        if (!end.IsEmpty && Math.Abs(end.Top - rect.Top) < rect.Height / 2)
                        {
                            rect.Union(end);
                        }

                        ink.Union(toStage.TransformBounds(rect));
                    }
                }
            }

            pointer = pointer.GetNextInsertionPosition(LogicalDirection.Forward);
        }

        Assert.False(ink.IsEmpty, "The text block drew no characters.");
        return ink;
    }
}
