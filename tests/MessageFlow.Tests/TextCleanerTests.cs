using MessageFlow.Importer;

namespace MessageFlow.Tests;

public class TextCleanerTests
{
    [Fact]
    public void Em_dash_at_the_end_of_a_pdf_line_is_kept_between_the_words()
    {
        // The shape that projected "I'mI'm" and "aa kitten": the em dash was turned into "-"
        // and then removed as if it were a word-break hyphen.
        Assert.Equal("that I'm-I'm against", TextCleaner.CleanExtractedText("that I'm—\nI'm against"));
        Assert.Equal("there'd be a-a kitten", TextCleaner.CleanExtractedText("there'd be a— \n a kitten"));
    }

    [Fact]
    public void Word_break_hyphen_at_the_end_of_a_line_still_joins_the_word()
    {
        Assert.Equal("fruitbearing tree", TextCleaner.CleanExtractedText("fruit-\nbearing tree"));
    }

    [Fact]
    public void Em_dash_inside_a_line_is_unchanged()
    {
        Assert.Equal("I-I see", TextCleaner.CleanExtractedText("I—I see"));
    }
}
