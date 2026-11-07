using MessageFlow.App.ViewModels;
using MessageFlow.Search;

namespace MessageFlow.Tests;

/// <summary>
/// Bug A (docs/BUGS_UX_2026-11-07.md): after a Bible search replaces the list, the list shows the
/// requested verse; with only a book or a chapter chosen it shows the top of the list.
/// </summary>
public class BibleScrollTargetTests
{
    private static BibleNavigationItemViewModel Verse(int chapter, int verse) =>
        BibleNavigationItemViewModel.ForVerse(new BibleVerseResultViewModel(
            new BibleSearchResult(chapter * 100 + verse, 1, "King James", "KJV", 19, "Psalms", 19, chapter, verse, $"Text {verse}")));

    [Fact]
    public void Requested_verse_is_the_target()
    {
        var verses = Enumerable.Range(1, 40).Select(n => Verse(23, n)).ToList();
        Assert.Same(verses[4], MainViewModel.ChooseBibleScrollTarget(verses, verses[4]));
    }

    [Fact]
    public void Chapter_without_a_verse_goes_to_the_top_of_the_chapter()
    {
        var verses = Enumerable.Range(1, 40).Select(n => Verse(23, n)).ToList();
        Assert.Same(verses[0], MainViewModel.ChooseBibleScrollTarget(verses, null));
    }

    [Fact]
    public void Book_only_goes_to_the_start_of_the_book()
    {
        var books = new List<BibleNavigationItemViewModel>
        {
            BibleNavigationItemViewModel.ForChapter(19, "Psalms", 19, 1, 6),
            BibleNavigationItemViewModel.ForChapter(19, "Psalms", 19, 2, 12),
        };
        Assert.Same(books[0], MainViewModel.ChooseBibleScrollTarget(books, null));
    }

    [Fact]
    public void A_selection_from_the_previous_list_is_ignored()
    {
        var old = Verse(9, 24);
        var fresh = Enumerable.Range(1, 5).Select(n => Verse(23, n)).ToList();
        Assert.Same(fresh[0], MainViewModel.ChooseBibleScrollTarget(fresh, old));
    }

    [Fact]
    public void Empty_list_has_no_target() =>
        Assert.Null(MainViewModel.ChooseBibleScrollTarget([], null));
}
