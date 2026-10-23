using System.Xml.Linq;
using MessageFlow.Core.Text;

namespace MessageFlow.Tests;

public class PresentationParagraphTextTests
{
    private const string A = "http://schemas.openxmlformats.org/drawingml/2006/main";

    private static XDocument Slide(string paragraphs) =>
        XDocument.Parse($"<p:sld xmlns:p=\"urn:p\" xmlns:a=\"{A}\"><p:txBody>{paragraphs}</p:txBody></p:sld>");

    [Fact]
    public void Soft_line_breaks_inside_one_paragraph_become_separate_lines()
    {
        // The shape of the Kiswahili hymn that projected as one long line: a whole verse in one
        // <a:p>, lines separated by Shift+Enter (<a:br/>).
        var slide = Slide(
            "<a:p><a:r><a:t>1, yesu rafiki aliye nipenda</a:t></a:r><a:br/>" +
            "<a:r><a:t>Akaniokoa mimi</a:t></a:r><a:br><a:rPr/></a:br>" +
            "<a:r><a:t>Katika musalaba wa Golgotha</a:t></a:r></a:p>");

        var lines = PresentationParagraphText.GetSlideLines(slide);

        Assert.Equal(["1, yesu rafiki aliye nipenda", "Akaniokoa mimi", "Katika musalaba wa Golgotha"], lines);
    }

    [Fact]
    public void Lines_glued_without_a_space_are_split_at_the_break()
    {
        var slide = Slide(
            "<a:p><a:r><a:t>I can hear my Saviour calling,</a:t></a:r><a:br/>" +
            "<a:r><a:t>Take thy cross and follow, follow me.</a:t></a:r></a:p>");

        var lines = PresentationParagraphText.GetSlideLines(slide);

        Assert.Equal(["I can hear my Saviour calling,", "Take thy cross and follow, follow me."], lines);
    }

    [Fact]
    public void Runs_of_one_line_are_joined_and_separate_paragraphs_stay_separate_lines()
    {
        var slide = Slide(
            "<a:p><a:r><a:t>Ama</a:t></a:r><a:r><a:t>zing grace</a:t></a:r></a:p>" +
            "<a:p><a:r><a:t>How sweet</a:t></a:r><a:tab/><a:r><a:t>the sound</a:t></a:r></a:p>" +
            "<a:p></a:p>");

        var lines = PresentationParagraphText.GetSlideLines(slide);

        Assert.Equal(["Amazing grace", "How sweet the sound"], lines);
    }

    [Fact]
    public void A_vertical_tab_inside_the_text_is_a_line_break()
    {
        // A raw vertical tab is not allowed in XML text, so the run is built in memory, the way a
        // reader that decodes "_x000B_" hands it over.
        XNamespace a = A;
        var paragraph = new XElement(a + "p", new XElement(a + "r", new XElement(a + "t", "First line\vSecond line")));

        Assert.Equal(["First line", "Second line"], PresentationParagraphText.GetLines(paragraph));
    }

    [Fact]
    public void A_custom_run_joiner_sees_the_runs_of_each_line()
    {
        var paragraph = Slide("<a:p><a:r><a:t>Bwana</a:t></a:r><a:r><a:t>Yesu</a:t></a:r><a:br/><a:r><a:t>Amina</a:t></a:r></a:p>")
            .Descendants(XName.Get("p", A))
            .Single();

        var lines = PresentationParagraphText.GetLines(paragraph, runs => string.Join(" ", runs));

        Assert.Equal(["Bwana Yesu", "Amina"], lines);
    }
}
