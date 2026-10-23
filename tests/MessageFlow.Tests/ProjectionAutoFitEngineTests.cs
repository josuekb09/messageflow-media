using System.Text.RegularExpressions;
using MessageFlow.App.Services;
using MessageFlow.App.ViewModels;

namespace MessageFlow.Tests;

public class ProjectionAutoFitEngineTests
{
    private static readonly string LongParagraph = string.Join(" ", Enumerable.Repeat(
        "And the Word was made flesh, and dwelt among us, and we beheld his glory, the glory as of the only begotten of the Father, full of grace and truth.",
        30));

    private static string[] Words(IEnumerable<string> pages) =>
        pages.SelectMany(page => Regex.Split(page.Trim(), @"\s+")).Where(word => word.Length > 0).ToArray();

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    [InlineData(853.3, 480)] // 1280x720 at 150%
    public void A_long_paragraph_is_split_into_screens_holding_exactly_the_original_words(double width, double height)
    {
        Sta.Run(() =>
        {
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Sermon, "Title", "Paragraph 1", LongParagraph, width, height);

            Assert.True(layout.IsPaginated);
            Assert.Equal(Words([LongParagraph]), Words(layout.Pages));
            Assert.True(layout.FontSize >= layout.MinimumReadableFontSize - 0.01, $"{layout.FontSize} < {layout.MinimumReadableFontSize}");
        });
    }

    [Fact]
    public void Splitting_a_sentence_into_words_keeps_the_space_before_the_next_sentence()
    {
        Sta.Run(() =>
        {
            // One run-on sentence without clause marks forces a split by words; its last word must
            // not be glued to the first word of the next sentence ("Amen.And").
            var runOn = string.Join(" ", Enumerable.Repeat("and the word of the Lord came again", 80)) + ". And then it ended.";
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Sermon, "Title", string.Empty, runOn, 1280, 720);

            Assert.Equal(Words([runOn]), Words(layout.Pages));
            Assert.DoesNotContain(layout.Pages, page => page.Contains("again.And", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void A_plus_never_grows_past_the_auto_fit_size_of_a_single_screen()
    {
        Sta.Run(() =>
        {
            const string verse = "For God so loved the world, that he gave his only begotten Son.";
            var auto = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Bible, "John 3:16", string.Empty, verse, 1920, 1080);
            var larger = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Bible, "John 3:16", string.Empty, verse, 1920, 1080, operatorScaleMultiplier: 1.3);
            var smaller = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Bible, "John 3:16", string.Empty, verse, 1920, 1080, operatorScaleMultiplier: 0.95);

            Assert.True(auto.IsAtLargestFit);
            Assert.Equal(auto.FontSize, larger.FontSize);
            Assert.InRange(smaller.FontSize, auto.FontSize * 0.95 - 0.6, auto.FontSize * 0.95 + 0.6);
        });
    }

    [Fact]
    public void A_minus_never_goes_below_the_readable_minimum()
    {
        Sta.Run(() =>
        {
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Bible, "John 11:35", string.Empty, "Jesus wept.", 1280, 720, operatorScaleMultiplier: 0.1);

            Assert.True(layout.FontSize >= layout.MinimumReadableFontSize - 0.01);
        });
    }

    [Fact]
    public void Song_pages_keep_every_source_line_and_add_no_header()
    {
        Sta.Run(() =>
        {
            var lines = Enumerable.Range(1, 40).Select(i => $"Mstari wa wimbo namba {i}").ToArray();
            var section = string.Join("\r\n", lines);
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Song, "YESU RAFIKI", "Couplet - Slide 1", section, 1280, 720, isTitleSlide: true);

            Assert.Equal(0, layout.TitleFontSize);
            Assert.Equal(lines, layout.Pages.SelectMany(page => page.Split('\n')).ToArray());
        });
    }

    [Fact]
    public void Content_without_a_header_or_number_reports_zero_sizes_the_window_must_not_apply()
    {
        Sta.Run(() =>
        {
            // The projection window skips non-positive sizes; applying them threw for every
            // Bible verse and song lyric slide.
            var song = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Song, "Song", "Slide 2", "Line one\nLine two", 1920, 1080);
            var verse = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Bible, "John 3:16", string.Empty, "For God so loved the world.", 1920, 1080);

            Assert.Equal(0, song.NumberFontSize);
            Assert.Equal(0, verse.NumberFontSize);
            Assert.True(verse.TitleFontSize > 0);
        });
    }
}
