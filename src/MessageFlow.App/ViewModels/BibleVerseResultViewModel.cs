using MessageFlow.Search;

namespace MessageFlow.App.ViewModels;

public sealed class BibleVerseResultViewModel
{
    public BibleVerseResultViewModel(BibleSearchResult result)
    {
        VerseId = result.VerseId;
        TranslationId = result.TranslationId;
        TranslationName = result.TranslationName;
        TranslationAbbreviation = result.TranslationAbbreviation;
        BookId = result.BookId;
        BookName = result.BookName;
        BookOrder = result.BookOrder;
        Chapter = result.Chapter;
        Verse = result.Verse;
        Text = result.Text;
    }

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

    public string ReferenceDisplay => $"{BookName} {Chapter}:{Verse}";

    public string DisplayLine => $"{ReferenceDisplay} ({TranslationAbbreviation})";

    public string MetaLine => $"{TranslationName} | {TranslationAbbreviation}";

    public string PreviewText => Text.Length <= 140 ? Text : $"{Text[..140]}...";
}
