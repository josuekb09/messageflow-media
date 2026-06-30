namespace MessageFlow.App.ViewModels;

public sealed class BibleNavigationItemViewModel
{
    private BibleNavigationItemViewModel(
        string itemType,
        int bookId,
        string bookName,
        int bookOrder,
        int? chapter,
        BibleVerseResultViewModel? verse,
        string primaryText,
        string secondaryText,
        string previewText)
    {
        ItemType = itemType;
        BookId = bookId;
        BookName = bookName;
        BookOrder = bookOrder;
        Chapter = chapter;
        Verse = verse;
        PrimaryText = primaryText;
        SecondaryText = secondaryText;
        PreviewText = previewText;
    }

    public string ItemType { get; }

    public int BookId { get; }

    public string BookName { get; }

    public int BookOrder { get; }

    public int? Chapter { get; }

    public BibleVerseResultViewModel? Verse { get; }

    public string PrimaryText { get; }

    public string SecondaryText { get; }

    public string PreviewText { get; }

    public bool IsBook => Verse is null && Chapter is null;

    public bool IsChapter => Verse is null && Chapter is not null;

    public bool IsVerse => Verse is not null;

    public static BibleNavigationItemViewModel ForBook(
        int bookId,
        string bookName,
        string shortName,
        int bookOrder)
    {
        return new BibleNavigationItemViewModel(
            "Book",
            bookId,
            bookName,
            bookOrder,
            null,
            null,
            bookName,
            "Book",
            "Select this book to choose a chapter.");
    }

    public static BibleNavigationItemViewModel ForChapter(
        int bookId,
        string bookName,
        int bookOrder,
        int chapter,
        int verseCount)
    {
        var verseLabel = verseCount == 1 ? "1 verse" : $"{verseCount:N0} verses";
        return new BibleNavigationItemViewModel(
            "Chapter",
            bookId,
            bookName,
            bookOrder,
            chapter,
            null,
            $"{bookName} {chapter}",
            $"Chapter | {verseLabel}",
            "Select this chapter to choose a verse.");
    }

    public static BibleNavigationItemViewModel ForVerse(BibleVerseResultViewModel verse)
    {
        return new BibleNavigationItemViewModel(
            "Verse",
            verse.BookId,
            verse.BookName,
            verse.BookOrder,
            verse.Chapter,
            verse,
            verse.DisplayLine,
            verse.MetaLine,
            verse.PreviewText);
    }
}
