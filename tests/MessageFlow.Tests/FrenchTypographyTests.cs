using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MessageFlow.App.Services;
using MessageFlow.App.ViewModels;
using MessageFlow.Core.Text;

namespace MessageFlow.Tests;

/// <summary>
/// French typography at display time: a line never breaks between a word and the following
/// : ; ! ? » or after «. The stored text is not changed; only which spaces may break a line.
/// In the expectations below "~" stands for a no-break space (U+00A0).
/// </summary>
public class FrenchTypographyTests
{
    private const char Nbsp = (char)0xA0;
    private const char NarrowNbsp = (char)0x202F;

    private static string NoBreak(string marked) => marked.Replace('~', Nbsp);

    [Theory]
    [InlineData("Dieu !", "Dieu~!")]
    [InlineData("dire : Seigneur", "dire~: Seigneur")]
    [InlineData("attentivement ; la lire", "attentivement~; la lire")]
    [InlineData("vraiment ?", "vraiment~?")]
    [InlineData("le Seigneur »", "le Seigneur~»")]
    [InlineData("« Seigneur", "«~Seigneur")]
    [InlineData("« Dieu est bon ! »", "«~Dieu est bon~!~»")]
    [InlineData("Quoi ?! Oui", "Quoi~?! Oui")]
    public void The_space_before_a_closing_mark_and_after_an_opening_guillemet_cannot_break(string source, string expected)
    {
        Assert.Equal(NoBreak(expected), FrenchTypography.KeepWithNeighbour(source));
    }

    [Theory]
    [InlineData("Dieu!")]
    [InlineData("«Dieu»")]
    [InlineData("Il dit:oui;non")]
    [InlineData("Seigneur, nous voulons. Amen")]
    public void A_space_that_is_not_in_the_source_is_never_added(string source)
    {
        Assert.Equal(source, FrenchTypography.KeepWithNeighbour(source));
    }

    [Fact]
    public void Only_spaces_change_and_every_other_character_stays()
    {
        const string source = "Frère Branham a dit : « Dieu est fidèle ! » et nous le croyons ; amen ? Oui , bien sûr .";
        var shown = FrenchTypography.KeepWithNeighbour(source);

        Assert.NotEqual(source, shown);
        Assert.Equal(source, shown.Replace(Nbsp, ' '));
    }

    [Fact]
    public void A_no_break_space_already_in_the_source_is_kept_as_it_is()
    {
        var withNbsp = $"RÉVEIL{Nbsp}!";
        var withNarrow = $"RÉVEIL{NarrowNbsp}!";

        Assert.Equal(withNbsp, FrenchTypography.KeepWithNeighbour(withNbsp));
        Assert.Equal(withNarrow, FrenchTypography.KeepWithNeighbour(withNarrow));
    }

    [Fact]
    public void The_result_does_not_change_when_it_is_applied_twice()
    {
        const string source = "« Dieu est bon ! » dit-il : oui ; non ?";
        var once = FrenchTypography.KeepWithNeighbour(source);

        Assert.Equal(once, FrenchTypography.KeepWithNeighbour(once));
    }

    [Fact]
    public void Line_breaks_are_left_alone()
    {
        const string source = "premier\n! deuxième";

        Assert.Equal(source, FrenchTypography.KeepWithNeighbour(source));
    }

    [Fact]
    public void The_operator_text_keeps_the_french_space_as_a_no_break_space()
    {
        // The cleaner used to remove the space before ! ; : ? altogether ("RÉVEIL!").
        Assert.Equal(NoBreak("RÉVEIL~!"), ParagraphDisplayTextCleaner.Clean($"RÉVEIL{Nbsp}!"));
        Assert.Equal(NoBreak("attentivement~; la lire"), ParagraphDisplayTextCleaner.Clean("attentivement ; la lire"));
        Assert.Equal(NoBreak("dire~: Seigneur"), ParagraphDisplayTextCleaner.Clean("dire : Seigneur"));
        Assert.Equal(NoBreak("«~Dieu~»"), ParagraphDisplayTextCleaner.Clean("« Dieu »"));
    }

