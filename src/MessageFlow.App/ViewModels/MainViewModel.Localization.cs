using MessageFlow.App.Localization;
using MessageFlow.Core.Localization;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.App.ViewModels;

public sealed partial class MainViewModel
{
    private AppLanguage selectedUiLanguage = AppLanguages.Default;
    private bool suppressUiLanguageSave;
    private bool hasSermonContentForCurrentLanguage = true;
    private bool hasSongContentForCurrentLanguage = true;

    public IReadOnlyList<AppLanguage> AvailableUiLanguages => AppLanguages.All;

    public AppLanguage SelectedUiLanguage
    {
        get => selectedUiLanguage;
        set
        {
            if (selectedUiLanguage == value)
            {
                return;
            }

            selectedUiLanguage = value;
            OnPropertyChanged();
            if (!suppressUiLanguageSave)
            {
                UiLanguagePreference.Save(value);
                Localizer.Instance.SetLanguage(value);
                _ = ApplyContentLanguageAsync();
            }
        }
    }

    public string ContentLanguageCode => SelectedUiLanguage.ContentLanguageCode;

    public bool HasSermonContentForCurrentLanguage
    {
        get => hasSermonContentForCurrentLanguage;
        private set
        {
            if (SetProperty(ref hasSermonContentForCurrentLanguage, value))
            {
                OnPropertyChanged(nameof(SermonEmptyStateTitle));
                OnPropertyChanged(nameof(SermonEmptyStateDetail));
            }
        }
    }

    public bool HasSongContentForCurrentLanguage
    {
        get => hasSongContentForCurrentLanguage;
        private set
        {
            if (SetProperty(ref hasSongContentForCurrentLanguage, value))
            {
                OnPropertyChanged(nameof(SongEmptyStateTitle));
                OnPropertyChanged(nameof(SongEmptyStateDetail));
            }
        }
    }

    public string SermonEmptyStateTitle =>
        HasSermonContentForCurrentLanguage
            ? Loc.T("Sermon_ReadyToSearch")
            : Loc.T("Sermon_NoFrenchContent");

    public string SermonEmptyStateDetail =>
        HasSermonContentForCurrentLanguage
            ? Loc.T("Sermon_SearchHint")
            : Loc.T("Sermon_NoFrenchContentDetail");

    public string SongEmptyStateTitle =>
        HasSongContentForCurrentLanguage
            ? Loc.T("Song_ReadyToSearch")
            : Loc.T("Song_NoFrenchContent");

    public string SongEmptyStateDetail =>
        HasSongContentForCurrentLanguage
            ? Loc.T("Song_SearchHintShort")
            : Loc.T("Song_NoFrenchContentDetail");

    private void InitializeUiLanguage()
    {
        suppressUiLanguageSave = true;
        try
        {
            selectedUiLanguage = Localizer.Instance.CurrentLanguage;
            OnPropertyChanged(nameof(SelectedUiLanguage));
        }
        finally
        {
            suppressUiLanguageSave = false;
        }

        RebuildProjectionFontSizes();
        Localizer.Instance.LanguageChanged += (_, _) => RefreshLocalizedProperties();
    }

    private async Task ApplyContentLanguageAsync()
    {
        if (IsSermonReadingMode)
        {
            IsSermonReadingMode = false;
            focusedSermonId = null;
            focusedSermon = null;
            focusedSermonParagraphs = [];
        }

        ClearSelectedSong();
        selectedSermon = null;
        OnPropertyChanged(nameof(SelectedSermon));
        selectedParagraph = null;
        OnPropertyChanged(nameof(SelectedParagraph));
        SelectedBibleVerse = null;
        SelectedBibleNavigationItem = null;
        SetResults([], isSermonBrowseMode: false);
        SongResults.Clear();
        SongSections.Clear();
        RebuildProjectionFontSizes();
        RefreshProjectionDisplayOptions();
        RefreshLocalizedProperties();

        await RefreshContentAvailabilityAsync();
        await RefreshFilterOptionsPreservingSelectionAsync();
        await LoadFavoritesAsync();
        await LoadProjectionHistoryAsync();
        await LoadBibleTranslationsAsync();

        if (IsBibleMode)
        {
            QueueBibleSearch();
        }
        else if (IsSongsMode)
        {
            QueueSongSearch();
        }
        else
        {
            QueueSearch();
        }

        StatusText = SelectedUiLanguage == AppLanguages.French
            ? Loc.T("Lang_ChangedToFrench")
            : Loc.T("Lang_ChangedToEnglish");
    }

    private async Task RefreshContentAvailabilityAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var language = ContentLanguageCode;

        HasSermonContentForCurrentLanguage = await dbContext.Sermons
            .AsNoTracking()
            .AnyAsync(sermon => sermon.Language == language);

        HasSongContentForCurrentLanguage = await dbContext.Songs
            .AsNoTracking()
            .AnyAsync(song => song.IsActive && song.Language == language);
    }

    private void RebuildProjectionFontSizes()
    {
        var selectedSize = SelectedProjectionFontSize?.FontSize;
        ProjectionFontSizes.Clear();
        ProjectionFontSizes.Add(new ProjectionFontSizeOption(Loc.T("Font_Small"), 48, 60));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption(Loc.T("Font_Medium"), 62, 76));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption(Loc.T("Font_Large"), 76, 92));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption(Loc.T("Font_ExtraLarge"), 90, 108));
        SelectedProjectionFontSize = ProjectionFontSizes.FirstOrDefault(option =>
                                         selectedSize is not null &&
                                         Math.Abs(option.FontSize - selectedSize.Value) < 0.1) ??
                                     ProjectionFontSizes.FirstOrDefault(option => Math.Abs(option.FontSize - 62) < 0.1) ??
                                     ProjectionFontSizes.FirstOrDefault();
    }

    private void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(SermonReadingHeader));
        OnPropertyChanged(nameof(CenterPanelTitle));
        OnPropertyChanged(nameof(RightPanelTitle));
        OnPropertyChanged(nameof(LibraryCountText));
        OnPropertyChanged(nameof(PreviewHeader));
        OnPropertyChanged(nameof(PreviewMeta));
        OnPropertyChanged(nameof(SelectedParagraphHeader));
        OnPropertyChanged(nameof(SelectedParagraphMeta));
        OnPropertyChanged(nameof(FavoriteButtonText));
        OnPropertyChanged(nameof(PreviousButtonText));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(CurrentBibleTranslationDisplay));
        OnPropertyChanged(nameof(SelectedBibleVersionShortDisplay));
        OnPropertyChanged(nameof(CurrentBibleVerseCountDisplay));
        OnPropertyChanged(nameof(ProjectionStatusText));
        OnPropertyChanged(nameof(SermonEmptyStateTitle));
        OnPropertyChanged(nameof(SermonEmptyStateDetail));
        OnPropertyChanged(nameof(SongEmptyStateTitle));
        OnPropertyChanged(nameof(SongEmptyStateDetail));
    }
}
