using MessageFlow.Core.Localization;

namespace MessageFlow.App.ViewModels;

public sealed class BibleFavoriteVerseViewModel
{
    public BibleFavoriteVerseViewModel(
        int id,
        int verseId,
        int translationId,
        string translationName,
        string translationAbbreviation,
        int bookId,
        string bookName,
        int bookOrder,
        int chapter,
        int verse,
        string text,
        DateTime createdAt)
    {
        Id = id;
        VerseId = verseId;
        TranslationId = translationId;
        TranslationName = translationName;
        TranslationAbbreviation = translationAbbreviation;
        BookId = bookId;
        BookName = bookName;
        BookOrder = bookOrder;
        Chapter = chapter;
        Verse = verse;
        Text = text;
        CreatedAt = createdAt;
    }

    public int Id { get; }

    public int VerseId { get; }

    public int TranslationId { get; }

    public string TranslationName { get; }

    public string TranslationAbbreviation { get; }

    public int BookId { get; }

    public string BookName { get; }

    public int BookOrder { get; }

    public int Chapter { get; }

    public int Verse { get; }

    public string Text { get; }

    public DateTime CreatedAt { get; }

    public string DisplayBookName => Localizer.Instance.BookName(BookName);

    public string ReferenceDisplay => Localizer.Instance.BookReference(BookName, Chapter, Verse);

    public string DisplayLine => $"{ReferenceDisplay} ({TranslationAbbreviation})";

    public string MetaLine => TranslationName;

    public string PreviewText => Text.Length <= 150 ? Text : $"{Text[..150]}...";

    public string SavedAtText => Localizer.Instance.Format("Fav_SavedAt", CreatedAt.ToLocalTime().ToString("g"));
}