    [Fact]
    public void The_operator_text_still_removes_the_stray_space_before_a_comma_or_a_period()
    {
        Assert.Equal("mot, suite.", ParagraphDisplayTextCleaner.Clean("mot , suite ."));
    }

    [Fact]
    public void The_operator_preview_keeps_the_no_break_spaces()
    {
        var preview = ParagraphDisplayTextCleaner.CreatePreview("Quel réveil ! Dieu a dit : oui.");

        Assert.Equal(NoBreak("Quel réveil~! Dieu a dit~: oui."), preview);
    }

    [Fact]
    public void Screens_never_start_with_a_mark_or_end_with_an_opening_guillemet_and_keep_the_french_spaces()
    {
        Sta.Run(() =>
        {
            const string phrase = "Frère Branham a dit : « Dieu est fidèle ! » et nous le croyons ; amen ? ";
            var text = string.Concat(Enumerable.Repeat(phrase, 60)).Trim();
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Sermon, "Titre", "Paragraphe 1", text, 1280, 720);

            Assert.True(layout.IsPaginated);
            foreach (var page in layout.Pages)
            {
                Assert.DoesNotContain(page[0], ":;!?»" + Nbsp);
                Assert.NotEqual('«', page[^1]);
                // Every space before a mark or after « is a no-break space on screen.
                Assert.False(Regex.IsMatch(page, @"[ ](?=[:;!?»])"), "a breaking space before a mark");
                Assert.False(Regex.IsMatch(page, @"(?<=«)[ ]"), "a breaking space after «");
            }

            // The pages hold exactly the source words, in order, and the same no-break spaces.
            var expected = FrenchTypography.KeepWithNeighbour(text);
            Assert.Equal(
                Regex.Split(expected, @"[^\S\u00A0\u202F]+").Where(word => word.Length > 0),
                layout.Pages.SelectMany(page => Regex.Split(page, @"[^\S\u00A0\u202F]+")).Where(word => word.Length > 0));
        });
    }

    [Fact]
    public void A_screen_cut_never_separates_a_word_from_the_mark_that_follows_it()
    {
        Sta.Run(() =>
        {
            // One run-on sentence with no clause mark forces the cut to go word by word.
            var runOn = string.Join(" ", Enumerable.Repeat("et la parole du Seigneur vint encore", 90)) + " et voilà !";
            var layout = ProjectionAutoFitEngine.CalculateLayout(ProjectionContentType.Sermon, "Titre", string.Empty, runOn, 1280, 720);

            Assert.True(layout.IsPaginated);
            Assert.DoesNotContain(layout.Pages, page => page.StartsWith('!'));
            Assert.Contains(layout.Pages, page => page.EndsWith(NoBreak("voilà~!"), StringComparison.Ordinal));
        });
    }

    [Fact]
    public void A_line_wraps_a_word_together_with_its_following_mark_when_the_space_is_a_no_break_space()
    {
        Sta.Run(() =>
        {
            // Narrower than "aaaa bbbb !": the word has to move down together with its mark.
            var french = FirstWordOfLastLine("aaaa bbbb" + Nbsp + "!");

            Assert.Equal("bbbb", french);
        });
    }

    private static string FirstWordOfLastLine(string text)
    {
        var probe = new TextBlock { Text = "aaaa bbbb", FontSize = 40, TextWrapping = TextWrapping.NoWrap };
        probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var block = new TextBlock { Text = text, FontSize = 40, TextWrapping = TextWrapping.Wrap, Width = probe.DesiredSize.Width + 4 };
        block.Measure(new Size(block.Width, double.PositiveInfinity));
        block.Arrange(new Rect(new Size(block.Width, block.DesiredSize.Height)));
        block.UpdateLayout();

        var position = block.GetPositionFromPoint(new Point(1, block.ActualHeight - 2), true);
        var lastLine = new TextRange(position, block.ContentEnd).Text.Trim();
        return lastLine.Split(' ', Nbsp)[0];
    }
}
