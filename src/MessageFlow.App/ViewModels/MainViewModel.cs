using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using MessageFlow.App.Localization;
using MessageFlow.Core.Localization;
using MessageFlow.App.Infrastructure;
using MessageFlow.Core.Bible;
using MessageFlow.Core.ContentSources;
using MessageFlow.Core.Sermons;
using MessageFlow.Data;
using MessageFlow.Importer;
using MessageFlow.Search;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace MessageFlow.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const int SearchDebounceMilliseconds = 250;
    /// <summary>
    /// Brother Frank sermons stay hidden from the church UI until official PDFs are imported.
    /// Records remain in the database; admin import and cleanup tools stay available.
    /// Flip this flag after the official Frank library is loaded.
    /// </summary>
    private static readonly bool ShowBrotherFrankLibrary = false;
    private const double ProjectionFontAdjustmentStep = 2;
    private const double MinimumProjectionFontAdjustment = -24;
    private const double MaximumProjectionFontAdjustment = 24;
    private const double MinimumSermonProjectionFontAdjustment = -12;
    private const double MaximumSermonProjectionFontAdjustment = 10;
    private readonly IServiceScopeFactory scopeFactory;
    private CancellationTokenSource? searchDebounce;
    private CancellationTokenSource? bibleSearchDebounce;
    private CancellationTokenSource? songSearchDebounce;
    private CancellationTokenSource? sermonWithinSearchDebounce;
    private int searchRequestVersion;
    private int bibleSearchRequestVersion;
    private int songSearchRequestVersion;
    private int sermonWithinSearchRequestVersion;
    private int sourceDetailsRequestVersion;
    private string searchText = string.Empty;
    private FilterOption? selectedAuthor;
    private FilterOption? selectedSourceFilter;
    private FilterOption? selectedYear;
    private SermonResultViewModel? selectedSermon;
    private ParagraphResultViewModel? selectedParagraph;
    private SongResultViewModel? selectedSong;
    private SongSectionViewModel? selectedSongSection;
    private SavedParagraphViewModel? selectedFavoriteParagraph;
    private BibleFavoriteVerseViewModel? selectedBibleFavoriteVerse;
    private SavedParagraphViewModel? selectedHistoryParagraph;
    private ContentSourceViewModel? selectedContentSource;
    private ProjectionDisplayOption? selectedProjectionDisplayOption;
    private BibleTranslationOption? selectedBibleTranslation;
    private BibleVerseResultViewModel? selectedBibleVerse;
    private BibleNavigationItemViewModel? selectedBibleNavigationItem;
    private SourceDiagnosticsViewModel selectedSourceDetails = SourceDiagnosticsViewModel.None;
    private ProjectionFontSizeOption? selectedProjectionFontSize;
    private double projectionFontSizeAdjustment;
    private bool projectionFontSizeUsesSafeFit = true;
    private int projectionPageIndex;
    private int projectionPageCount;
    private ProjectedContentSnapshot? activeProjectionContent;
    private string bibleSearchText = string.Empty;
    private string songSearchText = string.Empty;
    private string sermonWithinSearchText = string.Empty;
    private string sermonWithinSearchStatus = Loc.T("Sermon_FindInSelected");
    private string statusText = Loc.T("Status_Ready");
    private string? latestBackupPath;
    private int currentBibleVerseCount;
    private bool isProjectionOpen;
    private bool isSearching;
    private bool isDatabaseOperationRunning;
    private bool isBibleAvailable;
    private bool isBibleMode;
    private bool isSongsMode;
    private bool isSermonWorkspaceActive = true;
    private bool selectedBibleVerseIsFavorite;
    private bool showTestSourcesInManageSources;
    private bool suppressBibleSearchQueue;
    private bool isSermonBrowseMode;
    private bool isSermonReadingMode;
    private int? focusedSermonId;
    private SermonResultViewModel? focusedSermon;
    private bool suppressProjectionDisplayPreferenceSave;
    private int resultCount;
    private List<ParagraphResultViewModel> allParagraphResults = [];
    private List<ParagraphResultViewModel> focusedSermonParagraphs = [];
    private List<ParagraphResultViewModel> sermonWithinMatches = [];
    private string sermonWithinSearchAppliedText = string.Empty;
    private int? sermonWithinSearchAppliedSermonId;
    private int sermonWithinMatchIndex = -1;
    private string projectionOpenDisplayText = "Primary Display";

    internal IServiceScopeFactory ScopeFactory => scopeFactory;

    public MainViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;

        AuthorFilters.Add(new FilterOption(null, Loc.T("Filter_AllAuthors")));
        SourceFilters.Add(new FilterOption(null, Loc.T("Filter_AllSources")));
        YearFilters.Add(new FilterOption(null, Loc.T("Filter_AllYears")));
        selectedAuthor = AuthorFilters[0];
        selectedSourceFilter = SourceFilters[0];
        selectedYear = YearFilters[0];

        PreviousParagraphCommand = new RelayCommand(SelectPreviousParagraph, CanNavigatePreviousCurrentSelection);
        NextParagraphCommand = new RelayCommand(SelectNextParagraph, CanNavigateNextCurrentSelection);
        CopyCommand = new RelayCommand(CopySelectedParagraph, CanUseCurrentSelection);
        ProjectCommand = new RelayCommand(ProjectSelectedParagraph, CanUseCurrentSelection);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, CanUseCurrentSelection);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        BibleSearchCommand = new RelayCommand(
            () => _ = SearchBibleAsync(),
            () => IsBibleAvailable && !IsSearching);
        ProjectFavoriteCommand = new RelayCommand<SavedParagraphViewModel>(
            item => _ = ProjectSavedParagraphAsync(item),
            item => item is not null);
        RemoveFavoriteCommand = new RelayCommand<SavedParagraphViewModel>(
            item => _ = RemoveSavedFavoriteAsync(item),
            item => item is not null && !IsDatabaseOperationRunning);
        ProjectBibleFavoriteCommand = new RelayCommand<BibleFavoriteVerseViewModel>(
            item => _ = ProjectBibleFavoriteAsync(item),
            item => item is not null);
        CopyBibleFavoriteCommand = new RelayCommand<BibleFavoriteVerseViewModel>(
            CopyBibleFavorite,
            item => item is not null);
        RemoveBibleFavoriteCommand = new RelayCommand<BibleFavoriteVerseViewModel>(
            item => _ = RemoveBibleFavoriteAsync(item),
            item => item is not null && !IsDatabaseOperationRunning);
        ProjectHistoryCommand = new RelayCommand<SavedParagraphViewModel>(
            item => _ = ProjectSavedParagraphAsync(item),
            item => item is not null);
        BackupDatabaseCommand = new RelayCommand(
            () => _ = BackupDatabaseAsync(),
            () => !IsDatabaseOperationRunning);
        RestoreDatabaseCommand = new RelayCommand(
            () => _ = RestoreDatabaseAsync(),
            () => !IsDatabaseOperationRunning);
        OpenBackupFolderCommand = new RelayCommand(
            OpenLatestBackupFolder,
            CanOpenLatestBackupFolder);
        AddNewSourceCommand = new RelayCommand(
            () => _ = AddNewSourceAsync(),
            () => !IsDatabaseOperationRunning);
        ManageSourcesCommand = new RelayCommand(
            ShowManageSources,
            () => !IsDatabaseOperationRunning);
        ImportSourceCommand = new RelayCommand(
            () => _ = ImportSelectedSourceAsync(),
            () => SelectedContentSource is not null && !IsDatabaseOperationRunning);
        RepairSourceMetadataCommand = new RelayCommand(
            () => _ = RepairSelectedSourceMetadataAsync(),
            () => SelectedContentSource is not null && !IsDatabaseOperationRunning);
        RebuildSermonSearchIndexCommand = new RelayCommand(
            () => _ = RebuildSermonSearchIndexAsync(),
            () => !IsDatabaseOperationRunning);
        ImportBibleCommand = new RelayCommand(
            () => _ = ImportBibleAsync(),
            () => !IsDatabaseOperationRunning);
        ClearHistoryCommand = new RelayCommand(
            () => _ = ClearHistoryAsync(),
            () => !IsDatabaseOperationRunning && ProjectionHistoryItems.Count > 0);
        VerifyProductionDataCommand = new RelayCommand(
            () => _ = VerifyProductionDataAsync(),
            () => !IsDatabaseOperationRunning);
        CleanupTestDataCommand = new RelayCommand(
            () => _ = CleanupTestDataAsync(),
            () => !IsDatabaseOperationRunning);
        CleanupBrotherFrankCircularLettersCommand = new RelayCommand(
            () => _ = CleanupBrotherFrankCircularLettersAsync(),
            () => !IsDatabaseOperationRunning);
        TestProjectionDisplayCommand = new RelayCommand(
            RequestProjectionDisplayTest,
            () => !IsDatabaseOperationRunning);
        OpenProjectionPreviewCommand = new RelayCommand(
            RequestProjectionPreview,
            CanUseCurrentSelection);
        RefreshProjectionDisplaysCommand = new RelayCommand(RefreshProjectionDisplayOptions);
        DecreaseProjectionTextSizeCommand = new RelayCommand(
            DecreaseProjectionTextSize,
            () => projectionFontSizeAdjustment > GetMinimumProjectionFontAdjustment());
        IncreaseProjectionTextSizeCommand = new RelayCommand(
            IncreaseProjectionTextSize,
            () => projectionFontSizeAdjustment < GetMaximumProjectionFontAdjustment());
        ResetProjectionTextSizeCommand = new RelayCommand(ResetProjectionTextSize);
        PreviousProjectionPageCommand = new RelayCommand(
            RequestPreviousProjectionPage,
            () => HasProjectionPages && projectionPageIndex > 0);
        NextProjectionPageCommand = new RelayCommand(
            RequestNextProjectionPage,
            () => HasProjectionPages && projectionPageIndex < projectionPageCount - 1);
        PreviousSermonWithinMatchCommand = new RelayCommand(
            () => _ = SelectPreviousSermonWithinMatchAsync(),
            CanNavigateSermonWithinMatches);
        NextSermonWithinMatchCommand = new RelayCommand(
            () => _ = SelectNextSermonWithinMatchAsync(),
            CanNavigateSermonWithinMatches);
        ClearSermonWithinSearchCommand = new RelayCommand(ClearSermonWithinSearch);
        OpenSermonCommand = new RelayCommand<SermonResultViewModel>(
            sermon => _ = OpenSermonAsync(sermon),
            sermon => sermon is not null && !IsSermonReadingMode);
        BackToSermonSearchResultsCommand = new RelayCommand(
            BackToSermonSearchResults,
            () => IsSermonReadingMode);

        InitializeUiLanguage();
        InitializeUiTheme();
        RefreshProjectionDisplayOptions();

        ParagraphResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsParagraphResultsEmpty));
        FavoriteParagraphs.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsFavoritesEmpty));
            OnPropertyChanged(nameof(HasFavorites));
            OnPropertyChanged(nameof(IsSermonFavoritesEmpty));
        };
        BibleFavoriteVerses.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsFavoritesEmpty));
            OnPropertyChanged(nameof(HasFavorites));
            OnPropertyChanged(nameof(IsBibleFavoritesEmpty));
        };
        ProjectionHistoryItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsProjectionHistoryEmpty));
            ClearHistoryCommand.RaiseCanExecuteChanged();
        };
        BibleResults.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(LibraryCountText));
        };
        BibleTranslations.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSingleBibleTranslation));
            OnPropertyChanged(nameof(HasMultipleBibleTranslations));
            OnPropertyChanged(nameof(SelectedBibleVersionShortDisplay));
        };
        BibleNavigationItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsBibleResultsEmpty));
            OnPropertyChanged(nameof(LibraryCountText));
        };
        SongResults.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsSongResultsEmpty));
            OnPropertyChanged(nameof(LibraryCountText));
        };
        SongSections.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsSongSectionsEmpty));
        };
    }

    public event Action? ProjectRequested;

    public event Action? ProjectionTestRequested;

    public event Action? ProjectionPreviewRequested;

    public event Action? PreviousProjectionPageRequested;

    public event Action? NextProjectionPageRequested;

    public ObservableCollection<FilterOption> AuthorFilters { get; } = [];

    public ObservableCollection<FilterOption> SourceFilters { get; } = [];

    public ObservableCollection<FilterOption> YearFilters { get; } = [];

    public ObservableCollection<SermonResultViewModel> SermonResults { get; } = [];

    public ObservableCollection<ParagraphResultViewModel> ParagraphResults { get; } = [];

    public ObservableCollection<SavedParagraphViewModel> FavoriteParagraphs { get; } = [];

    public ObservableCollection<BibleFavoriteVerseViewModel> BibleFavoriteVerses { get; } = [];

    public ObservableCollection<SavedParagraphViewModel> ProjectionHistoryItems { get; } = [];

    public ObservableCollection<ContentSourceViewModel> ContentSources { get; } = [];

    public ObservableCollection<ContentSourceViewModel> ManageableContentSources { get; } = [];

    public ObservableCollection<ProjectionDisplayOption> ProjectionDisplayOptions { get; } = [];

    public ObservableCollection<BibleTranslationOption> BibleTranslations { get; } = [];

    public ObservableCollection<BibleVerseResultViewModel> BibleResults { get; } = [];

    public ObservableCollection<BibleNavigationItemViewModel> BibleNavigationItems { get; } = [];

    public ObservableCollection<SongResultViewModel> SongResults { get; } = [];

    public ObservableCollection<SongSectionViewModel> SongSections { get; } = [];

    public ObservableCollection<ProjectionFontSizeOption> ProjectionFontSizes { get; } = [];

    public RelayCommand PreviousParagraphCommand { get; }

    public RelayCommand NextParagraphCommand { get; }

    public RelayCommand CopyCommand { get; }

    public RelayCommand ProjectCommand { get; }

    public RelayCommand ToggleFavoriteCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public RelayCommand BibleSearchCommand { get; }

    public RelayCommand<SavedParagraphViewModel> ProjectFavoriteCommand { get; }

    public RelayCommand<SavedParagraphViewModel> RemoveFavoriteCommand { get; }

    public RelayCommand<BibleFavoriteVerseViewModel> ProjectBibleFavoriteCommand { get; }

    public RelayCommand<BibleFavoriteVerseViewModel> CopyBibleFavoriteCommand { get; }

    public RelayCommand<BibleFavoriteVerseViewModel> RemoveBibleFavoriteCommand { get; }

    public RelayCommand<SavedParagraphViewModel> ProjectHistoryCommand { get; }

    public RelayCommand BackupDatabaseCommand { get; }

    public RelayCommand RestoreDatabaseCommand { get; }

    public RelayCommand OpenBackupFolderCommand { get; }

    public RelayCommand AddNewSourceCommand { get; }

    public RelayCommand ManageSourcesCommand { get; }

    public RelayCommand ImportSourceCommand { get; }

    public RelayCommand RepairSourceMetadataCommand { get; }

    public RelayCommand RebuildSermonSearchIndexCommand { get; }

    public RelayCommand ImportBibleCommand { get; }

    public RelayCommand ClearHistoryCommand { get; }

    public RelayCommand VerifyProductionDataCommand { get; }

    public RelayCommand CleanupTestDataCommand { get; }

    public RelayCommand CleanupBrotherFrankCircularLettersCommand { get; }

    public RelayCommand TestProjectionDisplayCommand { get; }

    public RelayCommand OpenProjectionPreviewCommand { get; }

    public RelayCommand RefreshProjectionDisplaysCommand { get; }

    public RelayCommand DecreaseProjectionTextSizeCommand { get; }

    public RelayCommand IncreaseProjectionTextSizeCommand { get; }

    public RelayCommand ResetProjectionTextSizeCommand { get; }

    public RelayCommand PreviousProjectionPageCommand { get; }

    public RelayCommand NextProjectionPageCommand { get; }

    public RelayCommand PreviousSermonWithinMatchCommand { get; }

    public RelayCommand NextSermonWithinMatchCommand { get; }

    public RelayCommand ClearSermonWithinSearchCommand { get; }

    public RelayCommand<SermonResultViewModel> OpenSermonCommand { get; }

    public RelayCommand BackToSermonSearchResultsCommand { get; }

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                OnPropertyChanged(nameof(SermonHighlightQuery));
                OnPropertyChanged(nameof(PreviewHighlightQuery));
                ExitSermonReadingModeForGlobalSearch();
                QueueSearch();
            }
        }
    }

    public FilterOption? SelectedAuthor
    {
        get => selectedAuthor;
        set
        {
            if (SetProperty(ref selectedAuthor, value))
            {
                ExitSermonReadingModeForGlobalSearch();
                QueueSearch();
            }
        }
    }

    public FilterOption? SelectedSourceFilter
    {
        get => selectedSourceFilter;
        set
        {
            if (SetProperty(ref selectedSourceFilter, value))
            {
                OnPropertyChanged(nameof(CenterPanelTitle));
                ExitSermonReadingModeForGlobalSearch();
                QueueSearch();
            }
        }
    }

    public FilterOption? SelectedYear
    {
        get => selectedYear;
        set
        {
            if (SetProperty(ref selectedYear, value))
            {
                ExitSermonReadingModeForGlobalSearch();
                QueueSearch();
            }
        }
    }

    public SermonResultViewModel? SelectedSermon
    {
        get => selectedSermon;
        set
        {
            if (SetProperty(ref selectedSermon, value))
            {
                if (!isSermonReadingMode)
                {
                    ClearSermonWithinSearch(resetText: true);
                    RefreshParagraphResultsForSelectedSermon();
                }
            }
        }
    }

    public string SermonWithinSearchText
    {
        get => sermonWithinSearchText;
        set
        {
            if (SetProperty(ref sermonWithinSearchText, value))
            {
                OnPropertyChanged(nameof(SermonHighlightQuery));
                OnPropertyChanged(nameof(PreviewHighlightQuery));
                QueueSermonWithinSearch();
            }
        }
    }

    public string SermonWithinSearchStatus
    {
        get => sermonWithinSearchStatus;
        private set => SetProperty(ref sermonWithinSearchStatus, value);
    }

    public ParagraphResultViewModel? SelectedParagraph
    {
        get => selectedParagraph;
        set
        {
            if (SetProperty(ref selectedParagraph, value))
            {
                if (value is not null && selectedBibleVerse is not null)
                {
                    selectedBibleVerse = null;
                    selectedBibleVerseIsFavorite = false;
                    OnPropertyChanged(nameof(SelectedBibleVerse));
                }

                if (value is not null)
                {
                    ClearSelectedSong();
                }

                if (value is not null)
                {
                    IsBibleMode = false;
                    IsSongsMode = false;
                }

                OnPropertyChanged(nameof(SelectedParagraphHeader));
                OnPropertyChanged(nameof(SelectedParagraphMeta));
                OnPropertyChanged(nameof(PreviewHeader));
                OnPropertyChanged(nameof(PreviewMeta));
                OnPropertyChanged(nameof(PreviewText));
                OnPropertyChanged(nameof(ProjectionParagraphTitle));
                OnPropertyChanged(nameof(ProjectionParagraphNumber));
                OnPropertyChanged(nameof(SelectedParagraphText));
                OnPropertyChanged(nameof(FavoriteButtonText));
                _ = RefreshSelectedFavoriteStateAsync(value?.ParagraphId);

                RaiseCommandStates();
            }
        }
    }

    public SavedParagraphViewModel? SelectedFavoriteParagraph
    {
        get => selectedFavoriteParagraph;
        set
        {
            if (SetProperty(ref selectedFavoriteParagraph, value) && value is not null)
            {
                _ = SelectSavedParagraphAsync(value.ParagraphId);
            }
        }
    }

    public BibleFavoriteVerseViewModel? SelectedBibleFavoriteVerse
    {
        get => selectedBibleFavoriteVerse;
        set
        {
            if (SetProperty(ref selectedBibleFavoriteVerse, value) && value is not null)
            {
                SelectBibleFavorite(value);
            }
        }
    }

    public SavedParagraphViewModel? SelectedHistoryParagraph
    {
        get => selectedHistoryParagraph;
        set
        {
            if (SetProperty(ref selectedHistoryParagraph, value) && value is not null)
            {
                _ = SelectSavedParagraphAsync(value.ParagraphId);
            }
        }
    }

    public ContentSourceViewModel? SelectedContentSource
    {
        get => selectedContentSource;
        set
        {
            if (SetProperty(ref selectedContentSource, value))
            {
                ImportSourceCommand.RaiseCanExecuteChanged();
                RepairSourceMetadataCommand.RaiseCanExecuteChanged();
                _ = LoadSelectedSourceDiagnosticsAsync(value);
            }
        }
    }

    public ProjectionDisplayOption? SelectedProjectionDisplayOption
    {
        get => selectedProjectionDisplayOption;
        set
        {
            if (SetProperty(ref selectedProjectionDisplayOption, value))
            {
                if (!suppressProjectionDisplayPreferenceSave && value is not null)
                {
                    ProjectionDisplayService.SavePreference(value.PreferenceKey);
                    StatusText = $"Projection display set to {value.Label}.";
                }
            }
        }
    }

    public BibleTranslationOption? SelectedBibleTranslation
    {
        get => selectedBibleTranslation;
        set
        {
            if (SetProperty(ref selectedBibleTranslation, value))
            {
                OnPropertyChanged(nameof(CurrentBibleTranslationDisplay));
                OnPropertyChanged(nameof(CurrentBibleVerseCountDisplay));
                OnPropertyChanged(nameof(SelectedBibleVersionShortDisplay));
                QueueBibleSearch();
            }
        }
    }

    public BibleVerseResultViewModel? SelectedBibleVerse
    {
        get => selectedBibleVerse;
        set
        {
            if (SetProperty(ref selectedBibleVerse, value))
            {
                if (value is not null && selectedParagraph is not null && !isSermonReadingMode)
                {
                    selectedParagraph = null;
                    OnPropertyChanged(nameof(SelectedParagraph));
                }

                if (value is not null)
                {
                    ClearSelectedSong();
                    IsBibleMode = true;
                }

                OnPropertyChanged(nameof(SelectedParagraphHeader));
                OnPropertyChanged(nameof(SelectedParagraphMeta));
                OnPropertyChanged(nameof(PreviewHeader));
                OnPropertyChanged(nameof(PreviewMeta));
                OnPropertyChanged(nameof(PreviewText));
                OnPropertyChanged(nameof(ProjectionParagraphTitle));
                OnPropertyChanged(nameof(ProjectionParagraphNumber));
                OnPropertyChanged(nameof(SelectedParagraphText));
                OnPropertyChanged(nameof(FavoriteButtonText));
                if (value is null)
                {
                    selectedBibleVerseIsFavorite = false;
                    OnPropertyChanged(nameof(FavoriteButtonText));
                }
                else
                {
                    _ = RefreshSelectedBibleFavoriteStateAsync(value.VerseId);
                }

                RaiseCommandStates();
            }
        }
    }

    public SongResultViewModel? SelectedSong
    {
        get => selectedSong;
        set
        {
            if (SetProperty(ref selectedSong, value))
            {
                if (value is not null)
                {
                    ClearNonSongSelection();
                    IsSongsMode = true;
                    _ = LoadSongSectionsAsync(value);
                }
                else
                {
                    SongSections.Clear();
                    SelectedSongSection = null;
                }

                RaiseCurrentSelectionProperties();
                RaiseCommandStates();
            }
        }
    }

    public SongSectionViewModel? SelectedSongSection
    {
        get => selectedSongSection;
        set
        {
            if (SetProperty(ref selectedSongSection, value))
            {
                if (value is not null)
                {
                    ClearNonSongSelection();
                    IsSongsMode = true;
                }

                RaiseCurrentSelectionProperties();
                RaiseCommandStates();
            }
        }
    }

    public BibleNavigationItemViewModel? SelectedBibleNavigationItem
    {
        get => selectedBibleNavigationItem;
        set
        {
            if (SetProperty(ref selectedBibleNavigationItem, value) && value?.Verse is not null)
            {
                SelectedBibleVerse = value.Verse;
                StatusText = $"Selected {value.Verse.ReferenceDisplay}.";
            }
        }
    }

    public string BibleSearchText
    {
        get => bibleSearchText;
        set
        {
            if (SetProperty(ref bibleSearchText, value) && !suppressBibleSearchQueue)
            {
                QueueBibleSearch();
            }
        }
    }

    public string SongSearchText
    {
        get => songSearchText;
        set
        {
            if (SetProperty(ref songSearchText, value))
            {
                QueueSongSearch();
            }
        }
    }

    public SourceDiagnosticsViewModel SelectedSourceDetails
    {
        get => selectedSourceDetails;
        private set => SetProperty(ref selectedSourceDetails, value);
    }

    public ProjectionFontSizeOption? SelectedProjectionFontSize
    {
        get => selectedProjectionFontSize;
        set
        {
            if (SetProperty(ref selectedProjectionFontSize, value))
            {
                projectionFontSizeAdjustment = 0;
                projectionFontSizeUsesSafeFit = false;
                OnPropertyChanged(nameof(ProjectionFontSize));
                OnPropertyChanged(nameof(ProjectionLineHeight));
                OnPropertyChanged(nameof(ProjectionFontSizeOffset));
                OnPropertyChanged(nameof(SermonProjectionFontSize));
                OnPropertyChanged(nameof(IsProjectionFontSizeUsingSafeFit));
                RaiseProjectionTextSizeCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => statusText;
        set => SetProperty(ref statusText, value);
    }

    public string? LatestBackupPath
    {
        get => latestBackupPath;
        private set
        {
            if (SetProperty(ref latestBackupPath, value))
            {
                OnPropertyChanged(nameof(LatestBackupDisplay));
                OpenBackupFolderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LatestBackupDisplay =>
        string.IsNullOrWhiteSpace(LatestBackupPath)
            ? "No backup created this session."
            : LatestBackupPath;

    public bool IsSearching
    {
        get => isSearching;
        set
        {
            if (SetProperty(ref isSearching, value))
            {
                BibleSearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsDatabaseOperationRunning
    {
        get => isDatabaseOperationRunning;
        set
        {
            if (SetProperty(ref isDatabaseOperationRunning, value))
            {
                BackupDatabaseCommand.RaiseCanExecuteChanged();
                RestoreDatabaseCommand.RaiseCanExecuteChanged();
                OpenBackupFolderCommand.RaiseCanExecuteChanged();
                AddNewSourceCommand.RaiseCanExecuteChanged();
                ManageSourcesCommand.RaiseCanExecuteChanged();
                ImportSourceCommand.RaiseCanExecuteChanged();
                RepairSourceMetadataCommand.RaiseCanExecuteChanged();
                ImportBibleCommand.RaiseCanExecuteChanged();
                ClearHistoryCommand.RaiseCanExecuteChanged();
                VerifyProductionDataCommand.RaiseCanExecuteChanged();
                CleanupTestDataCommand.RaiseCanExecuteChanged();
                CleanupBrotherFrankCircularLettersCommand.RaiseCanExecuteChanged();
                TestProjectionDisplayCommand.RaiseCanExecuteChanged();
                RemoveFavoriteCommand.RaiseCanExecuteChanged();
                RemoveBibleFavoriteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowTestSourcesInManageSources
    {
        get => showTestSourcesInManageSources;
        set
        {
            if (SetProperty(ref showTestSourcesInManageSources, value))
            {
                RefreshManageableContentSources();
            }
        }
    }

    public bool IsBibleAvailable
    {
        get => isBibleAvailable;
        private set
        {
            if (SetProperty(ref isBibleAvailable, value))
            {
                OnPropertyChanged(nameof(IsBibleUnavailable));
                BibleSearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBibleUnavailable => !IsBibleAvailable;

    public string CurrentBibleTranslationDisplay =>
        SelectedBibleTranslation is null
            ? Loc.T("Info_NoTranslationImported")
            : Loc.F("Info_CurrentTranslation", SelectedBibleTranslation.Name, SelectedBibleTranslation.Abbreviation);

    public string SelectedBibleVersionShortDisplay =>
        SelectedBibleTranslation is null
            ? Loc.T("Info_VersionNone")
            : SelectedBibleTranslation.Name;

    public bool HasSingleBibleTranslation => BibleTranslations.Count == 1;

    public bool HasMultipleBibleTranslations => BibleTranslations.Count > 1;

    public string CurrentBibleVerseCountDisplay =>
        currentBibleVerseCount <= 0
            ? Loc.T("Info_NoVersesImported")
            : Loc.F("Info_VerseCount", currentBibleVerseCount.ToString("#,0", CultureInfo.InvariantCulture));

    public bool IsProjectionOpen
    {
        get => isProjectionOpen;
        private set
        {
            if (SetProperty(ref isProjectionOpen, value))
            {
                OnPropertyChanged(nameof(ProjectionStatusText));
            }
        }
    }

    public int ResultCount
    {
        get => resultCount;
        set
        {
            if (SetProperty(ref resultCount, value))
            {
                OnPropertyChanged(nameof(LibraryCountText));
                OnPropertyChanged(nameof(CenterPanelTitle));
            }
        }
    }

    public string ProjectionStatusText =>
        IsProjectionOpen
            ? Loc.F("Status_ProjectionOpen", projectionOpenDisplayText)
            : Loc.T("Status_ProjectionClosed");

    public bool HasProjectionPages =>
        IsProjectionOpen &&
        ActiveProjectionContent is not null &&
        projectionPageCount > 1 &&
        CurrentSelectionMatchesActiveProjection();

    public string ProjectionPageIndicatorText =>
        HasProjectionPages ? $"Page {projectionPageIndex + 1} of {projectionPageCount}" : string.Empty;

    public bool IsBibleMode
    {
        get => isBibleMode;
        private set
        {
            if (SetProperty(ref isBibleMode, value))
            {
                if (value && isSongsMode)
                {
                    isSongsMode = false;
                    OnPropertyChanged(nameof(IsSongsMode));
                }

                OnPropertyChanged(nameof(IsNotBibleMode));
                OnPropertyChanged(nameof(IsNotBibleOrSongsMode));
                OnPropertyChanged(nameof(IsSermonMode));
                OnPropertyChanged(nameof(IsSermonReadingMode));
                OnPropertyChanged(nameof(IsSermonGlobalSearchMode));
                OnPropertyChanged(nameof(CanShowSelectionNavigation));
                OnPropertyChanged(nameof(CenterPanelTitle));
                OnPropertyChanged(nameof(RightPanelTitle));
                OnPropertyChanged(nameof(LibraryCountText));
                OnPropertyChanged(nameof(IsFavoriteButtonVisible));
                OnPropertyChanged(nameof(PreviousButtonText));
                OnPropertyChanged(nameof(NextButtonText));
                OnPropertyChanged(nameof(FavoriteButtonText));
                RaiseCurrentSelectionProperties();
                RaiseCommandStates();
            }
        }
    }

    public bool IsNotBibleMode => !IsBibleMode;

    public bool IsSongsMode
    {
        get => isSongsMode;
        private set
        {
            if (SetProperty(ref isSongsMode, value))
            {
                if (value && isBibleMode)
                {
                    isBibleMode = false;
                    OnPropertyChanged(nameof(IsBibleMode));
                    OnPropertyChanged(nameof(IsNotBibleMode));
                }

                OnPropertyChanged(nameof(IsNotBibleOrSongsMode));
                OnPropertyChanged(nameof(IsSermonMode));
                OnPropertyChanged(nameof(IsSermonReadingMode));
                OnPropertyChanged(nameof(IsSermonGlobalSearchMode));
                OnPropertyChanged(nameof(CanShowSelectionNavigation));
                OnPropertyChanged(nameof(CenterPanelTitle));
                OnPropertyChanged(nameof(LibraryCountText));
                OnPropertyChanged(nameof(IsFavoriteButtonVisible));
                OnPropertyChanged(nameof(PreviousButtonText));
                OnPropertyChanged(nameof(NextButtonText));
                OnPropertyChanged(nameof(FavoriteButtonText));
                RaiseCurrentSelectionProperties();
                RaiseCommandStates();
            }
        }
    }

    public bool IsNotBibleOrSongsMode => !IsBibleMode && !IsSongsMode;

    public bool IsSermonMode => isSermonWorkspaceActive && !IsBibleMode && !IsSongsMode;

    public bool IsSermonReadingMode
    {
        get => isSermonReadingMode && IsSermonMode;
        private set
        {
            if (SetProperty(ref isSermonReadingMode, value))
            {
                OnPropertyChanged(nameof(IsSermonGlobalSearchMode));
                OnPropertyChanged(nameof(SermonReadingHeader));
                OnPropertyChanged(nameof(CenterPanelTitle));
                OnPropertyChanged(nameof(SermonHighlightQuery));
                OnPropertyChanged(nameof(PreviewHighlightQuery));
                OpenSermonCommand.RaiseCanExecuteChanged();
                BackToSermonSearchResultsCommand.RaiseCanExecuteChanged();
                RaiseCommandStates();
            }
        }
    }

    public bool IsSermonGlobalSearchMode => IsSermonMode && !IsSermonReadingMode;

    public int? FocusedSermonId => focusedSermonId;

    public int FocusedSermonParagraphCount => focusedSermonParagraphs.Count;

    public int FocusedSermonMatchCount => sermonWithinMatches.Count;

    public bool CanShowSelectionNavigation => IsBibleMode || IsSongsMode || IsSermonReadingMode;

    public string SermonReadingHeader => focusedSermon is null
        ? Loc.T("Panel_ReadingSermon")
        : Loc.F("Panel_SermonsBreadcrumb", focusedSermon.Title);

    public string CenterPanelTitle =>
        IsBibleMode
            ? Loc.T("Panel_BiblePreview")
            : IsSongsMode
                ? Loc.T("Panel_SongSections")
                : IsSermonReadingMode
                    ? SermonReadingHeader
                    : GetSearchResultsPanelTitle();

    public string RightPanelTitle => Loc.T("Panel_LiveProjection");

    public string LibraryCountText =>
        IsBibleMode
            ? Loc.Count(BibleNavigationItems.Count, "Count_BibleResult_One", "Count_BibleResult_Many")
            : IsSongsMode
                ? Loc.Count(SongResults.Count, "Count_Song_One", "Count_Song_Many")
            : isSermonBrowseMode
                ? Loc.Count(ResultCount, "Count_Sermon_One", "Count_Sermon_Many")
            : Loc.Count(ResultCount, "Count_Paragraph_One", "Count_Paragraph_Many");

    public string PreviewHeader =>
        IsBibleMode
            ? SelectedBibleVerse?.ReferenceDisplay ?? Loc.T("Bible_ReadyToSearchHeader")
            : IsSongsMode
                ? SelectedSong?.Title ?? Loc.T("Song_ReadyToSearchHeader")
            : SelectedParagraph is null
                ? Loc.T("Sermon_ReadyToSearchHeader")
                : $"{SelectedParagraph.SermonTitle}";

    public string PreviewMeta =>
        IsBibleMode
            ? SelectedBibleVerse?.MetaLine ?? Loc.T("Bible_ReferenceExamples")
            : IsSongsMode
                ? SelectedSongSection is null
                    ? Loc.T("Song_SearchHint")
                    : $"{SelectedSongSection.SectionLabel} | {SelectedSong?.FileName ?? Loc.T("Song_Source")}"
            : SelectedParagraph is null
                ? Loc.T("Sermon_SearchHintShort")
                : $"{SelectedParagraph.MetadataLine} | {Loc.F("Sermon_ParagraphLabel", SelectedParagraph.ParagraphNumber)}";

    public string PreviewText =>
        IsBibleMode
            ? SelectedBibleVerse?.Text ?? string.Empty
            : IsSongsMode
                ? SelectedSongSection?.Text ?? string.Empty
            : SelectedParagraph?.FullParagraphText ?? string.Empty;

    public string SermonHighlightQuery =>
        IsSermonReadingMode ? SermonWithinSearchText : SearchText;

    public string PreviewHighlightQuery =>
        IsSermonMode ? SermonHighlightQuery : string.Empty;

    public string SelectedParagraphHeader =>
        IsBibleMode && SelectedBibleVerse is not null
            ? SelectedBibleVerse.ReferenceDisplay
            : IsSongsMode && SelectedSong is not null
            ? SelectedSong.Title
            : SelectedParagraph is null
            ? Loc.T("Sermon_ReadyToSearchHeader")
            : $"{SelectedParagraph.SermonTitle}";

    public string SelectedParagraphMeta =>
        IsBibleMode && SelectedBibleVerse is not null
            ? SelectedBibleVerse.MetaLine
            : IsSongsMode && SelectedSongSection is not null
            ? $"{SelectedSongSection.SectionLabel} | {SelectedSong?.FileName ?? Loc.T("Song_Source")}"
            : SelectedParagraph is null
            ? Loc.T("Sermon_SearchHintShort")
            : $"{SelectedParagraph.MetadataLine} | {Loc.F("Sermon_ParagraphLabel", SelectedParagraph.ParagraphNumber)}";

    public string ProjectionParagraphTitle =>
        ActiveProjectionContent?.Title ?? "MessageFlow";

    public string ProjectionParagraphNumber =>
        ActiveProjectionContent?.Subtitle ?? string.Empty;

    public string SelectedParagraphText =>
        ActiveProjectionContent?.BodyText ?? string.Empty;

    public ProjectedContentSnapshot? ActiveProjectionContent
    {
        get => activeProjectionContent;
        private set
        {
            var contentTypeChanged = activeProjectionContent?.ContentType != value?.ContentType;
            if (SetProperty(ref activeProjectionContent, value))
            {
                if (contentTypeChanged && Math.Abs(projectionFontSizeAdjustment) > 0.1)
                {
                    projectionFontSizeAdjustment = 0;
                    OnPropertyChanged(nameof(ProjectionFontSize));
                    OnPropertyChanged(nameof(ProjectionLineHeight));
                    OnPropertyChanged(nameof(ProjectionFontSizeOffset));
                    OnPropertyChanged(nameof(SermonProjectionFontSize));
                }

                OnPropertyChanged(nameof(ProjectionParagraphTitle));
                OnPropertyChanged(nameof(ProjectionParagraphNumber));
                OnPropertyChanged(nameof(SelectedParagraphText));
                OnPropertyChanged(nameof(IsLiveBibleProjection));
                OnPropertyChanged(nameof(IsLiveSongProjection));
                OnPropertyChanged(nameof(IsLiveSermonProjection));
                OnPropertyChanged(nameof(HasProjectionPages));
                OnPropertyChanged(nameof(ProjectionPageIndicatorText));
                RaiseProjectionTextSizeCommandStates();
            }
        }
    }

    public bool IsLiveBibleProjection => ActiveProjectionContent?.ContentType == ProjectionContentType.Bible;

    public bool IsLiveSongProjection => ActiveProjectionContent?.ContentType == ProjectionContentType.Song;

    public bool IsLiveSermonProjection => ActiveProjectionContent?.ContentType == ProjectionContentType.Sermon;

    public double ProjectionFontSize =>
        Math.Max(24, (SelectedProjectionFontSize?.FontSize ?? 62) + projectionFontSizeAdjustment);

    public double ProjectionLineHeight =>
        ProjectionFontSize * ((SelectedProjectionFontSize?.LineHeight ?? 76) / (SelectedProjectionFontSize?.FontSize ?? 62));

    // Fit is zero. Manual controls move in exact 2-DIP steps below the measured safe maximum.
    public double ProjectionFontSizeOffset => projectionFontSizeAdjustment;

    public bool IsProjectionFontSizeUsingSafeFit => projectionFontSizeUsesSafeFit;

    // Sermons paginate rather than shrink to one screen. Medium retains the
    // established 60-DIP church-readable baseline while the other presets use
    // their intentional offsets from the shared 62-DIP Medium setting.
    public double SermonProjectionFontSize =>
        Math.Clamp(60 + (ProjectionFontSize - 62), 48, 108);

    public string FavoriteButtonText =>
        IsBibleMode
            ? selectedBibleVerseIsFavorite
                ? Loc.T("Bible_RemoveFavorite")
                : Loc.T("Bible_AddFavorite")
            : IsSongsMode
                ? string.Empty
            : SelectedParagraph?.IsFavorite == true
                ? Loc.T("Fav_Remove")
                : Loc.T("Fav_Add");

    public bool IsFavoriteButtonVisible => !IsSongsMode;

    public string PreviousButtonText =>
        IsBibleMode
            ? Loc.T("Bible_PreviousVerse")
            : IsSongsMode
                ? Loc.T("Song_PreviousSection")
                : Loc.T("Sermon_PreviousParagraph");

    public string NextButtonText =>
        IsBibleMode
            ? Loc.T("Bible_NextVerse")
            : IsSongsMode
                ? Loc.T("Song_NextSection")
                : Loc.T("Sermon_NextParagraph");

    public bool IsProjectionHistoryEmpty => ProjectionHistoryItems.Count == 0;

    public bool IsFavoritesEmpty => FavoriteParagraphs.Count == 0 && BibleFavoriteVerses.Count == 0;

    public bool HasFavorites => !IsFavoritesEmpty;

    public bool IsSermonFavoritesEmpty => FavoriteParagraphs.Count == 0;

    public bool IsBibleFavoritesEmpty => BibleFavoriteVerses.Count == 0;

    public bool IsParagraphResultsEmpty => ParagraphResults.Count == 0;

    public bool IsBibleResultsEmpty => BibleNavigationItems.Count == 0;

    public bool IsSongResultsEmpty => SongResults.Count == 0;

    public bool IsSongSectionsEmpty => SongSections.Count == 0;

    public async Task InitializeAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var startupMessages = new List<string>();

        QueueBrotherFrankPublicationMetadataRefresh();

        try
        {
            var filterLoadResult = await LoadFilterOptionsAsync(dbContext);
            if (filterLoadResult.LinkedAuthorCount == 0)
            {
                startupMessages.Add("No sermon authors found.");
            }

            if (filterLoadResult.YearCount == 0)
            {
                startupMessages.Add("No sermon years found.");
            }
        }
        catch (Exception ex)
        {
            App.LogStartupError("Author and year filters failed to load.", ex);
            startupMessages.Add($"Author/year filters could not load: {ex.Message}");
        }

        try
        {
            await LoadFavoritesAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Favorites failed to load during startup.", ex);
            startupMessages.Add("Favorites could not load.");
        }

        try
        {
            await LoadProjectionHistoryAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Projection history failed to load during startup.", ex);
            startupMessages.Add("Projection history could not load.");
        }

        try
        {
            await LoadContentSourcesAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Content sources failed to load during startup.", ex);
            startupMessages.Add("Content sources could not load.");
        }

        try
        {
            await LoadBibleTranslationsAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible translations failed to load during startup.", ex);
            startupMessages.Add("Bible translations could not load.");
        }

        try
        {
            await RefreshContentAvailabilityAsync();
            App.LogStartupMessage(
                $"Content availability for {ContentLanguageCode}: " +
                $"sermons={HasSermonContentForCurrentLanguage}, songs={HasSongContentForCurrentLanguage}.");
        }
        catch (Exception ex)
        {
            App.LogStartupError("Content language availability failed to load during startup.", ex);
        }

        StatusText = startupMessages.Count == 0
            ? Loc.T("Sermon_SearchHint")
            : string.Join(' ', startupMessages);
    }

    public Task RefreshFavoritesAsync()
    {
        return LoadFavoritesAsync();
    }

    public Task RefreshProjectionHistoryAsync()
    {
        return LoadProjectionHistoryAsync();
    }

    private void QueueBrotherFrankPublicationMetadataRefresh()
    {
        if (!ShowBrotherFrankLibrary)
        {
            App.LogStartupMessage("Brother Frank library is hidden until official import; skipping publication metadata refresh.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
                if (!await HasBrotherFrankPublicationMetadataDriftAsync(dbContext))
                {
                    App.LogStartupMessage("Brother Frank publication metadata already current.");
                    return;
                }

                App.LogStartupMessage("Brother Frank publication metadata repair queued after UI startup.");
                await EnsureBrotherFrankPublicationMetadataAsync(dbContext);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await RefreshFilterOptionsPreservingSelectionAsync();
                        await LoadContentSourcesAsync(SelectedContentSource?.Id);
                        QueueSearch();
                        StatusText = "Brother Frank publication metadata refreshed.";
                    }
                    catch (Exception ex)
                    {
                        App.LogStartupError("Brother Frank post-repair UI refresh failed.", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                App.LogStartupError("Brother Frank publication metadata repair failed after startup.", ex);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusText = "Brother Frank publication metadata could not be refreshed. Admin repair remains available.";
                });
            }
        });
    }

    public async Task RefreshLibrariesAfterImportAsync()
    {
        await RefreshFilterOptionsPreservingSelectionAsync();
        await LoadContentSourcesAsync(SelectedContentSource?.Id);
        QueueSearch();
        QueueSongSearch();
        StatusText = "Imported library content is searchable now.";
    }

    private async Task ClearHistoryAsync()
    {
        var confirmation = MessageBox.Show(
            Loc.T("Confirm_ClearHistory"),
            Loc.T("Confirm_ClearHistoryTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusText = "History was kept.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            await dbContext.ProjectionHistories.ExecuteDeleteAsync();

            ProjectionHistoryItems.Clear();
            SelectedHistoryParagraph = null;
            StatusText = "History cleared.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Clear history failed.", ex);
            StatusText = $"History could not be cleared: {ex.Message}";
            MessageBox.Show(
                $"History could not be cleared:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Clear History",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task VerifyProductionDataAsync()
    {
        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Verifying production data...";

            var report = await BuildProductionVerificationReportAsync();
            var window = new MessageFlow.App.ProductionVerificationWindow(report)
            {
                Owner = WindowPlacement.ResolveOwner(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.ShowDialog();

            StatusText = report.All(item => item.Passed)
                ? "Production data verification passed."
                : "Production data verification found items to review.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Production data verification failed.", ex);
            StatusText = $"Production verification failed: {ex.Message}";
            MessageBox.Show(
                $"Production verification failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Verify Production Data",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task<IReadOnlyList<ProductionVerificationItem>> BuildProductionVerificationReportAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var items = new List<ProductionVerificationItem>();

        var branhamSource = await dbContext.ContentSources
            .AsNoTracking()
            .FirstOrDefaultAsync(source => source.Name == "brother_branham" ||
                                           source.DisplayName.Contains("Branham"));
        items.Add(new ProductionVerificationItem(
            "Brother Branham source",
            branhamSource is not null,
            branhamSource is null ? "Brother Branham source was not found." : "Brother Branham source is available."));

        var branhamDocumentCount = branhamSource is null
            ? 0
            : await dbContext.Sermons
                .AsNoTracking()
                .CountAsync(sermon => sermon.ContentSourceId == branhamSource.Id);
        items.Add(new ProductionVerificationItem(
            "Brother Branham documents",
            branhamDocumentCount is >= 1_150 and <= 1_260,
            $"{branhamDocumentCount:N0} document(s) found."));

        var branhamParagraphCount = branhamSource is null
            ? 0
            : await dbContext.SermonParagraphs
                .AsNoTracking()
                .CountAsync(paragraph => paragraph.Sermon!.ContentSourceId == branhamSource.Id);
        items.Add(new ProductionVerificationItem(
            "Brother Branham paragraphs",
            branhamParagraphCount > 190_000,
            $"{branhamParagraphCount:N0} paragraph(s) found."));

        var kjvTranslation = await dbContext.BibleTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(translation => translation.Abbreviation == "KJV");
        items.Add(new ProductionVerificationItem(
            "KJV Bible",
            kjvTranslation is not null,
            kjvTranslation is null ? "KJV translation was not found." : "King James Version is available."));

        var bibleBookCount = await dbContext.BibleBooks
            .AsNoTracking()
            .CountAsync();
        items.Add(new ProductionVerificationItem(
            "Bible books",
            bibleBookCount == 66,
            $"{bibleBookCount:N0} book(s) found."));

        var kjvVerseCount = kjvTranslation is null
            ? 0
            : await dbContext.BibleVerses
                .AsNoTracking()
                .CountAsync(verse => verse.TranslationId == kjvTranslation.Id);
        items.Add(new ProductionVerificationItem(
            "KJV verse count",
            kjvVerseCount == 31_102,
            $"{kjvVerseCount:N0} verse(s) found."));

        if (kjvTranslation is not null)
        {
            items.Add(await VerifyBibleVerseAsync(dbContext, kjvTranslation.Id, "Genesis", 1, 1));
            items.Add(await VerifyBibleVerseAsync(dbContext, kjvTranslation.Id, "John", 3, 16));
            items.Add(await VerifyBibleVerseAsync(dbContext, kjvTranslation.Id, "Revelation", 22, 21));
        }
        else
        {
            items.Add(new ProductionVerificationItem("Genesis 1:1", false, "KJV was not available for verse checks."));
            items.Add(new ProductionVerificationItem("John 3:16", false, "KJV was not available for verse checks."));
            items.Add(new ProductionVerificationItem("Revelation 22:21", false, "KJV was not available for verse checks."));
        }

        items.Add(new ProductionVerificationItem(
            "Operator source list",
            ManageableContentSources.All(source => !LooksLikeTestSource(source) && !IsDeferredFrankLibrary(source)),
            "Test sources and deferred Brother Frank placeholders are hidden from normal source selection."));

        var searchIndexExists = await SearchIndexExistsAsync();
        items.Add(new ProductionVerificationItem(
            "Sermon search index",
            searchIndexExists,
            searchIndexExists ? "Sermon search index is available." : "Sermon search index was not found."));

        items.Add(new ProductionVerificationItem(
            "Projection window",
            true,
            "Projection window is available from Project."));

        return items;
    }

    private static async Task<ProductionVerificationItem> VerifyBibleVerseAsync(
        MessageFlowDbContext dbContext,
        int translationId,
        string bookName,
        int chapter,
        int verse)
    {
        var exists = await dbContext.BibleVerses
            .AsNoTracking()
            .AnyAsync(row =>
                row.TranslationId == translationId &&
                row.BibleBook!.Name == bookName &&
                row.Chapter == chapter &&
                row.Verse == verse);

        var reference = $"{bookName} {chapter}:{verse}";
        return new ProductionVerificationItem(
            reference,
            exists,
            exists ? $"{reference} is available." : $"{reference} was not found.");
    }

    private static async Task<bool> SearchIndexExistsAsync()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = MessageFlowDatabase.DefaultDatabasePath
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = 'SermonParagraphsFts';
            """;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    private async Task RebuildSermonSearchIndexAsync()
    {
        var confirmation = MessageBox.Show(
            Loc.T("Confirm_RebuildIndex"),
            Loc.T("Confirm_RebuildIndexTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusText = "Sermon search index rebuild canceled.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Rebuilding Sermon search index...";
            await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(MessageFlowDatabase.DefaultDatabasePath, App.LogStartupMessage);
            StatusText = "Sermon search index rebuilt.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Sermon search index rebuild failed.", ex);
            StatusText = $"Sermon search index rebuild failed: {ex.Message}";
            MessageBox.Show(
                $"Sermon search index rebuild failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Rebuild Sermon Search Index",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task CleanupTestDataAsync()
    {
        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Checking for test data...";

            var preview = await BuildTestDataCleanupPreviewAsync();
            if (preview.SourceCount == 0)
            {
                StatusText = "No test data was found.";
                MessageBox.Show(
                    "No test sources were found.",
                    "Cleanup Test Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirmationWindow = new MessageFlow.App.TestDataCleanupWindow(preview)
            {
                Owner = WindowPlacement.ResolveOwner(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (confirmationWindow.ShowDialog() != true)
            {
                StatusText = "Test data cleanup canceled.";
                return;
            }

            var databasePath = MessageFlowDatabase.DefaultDatabasePath;
            var backupPath = Path.Combine(
                Path.GetDirectoryName(databasePath) ?? Directory.GetCurrentDirectory(),
                "backups",
                $"messageflow_before_test_cleanup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            BackupDatabaseFile(databasePath, backupPath);
            LatestBackupPath = backupPath;

            await DeleteTestDataAsync(preview);
            await LoadContentSourcesAsync();
            await RefreshFilterOptionsPreservingSelectionAsync();

            StatusText = "Test data cleanup completed.";
            MessageBox.Show(
                $"Test data cleanup completed.{Environment.NewLine}{Environment.NewLine}Backup created:{Environment.NewLine}{backupPath}",
                "Cleanup Test Data",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Test data cleanup failed.", ex);
            StatusText = $"Test data cleanup failed: {ex.Message}";
            MessageBox.Show(
                $"Test data cleanup failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Cleanup Test Data",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task<TestDataCleanupPreview> BuildTestDataCleanupPreviewAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var sourceRows = await dbContext.ContentSources
            .AsNoTracking()
            .ToListAsync();

        var testSources = sourceRows
            .Where(source => !string.Equals(source.Name, "brother_branham", StringComparison.OrdinalIgnoreCase))
            .Where(source => LooksLikeTestSource(source.Name, source.DisplayName, source.LocalFolderPath))
            .OrderBy(source => source.DisplayName)
            .ToList();

        var sourcePreviews = new List<TestDataCleanupSourcePreview>();
        foreach (var source in testSources)
        {
            var documentCount = await dbContext.Sermons
                .AsNoTracking()
                .CountAsync(sermon => sermon.ContentSourceId == source.Id);
            var paragraphCount = await dbContext.SermonParagraphs
                .AsNoTracking()
                .CountAsync(paragraph => paragraph.Sermon!.ContentSourceId == source.Id);
            var favoriteCount = await dbContext.FavoriteParagraphs
                .AsNoTracking()
                .CountAsync(favorite => favorite.SermonParagraph!.Sermon!.ContentSourceId == source.Id);
            var historyCount = await dbContext.ProjectionHistories
                .AsNoTracking()
                .CountAsync(history => history.SermonParagraph!.Sermon!.ContentSourceId == source.Id);

            sourcePreviews.Add(new TestDataCleanupSourcePreview(
                source.Id,
                source.DisplayName,
                ContentSourceTypeOption.GetLabel(source.SourceType),
                string.IsNullOrWhiteSpace(source.LocalFolderPath) ? "No local folder configured." : source.LocalFolderPath,
                documentCount,
                paragraphCount,
                favoriteCount,
                historyCount));
        }

        return new TestDataCleanupPreview(sourcePreviews);
    }

    private async Task DeleteTestDataAsync(TestDataCleanupPreview preview)
    {
        var sourceIds = preview.Sources.Select(source => source.SourceId).ToArray();
        if (sourceIds.Length == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var testParagraphIds = dbContext.SermonParagraphs
            .Where(paragraph => paragraph.Sermon!.ContentSourceId != null &&
                                sourceIds.Contains(paragraph.Sermon.ContentSourceId.Value))
            .Select(paragraph => paragraph.Id);

        await dbContext.FavoriteParagraphs
            .Where(favorite => testParagraphIds.Contains(favorite.SermonParagraphId))
            .ExecuteDeleteAsync();
        await dbContext.ProjectionHistories
            .Where(history => testParagraphIds.Contains(history.SermonParagraphId))
            .ExecuteDeleteAsync();
        await dbContext.SermonParagraphs
            .Where(paragraph => paragraph.Sermon!.ContentSourceId != null &&
                                sourceIds.Contains(paragraph.Sermon.ContentSourceId.Value))
            .ExecuteDeleteAsync();
        await dbContext.Sermons
            .Where(sermon => sermon.ContentSourceId != null &&
                             sourceIds.Contains(sermon.ContentSourceId.Value))
            .ExecuteDeleteAsync();
        await dbContext.ContentSources
            .Where(source => sourceIds.Contains(source.Id))
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    private async Task CleanupBrotherFrankCircularLettersAsync()
    {
        BrotherFrankCircularLetterCleanupPreview preview;
        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Checking Brother Frank Circular Letter imports...";
            preview = await BuildBrotherFrankCircularLetterCleanupPreviewAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Brother Frank cleanup preview failed.", ex);
            StatusText = $"Brother Frank cleanup preview failed: {ex.Message}";
            MessageBox.Show(
                $"Brother Frank cleanup preview failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Cleanup Brother Frank Circular Letters",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }

        if (preview.DocumentCount == 0)
        {
            StatusText = "No Brother Frank Circular Letter imports were found.";
            MessageBox.Show(
                "No Brother Frank Circular Letter imports were found.",
                "Cleanup Brother Frank Circular Letters",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var sampleText = preview.SampleTitles.Count == 0
            ? "No sample titles available."
            : string.Join(Environment.NewLine, preview.SampleTitles.Select(title => $"- {title}"));
        var confirmation = MessageBox.Show(
            "Preview of Brother Frank Circular Letter data to remove:" +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"Documents: {preview.DocumentCount:N0}{Environment.NewLine}" +
            $"Paragraphs: {preview.ParagraphCount:N0}{Environment.NewLine}" +
            $"Linked favorites: {preview.FavoriteCount:N0}{Environment.NewLine}" +
            $"Linked projection history: {preview.HistoryCount:N0}{Environment.NewLine}{Environment.NewLine}" +
            "Sample documents:" +
            $"{Environment.NewLine}{sampleText}{Environment.NewLine}{Environment.NewLine}" +
            "This will not remove Brother Branham data, KJV Bible data, unrelated favorites, or the content source configuration.",
            "Confirm Brother Frank Cleanup",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.OK)
        {
            StatusText = "Brother Frank cleanup canceled.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Removing Brother Frank Circular Letter imports...";

            var databasePath = MessageFlowDatabase.DefaultDatabasePath;
            var backupPath = Path.Combine(
                Path.GetDirectoryName(databasePath) ?? Directory.GetCurrentDirectory(),
                "backups",
                $"messageflow_before_brother_frank_cleanup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            BackupDatabaseFile(databasePath, backupPath);
            LatestBackupPath = backupPath;

            var removed = await DeleteBrotherFrankCircularLettersAsync();
            await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(databasePath, App.LogStartupMessage);
            await RefreshFilterOptionsPreservingSelectionAsync();
            await LoadContentSourcesAsync(SelectedContentSource?.Id);
            await LoadFavoritesAsync();
            await LoadProjectionHistoryAsync();

            if (SelectedParagraph?.IsCircularLetter == true)
            {
                SetResults([]);
            }

            StatusText =
                $"Brother Frank cleanup completed: {removed.DocumentCount:N0} documents and {removed.ParagraphCount:N0} paragraphs removed.";
            MessageBox.Show(
                $"Brother Frank cleanup completed.{Environment.NewLine}{Environment.NewLine}" +
                $"Removed {removed.DocumentCount:N0} document(s), {removed.ParagraphCount:N0} paragraph(s), " +
                $"{removed.FavoriteCount:N0} linked favorite(s), and {removed.HistoryCount:N0} linked history item(s)." +
                $"{Environment.NewLine}{Environment.NewLine}Backup created:{Environment.NewLine}{backupPath}",
                "Cleanup Brother Frank Circular Letters",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Brother Frank cleanup failed.", ex);
            StatusText = $"Brother Frank cleanup failed: {ex.Message}";
            MessageBox.Show(
                $"Brother Frank cleanup failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Cleanup Brother Frank Circular Letters",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task<BrotherFrankCircularLetterCleanupPreview> BuildBrotherFrankCircularLetterCleanupPreviewAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var targetSermonIds = await BrotherFrankCircularLetterSermons(dbContext)
            .Select(sermon => sermon.Id)
            .ToListAsync();

        if (targetSermonIds.Count == 0)
        {
            return BrotherFrankCircularLetterCleanupPreview.Empty;
        }

        var paragraphIds = dbContext.SermonParagraphs
            .Where(paragraph => targetSermonIds.Contains(paragraph.SermonId))
            .Select(paragraph => paragraph.Id);

        var documentCount = targetSermonIds.Count;
        var paragraphCount = await dbContext.SermonParagraphs
            .AsNoTracking()
            .CountAsync(paragraph => targetSermonIds.Contains(paragraph.SermonId));
        var favoriteCount = await dbContext.FavoriteParagraphs
            .AsNoTracking()
            .CountAsync(favorite => paragraphIds.Contains(favorite.SermonParagraphId));
        var historyCount = await dbContext.ProjectionHistories
            .AsNoTracking()
            .CountAsync(history => paragraphIds.Contains(history.SermonParagraphId));
        var sampleTitles = await BrotherFrankCircularLetterSermons(dbContext)
            .OrderBy(sermon => sermon.Year)
            .ThenBy(sermon => sermon.Title)
            .Select(sermon => sermon.Title)
            .Take(8)
            .ToListAsync();

        return new BrotherFrankCircularLetterCleanupPreview(
            documentCount,
            paragraphCount,
            favoriteCount,
            historyCount,
            sampleTitles);
    }

    private async Task<BrotherFrankCircularLetterCleanupPreview> DeleteBrotherFrankCircularLettersAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var targetSermonIds = await BrotherFrankCircularLetterSermons(dbContext)
            .Select(sermon => sermon.Id)
            .ToListAsync();

        if (targetSermonIds.Count == 0)
        {
            return BrotherFrankCircularLetterCleanupPreview.Empty;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var targetParagraphIds = dbContext.SermonParagraphs
            .Where(paragraph => targetSermonIds.Contains(paragraph.SermonId))
            .Select(paragraph => paragraph.Id);

        var favoriteCount = await dbContext.FavoriteParagraphs
            .Where(favorite => targetParagraphIds.Contains(favorite.SermonParagraphId))
            .ExecuteDeleteAsync();
        var historyCount = await dbContext.ProjectionHistories
            .Where(history => targetParagraphIds.Contains(history.SermonParagraphId))
            .ExecuteDeleteAsync();
        var paragraphCount = await dbContext.SermonParagraphs
            .Where(paragraph => targetSermonIds.Contains(paragraph.SermonId))
            .ExecuteDeleteAsync();
        var documentCount = await dbContext.Sermons
            .Where(sermon => targetSermonIds.Contains(sermon.Id))
            .ExecuteDeleteAsync();

        dbContext.ImportLogs.Add(new ImportLog
        {
            FilePath = "Brother Frank Circular Letter cleanup",
            Status = "Cleanup",
            Message =
                $"Removed {documentCount} Brother Frank Circular Letter documents, {paragraphCount} paragraphs, " +
                $"{favoriteCount} linked favorites, and {historyCount} linked history items.",
            ImportedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new BrotherFrankCircularLetterCleanupPreview(
            documentCount,
            paragraphCount,
            favoriteCount,
            historyCount,
            []);
    }

    private static IQueryable<Sermon> BrotherFrankCircularLetterSermons(MessageFlowDbContext dbContext)
    {
        return dbContext.Sermons
            .Where(sermon =>
                sermon.ContentSource != null &&
                sermon.ContentSource.SourceType == "CircularLetter" &&
                (
                    (sermon.Author != null &&
                     (sermon.Author.DisplayName == "Brother Frank" ||
                      sermon.Author.FullName == "Ewald Frank")) ||
                    EF.Functions.Like(sermon.ContentSource.Name, "%ewald%") ||
                    EF.Functions.Like(sermon.ContentSource.DisplayName, "%Ewald Frank%") ||
                    EF.Functions.Like(sermon.ContentSource.DisplayName, "%Brother Frank%")
                ) &&
                (EF.Functions.Like(sermon.Title, "Circular Letter%") ||
                 EF.Functions.Like(sermon.SermonCode, "CL-%")));
    }

    public void SetBibleMode(bool enabled)
    {
        IsBibleMode = enabled;
        if (enabled)
        {
            IsSongsMode = false;
            StatusText = SelectedBibleVerse is null
                ? "Search by book, chapter, verse, or keyword."
                : $"Selected {SelectedBibleVerse.ReferenceDisplay}.";
            return;
        }

        if (IsSongsMode)
        {
            return;
        }

        if (SelectedParagraph is null && ParagraphResults.Count > 0)
        {
            SelectedParagraph = ParagraphResults.FirstOrDefault();
        }

        StatusText = SelectedParagraph is null
            ? Loc.T("Sermon_SearchHintShort")
            : Loc.F("Status_SelectedParagraph", SelectedParagraph.ParagraphNumber);
    }

    public void SetSermonWorkspaceActive(bool active)
    {
        if (isSermonWorkspaceActive == active)
        {
            return;
        }

        isSermonWorkspaceActive = active;
        if (!active)
        {
            sermonWithinSearchDebounce?.Cancel();
            ClearSermonWithinSearch(resetText: true);
        }

        OnPropertyChanged(nameof(IsSermonMode));
        OnPropertyChanged(nameof(IsSermonReadingMode));
        OnPropertyChanged(nameof(IsSermonGlobalSearchMode));
        OnPropertyChanged(nameof(CanShowSelectionNavigation));
        OnPropertyChanged(nameof(CenterPanelTitle));
        OnPropertyChanged(nameof(RightPanelTitle));
        RaiseCurrentSelectionProperties();
        OpenSermonCommand.RaiseCanExecuteChanged();
        BackToSermonSearchResultsCommand.RaiseCanExecuteChanged();
        RaiseCommandStates();
    }

    public void SetSongsMode(bool enabled)
    {
        IsSongsMode = enabled;
        if (enabled)
        {
            IsBibleMode = false;
            RestoreLibraryStatusText();
            QueueSongSearch();
            return;
        }

        songSearchDebounce?.Cancel();
        Interlocked.Increment(ref songSearchRequestVersion);
        IsSearching = false;

        if (!IsBibleMode && SelectedParagraph is null && ParagraphResults.Count > 0)
        {
            SelectedParagraph = ParagraphResults.FirstOrDefault();
        }

        RestoreLibraryStatusText();
    }

    private void RestoreLibraryStatusText()
    {
        if (IsSongsMode)
        {
            StatusText = SelectedSongSection is null
                ? HasSongContentForCurrentLanguage
                    ? Loc.T("Song_SearchHint")
                    : Loc.F("Song_NoContentForLanguage", SelectedUiLanguage.NativeName)
                : $"Selected {SelectedSongSection.SectionLabel}.";
            return;
        }

        if (IsBibleMode)
        {
            StatusText = SelectedBibleVerse is null
                ? Loc.T("Bible_SearchHint")
                : $"Selected {SelectedBibleVerse.ReferenceDisplay}.";
            return;
        }

        StatusText = SelectedParagraph is null
            ? Loc.T("Sermon_SearchHintShort")
            : Loc.F("Status_SelectedParagraph", SelectedParagraph.ParagraphNumber);
    }

    public async Task BackupDatabaseAsync()
    {
        var databasePath = MessageFlowDatabase.DefaultDatabasePath;
        var defaultBackupName = $"messageflow_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var dialog = new SaveFileDialog
        {
            Title = "Backup MessageFlow Database",
            AddExtension = true,
            DefaultExt = ".db",
            Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            FileName = defaultBackupName,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Backing up database...";

            await Task.Run(() => BackupDatabaseFile(databasePath, dialog.FileName));

            LatestBackupPath = dialog.FileName;
            StatusText = $"Backup completed successfully:{Environment.NewLine}{dialog.FileName}";

            MessageBox.Show(
                $"Backup completed successfully{Environment.NewLine}{Environment.NewLine}{dialog.FileName}",
                "MessageFlow Backup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Database backup failed.", ex);
            StatusText = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    public async Task RestoreDatabaseAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Restore MessageFlow Database",
            CheckFileExists = true,
            DefaultExt = ".db",
            Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            Loc.T("Confirm_RestoreDatabase"),
            Loc.T("Confirm_RestoreDatabaseTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Restoring database...";

            var databasePath = MessageFlowDatabase.DefaultDatabasePath;
            await Task.Run(() => RestoreDatabaseFile(databasePath, dialog.FileName));
            await MessageFlowDatabaseRepair.RepairAsync(databasePath, App.LogStartupMessage);
            await ReloadAfterDatabaseRestoreAsync();

            StatusText = "Restore completed successfully.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Database restore failed.", ex);
            StatusText = $"Restore failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private void OpenLatestBackupFolder()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(LatestBackupPath))
            {
                StatusText = "No backup has been created yet.";
                return;
            }

            var backupFolder = Path.GetDirectoryName(LatestBackupPath);
            if (string.IsNullOrWhiteSpace(backupFolder) || !Directory.Exists(backupFolder))
            {
                StatusText = "Backup folder could not be found.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = backupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.LogStartupError("Open backup folder failed.", ex);
            StatusText = $"Open backup folder failed: {ex.Message}";
        }
    }

    private async Task AddNewSourceAsync()
    {
        var dialog = new AddContentSourceWindow
        {
            Owner = WindowPlacement.ResolveOwner()
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var sourceName = CreateSourceName(dialog.DisplayNameValue);
        var description = string.IsNullOrWhiteSpace(dialog.DescriptionValue)
            ? $"Local {ContentSourceTypeOption.GetLabel(dialog.SourceTypeValue)} source."
            : dialog.DescriptionValue;

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = "Saving content source...";

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

            var duplicateExists = await dbContext.ContentSources
                .AsNoTracking()
                .AnyAsync(source => source.Name == sourceName);

            if (duplicateExists)
            {
                StatusText = $"A source named {dialog.DisplayNameValue} already exists.";
                MessageBox.Show(
                    "A source with that generated name already exists. Use a different display name.",
                    "MessageFlow Sources",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var source = new ContentSource
            {
                Name = sourceName,
                DisplayName = TrimTo(dialog.DisplayNameValue, 200),
                SourceType = dialog.SourceTypeValue,
                Description = TrimTo(description, 1000),
                LocalFolderPath = dialog.LocalFolderPathValue is null
                    ? null
                    : TrimTo(dialog.LocalFolderPathValue, 1024),
                CreatedAt = DateTime.UtcNow
            };

            dbContext.ContentSources.Add(source);
            await dbContext.SaveChangesAsync();
            await LoadContentSourcesAsync(source.Id);

            StatusText = $"Source added: {source.DisplayName}.";
        }
        catch (DbUpdateException ex)
        {
            App.LogStartupError("Content source save failed.", ex);
            StatusText = $"Source save failed: {ex.GetBaseException().Message}";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Content source save failed.", ex);
            StatusText = $"Source save failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private void ShowManageSources()
    {
        var dialog = new ManageSourcesWindow(this)
        {
            Owner = WindowPlacement.ResolveOwner(),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        dialog.ShowDialog();
    }

    private async Task ImportBibleAsync()
    {
        var dialog = new ImportBibleWindow
        {
            Owner = WindowPlacement.ResolveOwner(),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (dialog.ShowDialog() != true || dialog.PreviewSummary is null)
        {
            StatusText = "Bible import canceled. No database changes were made.";
            return;
        }

        var preview = dialog.PreviewSummary;
        if (preview.VerseCount == 0)
        {
            StatusText = "No Bible verses are ready to import.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Preparing {preview.Abbreviation} Bible import...";

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

            var translation = await dbContext.BibleTranslations
                .FirstOrDefaultAsync(item => item.Abbreviation == preview.Abbreviation);

            if (translation is not null)
            {
                var existingVerseCount = await dbContext.BibleVerses
                    .CountAsync(verse => verse.TranslationId == translation.Id);

                if (existingVerseCount > 0)
                {
                    var confirmation = MessageBox.Show(
                        Loc.F("Confirm_ImportBibleReplace", preview.Abbreviation),
                        Loc.T("ImportBible_Title"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No);

                    if (confirmation != MessageBoxResult.Yes)
                    {
                        StatusText = "Bible import canceled. Existing verses were kept.";
                        return;
                    }
                }
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            if (translation is null)
            {
                translation = new BibleTranslation
                {
                    Name = TrimTo(preview.TranslationName, 200),
                    Abbreviation = TrimTo(preview.Abbreviation, 40),
                    Language = TrimTo(preview.Language, 80),
                    Description = TrimTo(
                        string.IsNullOrWhiteSpace(preview.Description)
                            ? $"{preview.TranslationName} local CSV import."
                            : preview.Description,
                        1000),
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.BibleTranslations.Add(translation);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                translation.Name = TrimTo(preview.TranslationName, 200);
                translation.Language = TrimTo(preview.Language, 80);
                translation.Description = TrimTo(
                    string.IsNullOrWhiteSpace(preview.Description)
                        ? $"{preview.TranslationName} local CSV import."
                        : preview.Description,
                    1000);

                await dbContext.BibleVerses
                    .Where(verse => verse.TranslationId == translation.Id)
                    .ExecuteDeleteAsync();
                await dbContext.SaveChangesAsync();
            }

            var booksByName = await dbContext.BibleBooks
                .AsNoTracking()
                .ToDictionaryAsync(book => book.Name, StringComparer.OrdinalIgnoreCase);

            var distinctRows = preview.Verses
                .GroupBy(row => new { row.BookName, row.Chapter, row.Verse })
                .Select(group => group.Last())
                .ToList();

            var importedAt = DateTime.UtcNow;
            dbContext.BibleVerses.AddRange(distinctRows.Select(row =>
            {
                if (!booksByName.TryGetValue(row.BookName, out var book))
                {
                    throw new InvalidOperationException($"Bible book is missing from database: {row.BookName}.");
                }

                return new BibleVerse
                {
                    TranslationId = translation.Id,
                    BookId = book.Id,
                    Chapter = row.Chapter,
                    Verse = row.Verse,
                    Text = row.Text,
                    SearchText = row.SearchText,
                    CreatedAt = importedAt
                };
            }));

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            await LoadBibleTranslationsAsync(translation.Id);
            StatusText = $"Imported {preview.Abbreviation}: {distinctRows.Count:N0} verses.";
            MessageBox.Show(
                $"Imported {distinctRows.Count:N0} verses for {preview.TranslationName} ({preview.Abbreviation}).",
                "Import Bible",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible import failed.", ex);
            StatusText = $"Bible import failed: {ex.Message}";
            MessageBox.Show(
                $"Bible import failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Import Bible",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task ImportSelectedSourceAsync()
    {
        var source = SelectedContentSource;
        if (source is null)
        {
            StatusText = "Select a source before importing.";
            return;
        }

        if (!CanImportPdfSourceType(source.SourceType))
        {
            StatusText = "Import for this source type is coming soon.";
            MessageBox.Show(
                "Import for this source type is coming soon.",
                "MessageFlow Sources",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(source.LocalFolderPath) || !Directory.Exists(source.LocalFolderPath))
        {
            StatusText = "Source folder does not exist.";
            App.LogStartupMessage(
                $"Import preview failed before scan. Source: {source.DisplayName}. Folder: {source.LocalFolderPath ?? "(none)"}. Reason: folder does not exist.");
            MessageBox.Show(
                "Source folder does not exist.",
                "MessageFlow Sources",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ImportPreviewSummary preview;
        var previewStopwatch = Stopwatch.StartNew();
        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Scanning {source.DisplayName} for import preview...";
            App.LogStartupMessage(
                $"Import preview scan starting. Source: {source.DisplayName}. Folder: {source.LocalFolderPath}.");
            preview = await BuildImportPreviewAsync(source);
            previewStopwatch.Stop();
            LogImportPreviewDiagnostics(source, preview, previewStopwatch.ElapsedMilliseconds);
            StatusText = CreateImportPreviewReadyStatus(preview);
        }
        catch (Exception ex)
        {
            previewStopwatch.Stop();
            App.LogStartupError("Source import preview failed.", ex);
            StatusText = $"Import preview failed: {ex.Message}";
            MessageBox.Show(
                $"Import preview failed:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "MessageFlow Import Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }

        bool? previewResult;
        try
        {
            var previewWindow = new ImportPreviewWindow(preview)
            {
                Owner = WindowPlacement.ResolveOwner(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            previewResult = previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            App.LogStartupError("Source import preview dialog failed to open.", ex);
            StatusText = $"Import preview failed: {ex.Message}";
            MessageBox.Show(
                $"Import Preview could not open:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "MessageFlow Import Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (previewResult != true)
        {
            StatusText = "Import canceled. No database changes were made.";
            return;
        }

        if (!preview.CanStartImport)
        {
            StatusText = "No new PDF files are ready to import.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Starting import for {source.DisplayName}...";

            var progress = new Progress<ImportProgress>(UpdateImportProgress);
            var summary = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
                var importer = new PdfSermonImporter(dbContext);
                return await importer.ImportAsync(
                    new ImportOptions(
                        source.LocalFolderPath,
                        Force: false,
                        Reset: false,
                        ContentSourceId: source.Id,
                        Progress: progress));
            });

            await RefreshFilterOptionsPreservingSelectionAsync();
            await LoadContentSourcesAsync(source.Id);
            await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(MessageFlowDatabase.DefaultDatabasePath, App.LogStartupMessage);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                await SearchNowAsync();
            }

            StatusText =
                $"Import complete for {source.DisplayName}: {summary.ImportedFiles:N0} files, {summary.ImportedParagraphs:N0} paragraphs, {summary.SkippedFiles:N0} skipped, {summary.ErrorCount:N0} errors.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Source import failed.", ex);
            StatusText = $"Source import failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private static string CreateImportPreviewReadyStatus(ImportPreviewSummary preview)
    {
        if (preview.PdfFilesFound == 0)
        {
            return "No PDF files found in this source folder.";
        }

        return preview.ReadyToImportFiles == 0
            ? "No new files ready to import."
            : "Import preview ready.";
    }

    private static void LogImportPreviewDiagnostics(
        ContentSourceViewModel source,
        ImportPreviewSummary preview,
        long elapsedMilliseconds)
    {
        App.LogStartupMessage(
            "Import preview ready." +
            $"{Environment.NewLine}Source: {source.DisplayName}" +
            $"{Environment.NewLine}Folder: {preview.LocalFolderPath}" +
            $"{Environment.NewLine}PDFs found: {preview.PdfFilesFound:N0}" +
            $"{Environment.NewLine}Already imported: {preview.AlreadyImportedFiles:N0}" +
            $"{Environment.NewLine}Ready to import: {preview.ReadyToImportFiles:N0}" +
            $"{Environment.NewLine}Invalid or missing: {preview.InvalidOrMissingFilesCount:N0}" +
            $"{Environment.NewLine}Quality extracted paragraphs: {preview.QualitySummary.TotalExtractedParagraphs:N0}" +
            $"{Environment.NewLine}Quality accepted paragraphs: {preview.QualitySummary.AcceptedParagraphs:N0}" +
            $"{Environment.NewLine}Quality rejected paragraphs: {preview.QualitySummary.TotalRejected:N0}" +
            $"{Environment.NewLine}Scan elapsed ms: {elapsedMilliseconds:N0}");
    }

    private async Task RepairSelectedSourceMetadataAsync()
    {
        var source = SelectedContentSource;
        if (source is null)
        {
            StatusText = "Select a source before repairing metadata.";
            return;
        }

        var sourceContext = CreateSourceMetadataContext(source);
        if (SermonMetadataParser.IsBrotherBranhamSource(sourceContext))
        {
            StatusText = "Brother Branham metadata is protected and was not changed.";
            MessageBox.Show(
                "Brother Branham metadata uses the established sermon parser and will not be repaired by this tool.",
                "MessageFlow Sources",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!SermonMetadataParser.IsEwaldFrankSource(sourceContext))
        {
            StatusText = "This repair is currently limited to Ewald Frank publication sources.";
            MessageBox.Show(
                "This repair action is currently limited to Ewald Frank publication sources.",
                "MessageFlow Sources",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SourceMetadataRepairPreview repairPreview;
        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Checking repair preview for {source.DisplayName}...";
            repairPreview = await BuildSourceMetadataRepairPreviewAsync(source, sourceContext);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Source metadata repair preview failed.", ex);
            StatusText = $"Source metadata repair preview failed: {ex.Message}";
            return;
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }

        if (repairPreview.DocumentCount == 0)
        {
            StatusText = $"No documents were found for {source.DisplayName}.";
            MessageBox.Show(
                "No imported documents were found for the selected source.",
                "Repair Source Metadata",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var confirmation = MessageBox.Show(
            $"Repair metadata for \"{source.DisplayName}\"?{Environment.NewLine}{Environment.NewLine}" +
            $"Documents checked: {repairPreview.DocumentCount:N0}{Environment.NewLine}" +
            $"Documents that would be repaired: {repairPreview.DocumentsToRepairCount:N0}{Environment.NewLine}" +
            $"Source type would change: {(repairPreview.WouldChangeSourceType ? "Yes" : "No")}{Environment.NewLine}{Environment.NewLine}" +
            "This updates title, code, year, author, and source type from local PDF file names. Paragraph text, favorites, and projection history are not changed.",
            "Repair Source Metadata",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusText = "Source metadata repair canceled.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Repairing metadata for {source.DisplayName}...";

            var repairedCount = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

                var sourceEntity = await dbContext.ContentSources
                    .FirstOrDefaultAsync(contentSource => contentSource.Id == source.Id);

                if (sourceEntity is null)
                {
                    throw new InvalidOperationException("The selected content source could not be found.");
                }

                var sermons = await dbContext.Sermons
                    .Where(sermon => sermon.ContentSourceId == source.Id)
                    .ToListAsync();

                if (sermons.Count == 0)
                {
                    return 0;
                }

                var authorId = await EnsureSourceRepairAuthorAsync(dbContext, sourceContext);
                var sourceRoot = string.IsNullOrWhiteSpace(source.LocalFolderPath)
                    ? null
                    : Path.GetFullPath(source.LocalFolderPath);
                var repaired = 0;
                var hasCircularLetters = false;

                foreach (var sermon in sermons)
                {
                    var metadataRoot = sourceRoot ??
                                       Path.GetDirectoryName(sermon.SourceFilePath) ??
                                       Directory.GetCurrentDirectory();
                    var metadata = SermonMetadataParser.Parse(
                        sermon.SourceFilePath,
                        metadataRoot,
                        sourceContext);
                    var changed = false;
                    hasCircularLetters = hasCircularLetters ||
                                         metadata.Title.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase) ||
                                         metadata.SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase);

                    if (!string.Equals(sermon.Title, metadata.Title, StringComparison.Ordinal))
                    {
                        sermon.Title = metadata.Title;
                        changed = true;
                    }

                    if (!string.Equals(sermon.SermonCode, metadata.SermonCode, StringComparison.Ordinal))
                    {
                        sermon.SermonCode = metadata.SermonCode;
                        changed = true;
                    }

                    if (sermon.Year != metadata.Year)
                    {
                        sermon.Year = metadata.Year;
                        changed = true;
                    }

                    if (sermon.Date != metadata.Date)
                    {
                        sermon.Date = metadata.Date;
                        changed = true;
                    }

                    if (!string.Equals(sermon.Location, metadata.Location, StringComparison.Ordinal))
                    {
                        sermon.Location = metadata.Location;
                        changed = true;
                    }

                    if (!string.Equals(sermon.Language, metadata.Language, StringComparison.Ordinal))
                    {
                        sermon.Language = metadata.Language;
                        changed = true;
                    }

                    if (sermon.AuthorId != authorId)
                    {
                        sermon.AuthorId = authorId;
                        changed = true;
                    }

                    if (changed)
                    {
                        repaired++;
                    }
                }

                var repairedSourceType = hasCircularLetters ? "CircularLetter" : "Book";
                if (!string.Equals(sourceEntity.DisplayName, "Brother Frank Publications", StringComparison.Ordinal))
                {
                    sourceEntity.DisplayName = "Brother Frank Publications";
                }

                if (!string.Equals(sourceEntity.SourceType, repairedSourceType, StringComparison.OrdinalIgnoreCase))
                {
                    sourceEntity.SourceType = repairedSourceType;
                }

                if (dbContext.ChangeTracker.HasChanges())
                {
                    await dbContext.SaveChangesAsync();
                }

                return repaired;
            });

            await RefreshFilterOptionsPreservingSelectionAsync();
            await LoadContentSourcesAsync(source.Id);
            await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(MessageFlowDatabase.DefaultDatabasePath, App.LogStartupMessage);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                await SearchNowAsync();
            }

            StatusText = repairedCount == 0
                ? $"No metadata changes were needed for {source.DisplayName}."
                : $"Repaired metadata for {repairedCount:N0} document(s) in {source.DisplayName}.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Source metadata repair failed.", ex);
            StatusText = $"Source metadata repair failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task<SourceMetadataRepairPreview> BuildSourceMetadataRepairPreviewAsync(
        ContentSourceViewModel source,
        SourceMetadataContext sourceContext)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var sermons = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId == source.Id)
            .Select(sermon => new SourceDocumentDiagnosticsRow(
                sermon.Title,
                sermon.SermonCode,
                sermon.Year,
                sermon.Date,
                sermon.Location,
                sermon.Language,
                sermon.AuthorId,
                sermon.Author == null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(sermon.Author.DisplayName)
                        ? sermon.Author.FullName
                        : sermon.Author.DisplayName,
                sermon.SourceFilePath))
            .ToListAsync();

        if (sermons.Count == 0)
        {
            return new SourceMetadataRepairPreview(0, 0, false);
        }

        var authorMetadata = SermonMetadataParser.GetAuthorMetadata(sourceContext);
        var existingAuthorId = await dbContext.Authors
            .AsNoTracking()
            .Where(author => author.FullName == authorMetadata.FullName ||
                             author.DisplayName == authorMetadata.DisplayName)
            .Select(author => (int?)author.Id)
            .FirstOrDefaultAsync();

        var sourceRoot = string.IsNullOrWhiteSpace(source.LocalFolderPath)
            ? null
            : Path.GetFullPath(source.LocalFolderPath);
        var repairCount = 0;
        var hasCircularLetters = false;

        foreach (var sermon in sermons)
        {
            var metadataRoot = sourceRoot ??
                               Path.GetDirectoryName(sermon.SourceFilePath) ??
                               Directory.GetCurrentDirectory();
            var metadata = SermonMetadataParser.Parse(
                sermon.SourceFilePath,
                metadataRoot,
                sourceContext);

            hasCircularLetters = hasCircularLetters || IsCircularLetterMetadata(metadata);

            if (WouldRepairSourceMetadata(sermon, metadata, existingAuthorId))
            {
                repairCount++;
            }
        }

        var repairedSourceType = hasCircularLetters ? "CircularLetter" : "Book";
        var wouldChangeSourceType =
            !string.Equals(source.SourceType, repairedSourceType, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(source.DisplayName, "Brother Frank Publications", StringComparison.Ordinal);

        return new SourceMetadataRepairPreview(sermons.Count, repairCount, wouldChangeSourceType);
    }

    private async Task<ImportPreviewSummary> BuildImportPreviewAsync(ContentSourceViewModel source)
    {
        if (string.IsNullOrWhiteSpace(source.LocalFolderPath))
        {
            throw new InvalidOperationException("The selected source does not have a local folder path.");
        }

        var localFolderPath = Path.GetFullPath(source.LocalFolderPath);
        return await Task.Run(async () =>
        {
            var scan = ScanPdfFiles(localFolderPath);

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var importedPaths = await dbContext.Sermons
                .AsNoTracking()
                .Select(sermon => sermon.SourceFilePath)
                .ToListAsync();

            var importedSet = importedPaths
                .Select(NormalizeFilePathForComparison)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var readyFiles = scan.PdfFilePaths
                .Where(path => !importedSet.Contains(NormalizeFilePathForComparison(path)))
                .ToList();
            var sourceContext = CreateSourceMetadataContext(source);
            var authorName = SermonMetadataParser.GetAuthorMetadata(sourceContext).DisplayName;
            var metadataSamples = scan.PdfFilePaths
                .Take(10)
                .Select(path => CreateImportPreviewMetadataSample(
                    path,
                    localFolderPath,
                    source,
                    sourceContext,
                    authorName,
                    importedSet))
                .ToList();
            var qualitySummary = ShouldPreviewCircularLetterQuality(sourceContext)
                ? BuildImportPreviewQualitySummary(readyFiles)
                : ImportPreviewQualitySummary.Empty;

            return new ImportPreviewSummary(
                source.DisplayName,
                source.SourceTypeDisplay,
                localFolderPath,
                scan.PdfFilePaths.Count,
                scan.PdfFilePaths.Count - readyFiles.Count,
                readyFiles.Count,
                scan.InvalidOrMissingFiles,
                authorName,
                readyFiles,
                metadataSamples,
                qualitySummary);
        });
    }

    private static bool ShouldPreviewCircularLetterQuality(SourceMetadataContext sourceContext)
    {
        return SermonMetadataParser.IsEwaldFrankSource(sourceContext) &&
               string.Equals(sourceContext.SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase);
    }

    private static ImportPreviewQualitySummary BuildImportPreviewQualitySummary(IReadOnlyList<string> readyFiles)
    {
        if (readyFiles.Count == 0)
        {
            return ImportPreviewQualitySummary.Empty;
        }

        var extractor = new PdfTextExtractor();
        var total = ParagraphQualitySummary.Empty;

        foreach (var filePath in readyFiles)
        {
            try
            {
                var pages = extractor.ExtractPages(filePath);
                var paragraphs = ParagraphSplitter.Split(pages);
                var filtered = CircularLetterParagraphQualityFilter.Apply(paragraphs);
                total = total.Add(filtered.Summary);
            }
            catch (Exception ex)
            {
                App.LogStartupMessage(
                    $"Brother Frank quality preview skipped {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        return new ImportPreviewQualitySummary(total);
    }

    private static ImportPreviewMetadataSample CreateImportPreviewMetadataSample(
        string filePath,
        string sourceRoot,
        ContentSourceViewModel source,
        SourceMetadataContext sourceContext,
        string authorName,
        HashSet<string> importedSet)
    {
        var fileName = Path.GetFileName(filePath);
        try
        {
            var metadata = SermonMetadataParser.Parse(filePath, sourceRoot, sourceContext);
            var detectedSourceType = DetectPreviewSourceTypeDisplay(source, metadata);
            var warning = BuildMetadataPreviewWarning(source, metadata, detectedSourceType);
            var status = !string.IsNullOrWhiteSpace(warning)
                ? "Warning"
                : importedSet.Contains(NormalizeFilePathForComparison(filePath))
                    ? "Already Imported"
                    : "Ready";

            return new ImportPreviewMetadataSample(
                fileName,
                metadata.Title,
                metadata.SermonCode,
                metadata.Year,
                authorName,
                detectedSourceType,
                status,
                warning);
        }
        catch (Exception ex)
        {
            return new ImportPreviewMetadataSample(
                fileName,
                "Could not parse metadata",
                "Unknown",
                0,
                authorName,
                source.SourceTypeDisplay,
                "Warning",
                ex.Message);
        }
    }

    private static string DetectPreviewSourceTypeDisplay(
        ContentSourceViewModel source,
        SermonMetadata metadata)
    {
        return IsCircularLetterMetadata(metadata)
            ? ContentSourceTypeOption.GetLabel("CircularLetter")
            : source.SourceTypeDisplay;
    }

    private static string BuildMetadataPreviewWarning(
        ContentSourceViewModel source,
        SermonMetadata metadata,
        string detectedSourceType)
    {
        if (string.IsNullOrWhiteSpace(metadata.Title))
        {
            return "Detected title is empty.";
        }

        if (metadata.Title.Trim().Length < 4)
        {
            return "Detected title is very short.";
        }

        if (metadata.Year <= 0)
        {
            return "Detected year is unknown.";
        }

        if (string.Equals(detectedSourceType, ContentSourceTypeOption.GetLabel("CircularLetter"), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(source.SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase))
        {
            return "Detected circular letter metadata. Use Source Type: Circular Letter for production import.";
        }

        return string.Empty;
    }

    private static PdfSourceScanResult ScanPdfFiles(string localFolderPath)
    {
        var pdfFilePaths = new List<string>();
        var invalidOrMissingFiles = new List<string>();

        if (!Directory.Exists(localFolderPath))
        {
            invalidOrMissingFiles.Add($"Missing folder: {localFolderPath}");
            return new PdfSourceScanResult(pdfFilePaths, invalidOrMissingFiles);
        }

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(localFolderPath);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        pdfFilePaths.Add(Path.GetFullPath(filePath));
                    }
                    catch (Exception ex)
                    {
                        invalidOrMissingFiles.Add($"{filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                invalidOrMissingFiles.Add($"{directory}: {ex.Message}");
            }

            try
            {
                foreach (var childDirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        if ((File.GetAttributes(childDirectory) & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }

                        pendingDirectories.Push(childDirectory);
                    }
                    catch (Exception ex)
                    {
                        invalidOrMissingFiles.Add($"{childDirectory}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                invalidOrMissingFiles.Add($"{directory}: {ex.Message}");
            }
        }

        pdfFilePaths.Sort(StringComparer.OrdinalIgnoreCase);
        return new PdfSourceScanResult(pdfFilePaths, invalidOrMissingFiles);
    }

    public async Task SearchNowAsync()
    {
        if (isSermonReadingMode)
        {
            StatusText = "Use Search Inside This Sermon, or go Back to Search Results.";
            return;
        }

        searchDebounce?.Cancel();
        await ExecuteSearchAsync(CreateSearchSnapshot(projectBestResult: false), CancellationToken.None);
    }

    public async Task QuickProjectAsync()
    {
        searchDebounce?.Cancel();
        if (await TryQuickProjectBibleReferenceAsync())
        {
            return;
        }

        await ExecuteSearchAsync(CreateSearchSnapshot(projectBestResult: true), CancellationToken.None);
    }

    private async Task SearchBibleAsync()
    {
        bibleSearchDebounce?.Cancel();
        var version = Interlocked.Increment(ref bibleSearchRequestVersion);
        await UpdateBibleNavigationAsync(BibleSearchText.Trim(), selectExactOrKeywordVerse: true, CancellationToken.None, version);
    }

    private void QueueBibleSearch()
    {
        if (!IsBibleMode || !IsBibleAvailable)
        {
            return;
        }

        bibleSearchDebounce?.Cancel();
        bibleSearchDebounce = new CancellationTokenSource();
        var cancellationToken = bibleSearchDebounce.Token;
        var queryText = BibleSearchText.Trim();
        var version = Interlocked.Increment(ref bibleSearchRequestVersion);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(180, cancellationToken);
                var operation = System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => UpdateBibleNavigationAsync(queryText, selectExactOrKeywordVerse: false, cancellationToken, version));

                await operation.Task.Unwrap();
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private bool IsCurrentBibleSearch(int version)
    {
        return version == Volatile.Read(ref bibleSearchRequestVersion);
    }

    private async Task UpdateBibleNavigationAsync(
        string queryText,
        bool selectExactOrKeywordVerse,
        CancellationToken cancellationToken,
        int version)
    {
        if (!IsBibleAvailable)
        {
            StatusText = Loc.T("Bible_NotAvailable");
            return;
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            ClearBibleNavigation(clearSelectedVerse: true);
            StatusText = Loc.T("Bible_SearchHint");
            return;
        }

        try
        {
            IsSearching = true;
            StatusText = Loc.T("Status_SearchingBible");

            var result = await BuildBibleNavigationAsync(queryText, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentBibleSearch(version))
            {
                return;
            }

            IsBibleMode = true;
            ApplyBibleNavigationResult(result, selectExactOrKeywordVerse);
            StatusText = result.Items.Count == 0
                ? Loc.T("Status_NoBibleMatches")
                : result.StatusText;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible search failed.", ex);
            ClearBibleNavigation(clearSelectedVerse: true);
            StatusText = $"Bible search failed: {ex.Message}";
        }
        finally
        {
            if (IsCurrentBibleSearch(version))
            {
                IsSearching = false;
                BibleSearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task<BibleNavigationResult> BuildBibleNavigationAsync(
        string queryText,
        CancellationToken cancellationToken)
    {
        if (TryCreateReferenceFromCurrentBook(queryText, out var selectedBookReference))
        {
            return await BuildReferenceNavigationAsync(selectedBookReference, cancellationToken);
        }

        if (BibleReferenceParser.TryParse(queryText, out var reference) && reference.IsValid)
        {
            return await BuildReferenceNavigationAsync(reference, cancellationToken);
        }

        var bookMatches = BibleReferenceParser.FindMatchingBooks(queryText, 16);
        if (bookMatches.Count > 0)
        {
            return new BibleNavigationResult(
                bookMatches
                    .Select(book => BibleNavigationItemViewModel.ForBook(book.Id, book.Name, book.ShortName, book.BookOrder))
                    .ToList(),
                [],
                Loc.F("Status_BooksFound", Loc.Count(bookMatches.Count, "Count_BibleBook_One", "Count_BibleBook_Many")),
                AutoPreviewFirstVerse: false);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var bibleSearchService = scope.ServiceProvider.GetRequiredService<IBibleSearchService>();
        var results = await bibleSearchService.SearchAsync(
            new BibleSearchQuery(queryText, SelectedBibleTranslation?.Id, 200),
            cancellationToken);
        var verses = results.Select(result => new BibleVerseResultViewModel(result)).ToList();
        return new BibleNavigationResult(
            verses.Select(BibleNavigationItemViewModel.ForVerse).ToList(),
            verses,
            Loc.F("Status_BooksFound", Loc.Count(verses.Count, "Count_BibleResult_One", "Count_BibleResult_Many")),
            AutoPreviewFirstVerse: true);
    }

    private async Task<BibleNavigationResult> BuildReferenceNavigationAsync(
        BibleReference reference,
        CancellationToken cancellationToken)
    {
        var verses = await LoadBibleVersesForReferenceAsync(reference, 200, cancellationToken);
        return new BibleNavigationResult(
            verses.Select(BibleNavigationItemViewModel.ForVerse).ToList(),
            verses,
            verses.Count == 0
                ? Loc.F("Status_VerseNotFound", Localizer.Instance.BookReference(reference.BookName, reference.Chapter, reference.Verse))
                : reference.Verse is null
                    ? Loc.F(
                        "Status_VersesFoundIn",
                        Loc.Count(verses.Count, "Count_Verse_One", "Count_Verse_Many"),
                        Localizer.Instance.BookReference(reference.BookName, reference.Chapter))
                    : Loc.F(
                        "Status_SelectedInChapterContext",
                        Localizer.Instance.BookReference(reference.BookName, reference.Chapter, reference.Verse)),
            AutoPreviewFirstVerse: reference.Verse is null,
            SelectedVerseNumber: reference.Verse);
    }

    private async Task<IReadOnlyList<BibleVerseResultViewModel>> LoadBibleVersesForReferenceAsync(
        BibleReference reference,
        int maxResults,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bibleSearchService = scope.ServiceProvider.GetRequiredService<IBibleSearchService>();
        var queryText = reference.Verse is null
            ? $"{reference.BookName} {reference.Chapter}"
            : $"{reference.BookName} {reference.Chapter}:{reference.Verse}";
        var results = await bibleSearchService.SearchAsync(
            new BibleSearchQuery(queryText, SelectedBibleTranslation?.Id, maxResults),
            cancellationToken);
        return results.Select(result => new BibleVerseResultViewModel(result)).ToList();
    }

    private async Task ShowBibleChaptersAsync(BibleNavigationItemViewModel item)
    {
        if (!item.IsBook)
        {
            return;
        }

        suppressBibleSearchQueue = true;
        BibleSearchText = $"{item.BookName} ";
        suppressBibleSearchQueue = false;

        var chapters = await LoadBibleChapterItemsAsync(item.BookName, CancellationToken.None);
        BibleResults.Clear();
        BibleNavigationItems.Clear();
        foreach (var chapter in chapters)
        {
            BibleNavigationItems.Add(chapter);
        }

        SelectedBibleNavigationItem = null;
        SelectedBibleVerse = null;
        StatusText = $"{FormatCount(chapters.Count, "chapter", "chapters")} found for {item.BookName}.";
    }

    private async Task ShowBibleChapterVersesAsync(BibleNavigationItemViewModel item)
    {
        if (!item.IsChapter || item.Chapter is null)
        {
            return;
        }

        suppressBibleSearchQueue = true;
        BibleSearchText = $"{item.BookName} {item.Chapter}";
        suppressBibleSearchQueue = false;

        var reference = new BibleReference(item.BookName, item.Chapter.Value, null, true, string.Empty);
        var verses = await LoadBibleVersesForReferenceAsync(reference, 200, CancellationToken.None);
        ApplyBibleNavigationResult(
            new BibleNavigationResult(
                verses.Select(BibleNavigationItemViewModel.ForVerse).ToList(),
                verses,
                $"{FormatCount(verses.Count, "verse", "verses")} found in {item.BookName} {item.Chapter}.",
                AutoPreviewFirstVerse: false),
            selectExactOrKeywordVerse: false);
    }

    private async Task<IReadOnlyList<BibleNavigationItemViewModel>> LoadBibleChapterItemsAsync(
        string bookName,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var chapterRows = await dbContext.BibleVerses
            .AsNoTracking()
            .Where(verse =>
                verse.BibleBook != null &&
                verse.BibleBook.Name == bookName &&
                (SelectedBibleTranslation == null || verse.TranslationId == SelectedBibleTranslation.Id))
            .GroupBy(verse => new
            {
                verse.BookId,
                verse.BibleBook!.Name,
                verse.BibleBook.BookOrder,
                verse.Chapter
            })
            .Select(group => new
            {
                group.Key.BookId,
                BookName = group.Key.Name,
                group.Key.BookOrder,
                group.Key.Chapter,
                VerseCount = group.Count()
            })
            .OrderBy(row => row.Chapter)
            .ToListAsync(cancellationToken);

        return chapterRows
            .Select(row => BibleNavigationItemViewModel.ForChapter(
                row.BookId,
                row.BookName,
                row.BookOrder,
                row.Chapter,
                row.VerseCount))
            .ToList();
    }

    private void ApplyBibleNavigationResult(
        BibleNavigationResult result,
        bool selectExactOrKeywordVerse)
    {
        BibleResults.Clear();
        foreach (var verse in result.Verses)
        {
            BibleResults.Add(verse);
        }

        BibleNavigationItems.Clear();
        foreach (var item in result.Items)
        {
            BibleNavigationItems.Add(item);
        }

        var selectedVerseItem = result.SelectedVerseNumber is null
            ? null
            : BibleNavigationItems.FirstOrDefault(item => item.Verse?.Verse == result.SelectedVerseNumber.Value);
        if (selectedVerseItem is not null)
        {
            SelectedBibleNavigationItem = selectedVerseItem;
            return;
        }

        var firstVerseItem = BibleNavigationItems.FirstOrDefault(item => item.IsVerse);
        if (result.AutoPreviewFirstVerse && (selectExactOrKeywordVerse || result.Items.Count == 1) && firstVerseItem is not null)
        {
            SelectedBibleNavigationItem = firstVerseItem;
            return;
        }

        SelectedBibleNavigationItem = null;
        SelectedBibleVerse = null;
    }

    private void ClearBibleNavigation(bool clearSelectedVerse)
    {
        BibleResults.Clear();
        BibleNavigationItems.Clear();
        SelectedBibleNavigationItem = null;
        if (clearSelectedVerse)
        {
            SelectedBibleVerse = null;
        }
    }

    private bool TryCreateReferenceFromCurrentBook(string queryText, out BibleReference reference)
    {
        reference = new BibleReference(string.Empty, 0, null, false, string.Empty);
        var selectedBook = SelectedBibleNavigationItem?.IsBook == true
            ? SelectedBibleNavigationItem.BookName
            : SelectedBibleNavigationItem?.BookName;
        if (string.IsNullOrWhiteSpace(selectedBook) ||
            !int.TryParse(queryText.Trim(), out var chapter) ||
            chapter <= 0)
        {
            return false;
        }

        reference = new BibleReference(selectedBook, chapter, null, true, string.Empty);
        return true;
    }

    public async Task ActivateSelectedBibleNavigationItemAsync()
    {
        if (!IsBibleMode)
        {
            return;
        }

        if (SelectedBibleNavigationItem is null)
        {
            await SearchBibleAsync();
            if (SelectedBibleNavigationItem is null && BibleNavigationItems.Count > 0)
            {
                SelectedBibleNavigationItem = BibleNavigationItems[0];
            }
        }

        var item = SelectedBibleNavigationItem;
        if (item is null)
        {
            return;
        }

        if (item.IsBook)
        {
            await ShowBibleChaptersAsync(item);
            return;
        }

        if (item.IsChapter)
        {
            await ShowBibleChapterVersesAsync(item);
            return;
        }

        if (item.Verse is not null)
        {
            SelectedBibleVerse = item.Verse;
            StatusText = $"Selected {item.Verse.ReferenceDisplay}.";
        }
    }

    private async Task<bool> TryQuickProjectBibleReferenceAsync()
    {
        var queryText = SearchText.Trim();
        if (!IsBibleAvailable ||
            string.IsNullOrWhiteSpace(queryText) ||
            !BibleReferenceParser.TryParse(queryText, out var reference) ||
            !reference.IsValid ||
            reference.Verse is null)
        {
            return false;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var bibleSearchService = scope.ServiceProvider.GetRequiredService<IBibleSearchService>();
            var results = await bibleSearchService.SearchAsync(
                new BibleSearchQuery(queryText, SelectedBibleTranslation?.Id, 1));
            var result = results.FirstOrDefault();
            if (result is null)
            {
                StatusText = "No matching Bible verse found.";
                return true;
            }

            suppressBibleSearchQueue = true;
            BibleSearchText = queryText;
            suppressBibleSearchQueue = false;
            BibleResults.Clear();
            BibleNavigationItems.Clear();
            var verse = new BibleVerseResultViewModel(result);
            BibleResults.Add(verse);
            var navigationItem = BibleNavigationItemViewModel.ForVerse(verse);
            BibleNavigationItems.Add(navigationItem);
            IsBibleMode = true;
            SelectedBibleVerse = verse;
            SelectedBibleNavigationItem = navigationItem;
            await ProjectCurrentBibleSelectionAsync();
            return true;
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible quick project failed.", ex);
            StatusText = $"Bible quick project failed: {ex.Message}";
            return true;
        }
    }

    private void QueueSongSearch()
    {
        if (!IsSongsMode)
        {
            return;
        }

        songSearchDebounce?.Cancel();
        songSearchDebounce = new CancellationTokenSource();
        var cancellationToken = songSearchDebounce.Token;
        var queryText = SongSearchText.Trim();
        var version = Interlocked.Increment(ref songSearchRequestVersion);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
                var operation = System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => UpdateSongResultsAsync(queryText, cancellationToken, version));

                await operation.Task.Unwrap();
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private bool IsCurrentSongSearch(int version)
    {
        return version == Volatile.Read(ref songSearchRequestVersion);
    }

    private async Task UpdateSongResultsAsync(
        string queryText,
        CancellationToken cancellationToken,
        int version)
    {
        if (!IsCurrentSongSearch(version) || !IsSongsMode)
        {
            return;
        }

        try
        {
            IsSearching = true;
            StatusText = string.IsNullOrWhiteSpace(queryText)
                ? Loc.T("Status_LoadingSongs")
                : Loc.T("Status_SearchingSongs");

            await using var scope = scopeFactory.CreateAsyncScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISongSearchService>();
            var language = ContentLanguageCode;
            var results = await searchService.SearchAsync(
                new SongSearchQuery(queryText, 200, language),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSongSearch(version) || !IsSongsMode)
            {
                return;
            }

            SongResults.Clear();
            foreach (var result in results.Select(result => new SongResultViewModel(result)))
            {
                SongResults.Add(result);
            }

            SelectedSong = SongResults.FirstOrDefault();
            StatusText = SongResults.Count == 0
                ? HasSongContentForCurrentLanguage
                    ? Loc.T("Status_NoSongsFound")
                    : Loc.F("Song_NoContentForLanguage", SelectedUiLanguage.NativeName)
                : Loc.Count(SongResults.Count, "Count_Song_One", "Count_Song_Many") + ".";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrentSongSearch(version))
            {
                SongResults.Clear();
                SongSections.Clear();
                SelectedSong = null;
                if (IsSongsMode)
                {
                    StatusText = $"Song search failed: {ex.Message}";
                }
            }
        }
        finally
        {
            if (IsCurrentSongSearch(version))
            {
                IsSearching = false;
            }
        }
    }

    private async Task LoadSongSectionsAsync(SongResultViewModel song)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var sections = await dbContext.SongSections
                .AsNoTracking()
                .Where(section => section.SongId == song.SongId)
                .OrderBy(section => section.SectionOrder)
                .Select(section => new SongSectionViewModel(
                    section.Id,
                    section.SongId,
                    section.SectionOrder,
                    section.SectionType,
                    section.SectionLabel,
                    section.Text))
                .ToListAsync();

            if (SelectedSong?.SongId != song.SongId)
            {
                return;
            }

            SongSections.Clear();
            foreach (var section in sections)
            {
                SongSections.Add(section);
            }

            SelectedSongSection = song.MatchedSectionId is null
                ? SongSections.FirstOrDefault()
                : SongSections.FirstOrDefault(section => section.SectionId == song.MatchedSectionId.Value) ??
                  SongSections.FirstOrDefault();

            if (IsSongsMode)
            {
                StatusText = SelectedSongSection is null
                    ? $"No lyric sections found for {song.Title}."
                    : $"Selected {SelectedSongSection.SectionLabel}.";
            }
        }
        catch (Exception ex)
        {
            App.LogStartupError("Song section load failed.", ex);
            SongSections.Clear();
            SelectedSongSection = null;
            if (IsSongsMode)
            {
                StatusText = $"Could not load song sections: {ex.Message}";
            }
        }
    }

    private void QueueSearch()
    {
        if (isSermonReadingMode || IsSongsMode || IsBibleMode)
        {
            return;
        }

        searchDebounce?.Cancel();
        searchDebounce = new CancellationTokenSource();
        var cancellationToken = searchDebounce.Token;
        var snapshot = CreateSearchSnapshot(projectBestResult: false);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
                var operation = System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => ExecuteSearchAsync(snapshot, cancellationToken));

                await operation.Task.Unwrap();
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private void ExitSermonReadingModeForGlobalSearch()
    {
        if (!isSermonReadingMode)
        {
            return;
        }

        sermonWithinSearchDebounce?.Cancel();
        Interlocked.Increment(ref sermonWithinSearchRequestVersion);
        sermonWithinMatches = [];
        sermonWithinSearchAppliedText = string.Empty;
        sermonWithinSearchAppliedSermonId = null;
        sermonWithinMatchIndex = -1;
        if (!string.IsNullOrEmpty(sermonWithinSearchText))
        {
            sermonWithinSearchText = string.Empty;
            OnPropertyChanged(nameof(SermonWithinSearchText));
        }

        sermonWithinSearchStatus = "Find in selected Sermon.";
        OnPropertyChanged(nameof(SermonWithinSearchStatus));
        OnPropertyChanged(nameof(FocusedSermonMatchCount));

        isSermonReadingMode = false;
        focusedSermonId = null;
        focusedSermon = null;
        focusedSermonParagraphs = [];
        selectedParagraph = null;
        selectedSermon = null;
        ParagraphResults.Clear();
        SermonResults.Clear();
        allParagraphResults = [];
        ResultCount = 0;
        OnPropertyChanged(nameof(SelectedParagraph));
        OnPropertyChanged(nameof(SelectedSermon));
        OnPropertyChanged(nameof(IsSermonReadingMode));
        OnPropertyChanged(nameof(IsSermonGlobalSearchMode));
        OnPropertyChanged(nameof(FocusedSermonId));
        OnPropertyChanged(nameof(FocusedSermonParagraphCount));
        OnPropertyChanged(nameof(SermonReadingHeader));
        OnPropertyChanged(nameof(CenterPanelTitle));
        OnPropertyChanged(nameof(SermonHighlightQuery));
        OnPropertyChanged(nameof(PreviewHighlightQuery));
        RaiseSermonWithinSearchCommandStates();
        BackToSermonSearchResultsCommand.RaiseCanExecuteChanged();
        OpenSermonCommand.RaiseCanExecuteChanged();
        RaiseCommandStates();
    }

    private SearchSnapshot CreateSearchSnapshot(bool projectBestResult)
    {
        return new SearchSnapshot(
            SearchText.Trim(),
            SelectedAuthor?.Value,
            SelectedSourceFilter?.Value,
            SelectedYear?.Value,
            Interlocked.Increment(ref searchRequestVersion),
            projectBestResult,
            ContentLanguageCode);
    }

    private bool IsCurrentSearch(SearchSnapshot snapshot)
    {
        return snapshot.Version == Volatile.Read(ref searchRequestVersion);
    }

    private async Task ExecuteSearchAsync(
        SearchSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!IsCurrentSearch(snapshot))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(snapshot.QueryText) && !snapshot.HasFilter)
        {
            SetResults([], isSermonBrowseMode: false);
            StatusText = snapshot.ProjectBestResult
                ? Loc.T("Status_NoMatchingParagraph")
                : Loc.T("Sermon_SearchHintShort");
            IsSearching = false;
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            IsSearching = true;
            StatusText = Loc.T("Status_Searching");

            var resultViewModels = await LoadSearchResultsAsync(snapshot, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentSearch(snapshot))
            {
                return;
            }

            var preferredParagraphId = resultViewModels.FirstOrDefault()?.ParagraphId;

            SetResults(resultViewModels, preferredParagraphId, snapshot.IsFilterOnlyBrowse);

            if (snapshot.ProjectBestResult)
            {
                if (SelectedParagraph is null)
                {
                    StatusText = "No matching paragraph found.";
                    return;
                }

                await ProjectCurrentSelectionAsync(recordHistory: !IsProjectionOpen);
                return;
            }

            StatusText = BuildSearchStatus(snapshot, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrentSearch(snapshot))
            {
                SetResults([], isSermonBrowseMode: false);
                StatusText = $"Search failed: {ex.Message}";
            }
        }
        finally
        {
            if (IsCurrentSearch(snapshot))
            {
                IsSearching = false;
            }
        }
    }

    private Task<List<ParagraphResultViewModel>> LoadSearchResultsAsync(
        SearchSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISermonSearchService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var language = snapshot.Language;

            IReadOnlyList<SearchResult> results;
            if (snapshot.IsFilterOnlyBrowse)
            {
                results = await searchService.BrowseSermonsAsync(
                    snapshot.AuthorId,
                    snapshot.ContentSourceId,
                    snapshot.Year,
                    maxResults: 2000,
                    cancellationToken: cancellationToken,
                    language: language);
            }
            else if (snapshot.HasFilter)
            {
                results = await searchService.SearchAsync(
                    new SermonSearchQuery(
                        AuthorId: snapshot.AuthorId,
                        ContentSourceId: snapshot.ContentSourceId,
                        SearchText: string.IsNullOrWhiteSpace(snapshot.QueryText) ? null : snapshot.QueryText,
                        Year: snapshot.Year,
                        MaxResults: 200,
                        Language: language),
                    cancellationToken);
            }
            else
            {
                results = await searchService.SearchAsync(
                    snapshot.QueryText,
                    200,
                    cancellationToken,
                    language);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var viewModels = results
                .Where(result => !IsHiddenFromChurchUi(
                    result.SourceDisplayName,
                    result.AuthorDisplayName,
                    result.SourceFilePath))
                .Select(result => new ParagraphResultViewModel(result))
                .ToList();
            await ApplyFavoriteStateAsync(dbContext, viewModels, cancellationToken);
            return viewModels;
        }, cancellationToken);
    }

    private void SetResults(
        List<ParagraphResultViewModel> results,
        int? preferredParagraphId = null,
        bool isSermonBrowseMode = false)
    {
        if (isSermonReadingMode)
        {
            return;
        }

        IsSermonReadingMode = false;
        var browseModeChanged = this.isSermonBrowseMode != isSermonBrowseMode;
        this.isSermonBrowseMode = isSermonBrowseMode;
        allParagraphResults = results;
        ResultCount = allParagraphResults.Count;
        if (browseModeChanged)
        {
            OnPropertyChanged(nameof(LibraryCountText));
        }

        var preferredParagraph = preferredParagraphId is null
            ? allParagraphResults.FirstOrDefault()
            : allParagraphResults.FirstOrDefault(paragraph => paragraph.ParagraphId == preferredParagraphId.Value);

        SermonResults.Clear();
        foreach (var item in allParagraphResults
                     .Select((result, index) => new { Result = result, Index = index })
                     .GroupBy(item => item.Result.SermonId)
                     .Select(group => new
                     {
                         Rank = group.Min(item => item.Index),
                          Sermon = new SermonResultViewModel(
                              group.Key,
                              group.First().Result.SermonTitle,
                              group.First().Result.SermonCode,
                              group.First().Result.Year,
                              group.Count(),
                              group.First().Result.AuthorDisplayName,
                              group.First().Result.SourceDisplayName,
                              group.First().Result.SourceType,
                              group.First().Result.ParagraphTextPreview)
                      })
                     .OrderBy(item => item.Rank))
        {
            SermonResults.Add(item.Sermon);
        }

        var nextSermon = preferredParagraph is null
            ? SermonResults.FirstOrDefault()
            : SermonResults.FirstOrDefault(sermon => sermon.SermonId == preferredParagraph.SermonId);

        if (SelectedSermon == nextSermon)
        {
            RefreshParagraphResultsForSelectedSermon(preferredParagraphId);
            return;
        }

        SelectedSermon = nextSermon;
        if (preferredParagraphId is not null)
        {
            RefreshParagraphResultsForSelectedSermon(preferredParagraphId);
        }
    }

    private string BuildSearchStatus(SearchSnapshot snapshot, long elapsedMilliseconds)
    {
        if (snapshot.IsFilterOnlyBrowse)
        {
            return Loc.F(
                "Status_BrowseFound",
                Loc.Count(ResultCount, "Count_Document_One", "Count_Document_Many"),
                elapsedMilliseconds.ToString("N0"));
        }

        var documentCount = SermonResults.Count;
        if (documentCount == 0)
        {
            if (!HasSermonContentForCurrentLanguage)
            {
                return Loc.F("Sermon_NoContentForLanguage", SelectedUiLanguage.NativeName);
            }

            var noMatchKind = SermonTextSearchPattern.Create(snapshot.QueryText).IsExactPhrase
                ? Loc.T("Status_NoExactPhrase")
                : Loc.T("Count_NoResults");
            return Loc.F("Status_SearchNoMatch", noMatchKind, elapsedMilliseconds.ToString("N0"));
        }

        var pattern = SermonTextSearchPattern.Create(snapshot.QueryText);
        var matchKind = pattern.IsExactPhrase
            ? Loc.T("Status_ExactPhrase")
            : pattern.Terms.Count > 1
                ? Loc.T("Status_AllWords")
                : Loc.T("Status_Match");

        return Loc.F(
            "Status_SearchFound",
            matchKind,
            Loc.Count(documentCount, "Count_Document_One", "Count_Document_Many"),
            Loc.Count(ResultCount, "Count_Paragraph_One", "Count_Paragraph_Many"),
            elapsedMilliseconds.ToString("N0"));
    }

    private void RefreshParagraphResultsForSelectedSermon(int? preferredParagraphId = null)
    {
        ParagraphResults.Clear();

        var paragraphs = SelectedSermon is null
            ? allParagraphResults
            : allParagraphResults.Where(paragraph => paragraph.SermonId == SelectedSermon.SermonId);

        foreach (var paragraph in paragraphs.OrderBy(paragraph => paragraph.ParagraphNumber))
        {
            ParagraphResults.Add(paragraph);
        }

        SelectedParagraph = preferredParagraphId is null
            ? ParagraphResults.FirstOrDefault()
            : ParagraphResults.FirstOrDefault(paragraph => paragraph.ParagraphId == preferredParagraphId.Value) ??
              ParagraphResults.FirstOrDefault();

        if (isSermonReadingMode)
        {
            QueueSermonWithinSearch();
        }
    }

    private void QueueSermonWithinSearch()
    {
        sermonWithinSearchDebounce?.Cancel();
        var queryText = SermonWithinSearchText;
        var version = Interlocked.Increment(ref sermonWithinSearchRequestVersion);
        InvalidateSermonWithinSearchResults(queryText);

        if (string.IsNullOrWhiteSpace(queryText) || focusedSermonId is null || !isSermonReadingMode)
        {
            return;
        }

        sermonWithinSearchDebounce = new CancellationTokenSource();
        var cancellationToken = sermonWithinSearchDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(220, cancellationToken);
                var operation = System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => ApplySermonWithinSearchAsync(queryText, version, cancellationToken));

                await operation.Task.Unwrap();
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private async Task ApplySermonWithinSearchAsync(
        string queryText,
        int version,
        CancellationToken cancellationToken = default)
    {
        if (version != Volatile.Read(ref sermonWithinSearchRequestVersion))
        {
            return;
        }

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        sermonWithinMatches = [];
        sermonWithinMatchIndex = -1;
        OnPropertyChanged(nameof(FocusedSermonMatchCount));

        var focusedId = focusedSermonId;
        var readingSermon = focusedSermon;
        if (focusedId is null || readingSermon is null || !isSermonReadingMode)
        {
            SermonWithinSearchStatus = "Open a Sermon to search inside it.";
            RaiseSermonWithinSearchCommandStates();
            return;
        }

        var query = SermonTextSearchPattern.Create(queryText);
        if (query.IsEmpty)
        {
            SermonWithinSearchStatus = "Find in selected Sermon.";
            RaiseSermonWithinSearchCommandStates();
            return;
        }

        if (version != Volatile.Read(ref sermonWithinSearchRequestVersion) ||
            focusedSermonId != focusedId ||
            !isSermonReadingMode)
        {
            return;
        }

        sermonWithinSearchAppliedText = queryText;
        sermonWithinSearchAppliedSermonId = focusedId;
        sermonWithinMatches = focusedSermonParagraphs
            .Where(paragraph => paragraph.SermonId == focusedId.Value)
            .Select(paragraph => new
            {
                Paragraph = paragraph,
                Match = query.Match(paragraph.FullParagraphText)
            })
            .Where(item => item.Match is not null)
            .OrderBy(item => item.Match!.Value.Score)
            .ThenBy(item => item.Paragraph.ParagraphNumber)
            .Select(item => item.Paragraph)
            .ToList();
        OnPropertyChanged(nameof(FocusedSermonMatchCount));

        if (sermonWithinMatches.Count == 0)
        {
            SermonWithinSearchStatus = $"0 matches in this Sermon - {BuildSermonIdentity(readingSermon)}.";
            RaiseSermonWithinSearchCommandStates();
            return;
        }

        var selectedIndex = SelectedParagraph is null
            ? -1
            : sermonWithinMatches.FindIndex(match => match.ParagraphId == SelectedParagraph.ParagraphId);
        sermonWithinMatchIndex = selectedIndex >= 0 ? selectedIndex : 0;
        SelectCurrentSermonWithinMatch();
        RaiseSermonWithinSearchCommandStates();
    }

    private void InvalidateSermonWithinSearchResults(string queryText)
    {
        sermonWithinMatches = [];
        sermonWithinSearchAppliedText = string.Empty;
        sermonWithinSearchAppliedSermonId = null;
        sermonWithinMatchIndex = -1;

        OnPropertyChanged(nameof(FocusedSermonMatchCount));
        SermonWithinSearchStatus = focusedSermonId is null
            ? "Open a Sermon to search inside it."
            : string.IsNullOrWhiteSpace(queryText)
                ? "Find in selected Sermon."
                : "Searching selected Sermon...";

        RaiseSermonWithinSearchCommandStates();
    }

    private async Task<bool> EnsureCurrentSermonWithinMatchesAsync()
    {
        if (!IsCurrentSermonWithinSearchResultSet())
        {
            await ApplySermonWithinSearchNowAsync();
        }

        return IsCurrentSermonWithinSearchResultSet() && sermonWithinMatches.Count > 0;
    }

    private async Task ApplySermonWithinSearchNowAsync()
    {
        sermonWithinSearchDebounce?.Cancel();
        var version = Interlocked.Increment(ref sermonWithinSearchRequestVersion);
        await ApplySermonWithinSearchAsync(SermonWithinSearchText, version);
    }

    private bool CanNavigateSermonWithinMatches()
    {
        return IsCurrentSermonWithinSearchResultSet() && sermonWithinMatches.Count > 0;
    }

    private bool IsCurrentSermonWithinSearchResultSet()
    {
        return string.Equals(sermonWithinSearchAppliedText, SermonWithinSearchText, StringComparison.Ordinal) &&
               sermonWithinSearchAppliedSermonId == focusedSermonId &&
               isSermonReadingMode;
    }

    private async Task SelectNextSermonWithinMatchAsync()
    {
        if (!await EnsureCurrentSermonWithinMatchesAsync())
        {
            return;
        }

        sermonWithinMatchIndex = (sermonWithinMatchIndex + 1) % sermonWithinMatches.Count;
        SelectCurrentSermonWithinMatch();
    }

    private async Task SelectPreviousSermonWithinMatchAsync()
    {
        if (!await EnsureCurrentSermonWithinMatchesAsync())
        {
            return;
        }

        sermonWithinMatchIndex = sermonWithinMatchIndex <= 0
            ? sermonWithinMatches.Count - 1
            : sermonWithinMatchIndex - 1;
        SelectCurrentSermonWithinMatch();
    }

    private void SelectCurrentSermonWithinMatch()
    {
        if (sermonWithinMatchIndex < 0 || sermonWithinMatchIndex >= sermonWithinMatches.Count)
        {
            return;
        }

        var match = sermonWithinMatches[sermonWithinMatchIndex];
        if (focusedSermonId is null || match.SermonId != focusedSermonId.Value)
        {
            return;
        }

        SelectedParagraph = focusedSermonParagraphs.FirstOrDefault(paragraph => paragraph.ParagraphId == match.ParagraphId) ?? match;
        SermonWithinSearchStatus = BuildSermonWithinMatchStatus(SelectedParagraph);
    }

    private void ReplaceParagraphResultsForSelectedSermon(
        IReadOnlyList<ParagraphResultViewModel> paragraphs,
        int? preferredParagraphId)
    {
        ParagraphResults.Clear();
        foreach (var paragraph in paragraphs.OrderBy(paragraph => paragraph.ParagraphNumber))
        {
            ParagraphResults.Add(paragraph);
        }

        if (ParagraphResults.Count == 0)
        {
            SelectedParagraph = null;
            return;
        }

        SelectedParagraph = preferredParagraphId is null
            ? SelectedParagraph is not null
                ? ParagraphResults.FirstOrDefault(paragraph => paragraph.ParagraphId == SelectedParagraph.ParagraphId) ??
                  ParagraphResults.FirstOrDefault()
                : ParagraphResults.FirstOrDefault()
            : ParagraphResults.FirstOrDefault(paragraph => paragraph.ParagraphId == preferredParagraphId.Value) ??
              ParagraphResults.FirstOrDefault();
    }

    private string BuildSermonWithinMatchStatus(ParagraphResultViewModel paragraph)
    {
        var matchCountText = $"{sermonWithinMatches.Count:N0} {(sermonWithinMatches.Count == 1 ? "match" : "matches")}";
        return $"{matchCountText} - Match {sermonWithinMatchIndex + 1:N0} of {sermonWithinMatches.Count:N0} - " +
               $"Paragraph {paragraph.ParagraphNumber} - {BuildSermonIdentity(paragraph)}: {CreateSearchStatusSnippet(paragraph.FullParagraphText, SermonWithinSearchText)}";
    }

    private static string BuildSermonIdentity(SermonResultViewModel sermon)
    {
        var parts = new List<string> { sermon.Title };
        if (!string.IsNullOrWhiteSpace(sermon.SermonCode))
        {
            parts.Add(sermon.SermonCode);
        }

        if (sermon.Year > 0)
        {
            parts.Add(sermon.Year.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(" | ", parts);
    }

    private static string BuildSermonIdentity(ParagraphResultViewModel paragraph)
    {
        var parts = new List<string> { paragraph.SermonTitle };
        if (!string.IsNullOrWhiteSpace(paragraph.SermonCode))
        {
            parts.Add(paragraph.SermonCode);
        }

        if (paragraph.Year > 0)
        {
            parts.Add(paragraph.Year.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(" | ", parts);
    }

    private static string CreateSearchStatusSnippet(string text)
    {
        var snippet = string.Join(' ', text.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return snippet.Length <= 110 ? snippet : $"{snippet[..110].TrimEnd()}...";
    }

    private static string CreateSearchStatusSnippet(string text, string queryText)
    {
        var terms = SermonTextSearchPattern.TokenizeNormalized(SermonTextSearchPattern.NormalizeForSearch(queryText));
        var firstTerm = terms.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstTerm))
        {
            return CreateSearchStatusSnippet(text);
        }

        var index = text.IndexOf(firstTerm, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return CreateSearchStatusSnippet(text);
        }

        var start = Math.Max(0, index - 55);
        var length = Math.Min(170, text.Length - start);
        var snippet = string.Join(' ', text.Substring(start, length).Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return $"{(start > 0 ? "..." : string.Empty)}{snippet}{(start + length < text.Length ? "..." : string.Empty)}";
    }

    private void ClearSermonWithinSearch()
    {
        ClearSermonWithinSearch(resetText: true);
    }

    private void ClearSermonWithinSearch(bool resetText)
    {
        sermonWithinSearchDebounce?.Cancel();
        Interlocked.Increment(ref sermonWithinSearchRequestVersion);
        sermonWithinMatches = [];
        sermonWithinSearchAppliedText = string.Empty;
        sermonWithinSearchAppliedSermonId = null;
        sermonWithinMatchIndex = -1;
        OnPropertyChanged(nameof(FocusedSermonMatchCount));
        SermonWithinSearchStatus = focusedSermonId is null
            ? "Open a Sermon to search inside it."
            : "Find in selected Sermon.";

        if (resetText && !string.IsNullOrEmpty(sermonWithinSearchText))
        {
            sermonWithinSearchText = string.Empty;
            OnPropertyChanged(nameof(SermonWithinSearchText));
        }

        RaiseSermonWithinSearchCommandStates();
    }

    private async Task OpenSermonAsync(SermonResultViewModel? sermon)
    {
        sermon ??= SelectedSermon;
        if (sermon is null)
        {
            StatusText = "Select a Sermon, then choose Open Sermon.";
            return;
        }

        if (SelectedSermon?.SermonId != sermon.SermonId)
        {
            SelectedSermon = sermon;
        }

        var preferredParagraphId = SelectedParagraph?.SermonId == sermon.SermonId
            ? SelectedParagraph.ParagraphId
            : (int?)null;

        searchDebounce?.Cancel();
        Interlocked.Increment(ref searchRequestVersion);
        focusedSermonId = sermon.SermonId;
        focusedSermon = sermon;
        focusedSermonParagraphs = [];
        OnPropertyChanged(nameof(FocusedSermonId));
        OnPropertyChanged(nameof(FocusedSermonParagraphCount));
        OnPropertyChanged(nameof(SermonReadingHeader));
        IsSermonReadingMode = true;
        ClearSermonWithinSearch(resetText: true);

        try
        {
            var paragraphs = await LoadSermonParagraphsAsync(sermon.SermonId);
            if (focusedSermonId != sermon.SermonId || !isSermonReadingMode)
            {
                return;
            }

            focusedSermonParagraphs = paragraphs
                .Where(paragraph => paragraph.SermonId == sermon.SermonId)
                .OrderBy(paragraph => paragraph.ParagraphNumber)
                .ThenBy(paragraph => paragraph.ParagraphId)
                .ToList();
            OnPropertyChanged(nameof(FocusedSermonParagraphCount));
            ReplaceParagraphResultsForSelectedSermon(focusedSermonParagraphs, preferredParagraphId);
            StatusText = ParagraphResults.Count == 0
                ? $"{sermon.Title} has no paragraphs."
                : $"Reading {sermon.Title} - {ParagraphResults.Count:N0} paragraphs.";
        }
        catch (Exception ex)
        {
            focusedSermonId = null;
            focusedSermon = null;
            focusedSermonParagraphs = [];
            IsSermonReadingMode = false;
            OnPropertyChanged(nameof(FocusedSermonId));
            OnPropertyChanged(nameof(FocusedSermonParagraphCount));
            OnPropertyChanged(nameof(SermonReadingHeader));
            StatusText = $"Could not open Sermon: {ex.Message}";
        }
    }

    private void BackToSermonSearchResults()
    {
        if (!IsSermonReadingMode)
        {
            return;
        }

        var sermonId = focusedSermonId;
        var selectedParagraphId = SelectedParagraph?.ParagraphId;
        ClearSermonWithinSearch(resetText: true);
        IsSermonReadingMode = false;
        focusedSermonId = null;
        focusedSermon = null;
        focusedSermonParagraphs = [];
        OnPropertyChanged(nameof(FocusedSermonId));
        OnPropertyChanged(nameof(FocusedSermonParagraphCount));
        OnPropertyChanged(nameof(SermonReadingHeader));

        var preferredMatchId = selectedParagraphId is not null &&
                               allParagraphResults.Any(paragraph => paragraph.ParagraphId == selectedParagraphId.Value)
            ? selectedParagraphId
            : allParagraphResults.FirstOrDefault(paragraph => paragraph.SermonId == sermonId)?.ParagraphId;
        RefreshParagraphResultsForSelectedSermon(preferredMatchId);
        StatusText = string.IsNullOrWhiteSpace(SearchText)
            ? "Back to Sermon results."
            : $"Back to results for \"{SearchText.Trim()}\".";
    }

    private async void SelectPreviousParagraph()
    {
        if (IsBibleMode)
        {
            await MoveBibleSelectionAsync(-1);
            return;
        }

        if (IsSongsMode)
        {
            MoveSongSection(-1);
            return;
        }

        await MoveSelectionAsync(-1);
    }

    private async void SelectNextParagraph()
    {
        if (IsBibleMode)
        {
            await MoveBibleSelectionAsync(1);
            return;
        }

        if (IsSongsMode)
        {
            MoveSongSection(1);
            return;
        }

        await MoveSelectionAsync(1);
    }

    private Task MoveSelectionAsync(int offset)
    {
        var currentParagraph = SelectedParagraph;
        if (currentParagraph is null || focusedSermonId is null || currentParagraph.SermonId != focusedSermonId.Value)
        {
            return Task.CompletedTask;
        }

        var shouldUpdateLive = ProjectionNavigationRules.ShouldUpdateLiveSermonParagraph(
                                   IsProjectionOpen,
                                   ActiveProjectionContent,
                                   focusedSermonId.Value,
                                   currentParagraph.ParagraphId);
        var currentIndex = focusedSermonParagraphs.FindIndex(paragraph => paragraph.ParagraphId == currentParagraph.ParagraphId);
        var nextIndex = currentIndex + offset;
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= focusedSermonParagraphs.Count)
        {
            StatusText = offset > 0
                ? "Already at the last paragraph."
                : "Already at the first paragraph.";
            return Task.CompletedTask;
        }

        SelectedParagraph = focusedSermonParagraphs[nextIndex];
        if (shouldUpdateLive)
        {
            ActiveProjectionContent = CreateSermonSnapshot(SelectedParagraph);
            StatusText = $"Projecting Paragraph {SelectedParagraph.ParagraphNumber}.";
        }
        else
        {
            StatusText = $"Selected Paragraph {SelectedParagraph.ParagraphNumber}.";
        }

        return Task.CompletedTask;
    }

    private async Task MoveBibleSelectionAsync(int offset)
    {
        var currentVerse = SelectedBibleVerse;
        if (currentVerse is null)
        {
            return;
        }

        var shouldUpdateLive = ProjectionNavigationRules.ShouldUpdateLiveBibleVerse(
            IsProjectionOpen,
            ActiveProjectionContent,
            currentVerse.TranslationId,
            currentVerse.TranslationAbbreviation,
            currentVerse.VerseId);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var verseQuery = dbContext.BibleVerses
                .AsNoTracking()
                .Include(verse => verse.BibleBook)
                .Include(verse => verse.BibleTranslation)
                .Where(verse =>
                    verse.TranslationId == currentVerse.TranslationId &&
                    verse.BookId == currentVerse.BookId &&
                    verse.Chapter == currentVerse.Chapter);

            verseQuery = offset > 0
                ? verseQuery.Where(verse => verse.Verse > currentVerse.Verse)
                    .OrderBy(verse => verse.Verse)
                : verseQuery.Where(verse => verse.Verse < currentVerse.Verse)
                    .OrderByDescending(verse => verse.Verse);

            var adjacentVerse = await verseQuery
                .Select(verse => new BibleSearchResult(
                    verse.Id,
                    verse.TranslationId,
                    verse.BibleTranslation!.Name,
                    verse.BibleTranslation.Abbreviation,
                    verse.BookId,
                    verse.BibleBook!.Name,
                    verse.BibleBook.BookOrder,
                    verse.Chapter,
                    verse.Verse,
                    verse.Text))
                .FirstOrDefaultAsync();

            if (adjacentVerse is null)
            {
                StatusText = offset > 0
                    ? "Already at the last verse."
                    : "Already at the first verse.";
                return;
            }

            var viewModel = new BibleVerseResultViewModel(adjacentVerse);
            var existing = BibleResults.FirstOrDefault(result => result.VerseId == viewModel.VerseId);
            if (existing is null)
            {
                BibleResults.Add(viewModel);
                existing = viewModel;
            }

            var existingNavigationItem = BibleNavigationItems.FirstOrDefault(item => item.Verse?.VerseId == existing.VerseId);
            if (existingNavigationItem is null)
            {
                existingNavigationItem = BibleNavigationItemViewModel.ForVerse(existing);
                BibleNavigationItems.Add(existingNavigationItem);
            }

            SelectedBibleNavigationItem = existingNavigationItem;
            SelectedBibleVerse = existing;
            if (shouldUpdateLive)
            {
                ActiveProjectionContent = CreateBibleSnapshot(existing);
                StatusText = $"Projecting {existing.ReferenceDisplay} ({existing.TranslationAbbreviation}).";
            }
            else
            {
                StatusText = $"Selected {SelectedBibleVerse.ReferenceDisplay}.";
            }
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible verse navigation failed.", ex);
            StatusText = $"Could not move Bible selection: {ex.Message}";
        }
    }

    private void MoveSongSection(int offset)
    {
        if (SelectedSongSection is null || SongSections.Count == 0)
        {
            return;
        }

        var currentSection = SelectedSongSection;
        var shouldUpdateLive = SelectedSong is not null &&
                               ProjectionNavigationRules.ShouldUpdateLiveSongSection(
                                   IsProjectionOpen,
                                   ActiveProjectionContent,
                                   SelectedSong.SongId,
                                   SelectedSong.Title,
                                   currentSection.SectionId);

        var currentIndex = SongSections.IndexOf(currentSection);
        var nextIndex = currentIndex < 0
            ? 0
            : Math.Clamp(currentIndex + offset, 0, SongSections.Count - 1);

        if (nextIndex == currentIndex)
        {
            StatusText = offset > 0
                ? "Already at the last song section."
                : "Already at the first song section.";
            return;
        }

        SelectedSongSection = SongSections[nextIndex];
        if (shouldUpdateLive && SelectedSong is not null)
        {
            ActiveProjectionContent = CreateSongSnapshot(SelectedSong, SelectedSongSection);
            StatusText = $"Projecting {SelectedSong.Title} - {SelectedSongSection.SectionLabel}.";
        }
        else
        {
            StatusText = $"Selected {SelectedSongSection.SectionLabel}.";
        }
    }

    private void CopySelectedParagraph()
    {
        if (IsBibleMode)
        {
            if (SelectedBibleVerse is null)
            {
                StatusText = "Please select a Bible verse before copying.";
                return;
            }

            Clipboard.SetText(
                $"{SelectedBibleVerse.ReferenceDisplay} {SelectedBibleVerse.TranslationAbbreviation}{Environment.NewLine}{SelectedBibleVerse.Text}");
            StatusText = "Bible verse copied.";
            return;
        }

        if (IsSongsMode)
        {
            if (SelectedSong is null || SelectedSongSection is null)
            {
                StatusText = "Please select a song section before copying.";
                return;
            }

            Clipboard.SetText(
                $"{SelectedSong.Title} - {SelectedSongSection.SectionLabel}{Environment.NewLine}{SelectedSongSection.Text}");
            StatusText = "Song section copied.";
            return;
        }

        if (SelectedParagraph is null)
        {
            return;
        }

        Clipboard.SetText(SelectedParagraph.FullParagraphText);
        StatusText = "Paragraph copied.";
    }

    private async void ProjectSelectedParagraph()
    {
        if (IsBibleMode)
        {
            await ProjectCurrentBibleSelectionAsync();
            return;
        }

        if (IsSongsMode)
        {
            await ProjectCurrentSongSelectionAsync();
            return;
        }

        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        await ProjectCurrentSelectionAsync(recordHistory: true);
    }

    public void SetProjectionOpen(bool isOpen, ProjectionDisplayTarget? displayTarget = null)
    {
        if (displayTarget is not null &&
            !string.Equals(projectionOpenDisplayText, displayTarget.StatusDisplayName, StringComparison.Ordinal))
        {
            projectionOpenDisplayText = displayTarget.StatusDisplayName;
            OnPropertyChanged(nameof(ProjectionStatusText));
        }

        IsProjectionOpen = isOpen;

        if (!isOpen)
        {
            ActiveProjectionContent = null;
            ClearProjectionPageState();
            if (StatusText.StartsWith("Projecting ", StringComparison.Ordinal))
            {
                StatusText = "Ready";
            }
        }
    }

    public void ReportProjectionOpened(
        ProjectionDisplayTarget displayTarget,
        bool isTest,
        bool isWindowedPreview = false,
        bool isAdaptiveWindowed = false)
    {
        SetProjectionOpen(true, displayTarget);

        var messagePrefix = isTest
            ? isWindowedPreview ? "Projection test preview opened" : "Projection test opened"
            : isWindowedPreview
                ? "Projection preview opened"
                : isAdaptiveWindowed
                    ? "Projection opened in windowed mode"
                    : "Projection opened";
        StatusText =
            $"{messagePrefix} on {displayTarget.StatusDisplayName}. Screens detected: {displayTarget.ScreenCount:N0}. Bounds: {displayTarget.BoundsDisplay}.";

        App.LogStartupMessage(
            $"{messagePrefix}." +
            $"{Environment.NewLine}Screens detected: {displayTarget.ScreenCount:N0}" +
            $"{Environment.NewLine}Selected display: {displayTarget.StatusDisplayName}" +
            $"{Environment.NewLine}Device: {displayTarget.DeviceName}" +
            $"{Environment.NewLine}Bounds: {displayTarget.BoundsDisplay}" +
            $"{Environment.NewLine}Windowed output: {isWindowedPreview || isAdaptiveWindowed}" +
            $"{Environment.NewLine}Preference: {SelectedProjectionDisplayOption?.Label ?? "Auto"}");
    }

    public void ReportProjectionPageState(int pageIndex, int pageCount)
    {
        var normalizedPageCount = Math.Max(0, pageCount);
        var normalizedPageIndex = normalizedPageCount == 0
            ? 0
            : Math.Clamp(pageIndex, 0, normalizedPageCount - 1);

        var changed = projectionPageIndex != normalizedPageIndex ||
                      projectionPageCount != normalizedPageCount;

        projectionPageIndex = normalizedPageIndex;
        projectionPageCount = normalizedPageCount;

        if (changed)
        {
            OnPropertyChanged(nameof(HasProjectionPages));
            OnPropertyChanged(nameof(ProjectionPageIndicatorText));
            RaiseProjectionPageCommandStates();
        }
    }

    public void ClearProjectionPageState()
    {
        ReportProjectionPageState(0, 0);
    }

    public ProjectionDisplayTarget ResolveProjectionDisplayTarget()
    {
        return ProjectionDisplayService.ResolveTarget(SelectedProjectionDisplayOption?.PreferenceKey);
    }

    public ProjectionDisplayTarget ResolveLiveProjectionDisplayTarget()
    {
        return ProjectionDisplayService.ResolveLiveProjectionTarget(SelectedProjectionDisplayOption?.PreferenceKey);
    }

    public async Task ProjectSelectedSavedParagraphAsync()
    {
        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        await ProjectCurrentSelectionAsync(recordHistory: true);
    }

    public async Task ProjectSavedParagraphAsync(SavedParagraphViewModel? savedParagraph)
    {
        if (savedParagraph is null)
        {
            StatusText = "Please select a saved paragraph before projecting.";
            return;
        }

        await SelectSavedParagraphAsync(savedParagraph.ParagraphId);
        await ProjectCurrentSelectionAsync(recordHistory: true);
    }

    public async Task ProjectBibleFavoriteAsync(BibleFavoriteVerseViewModel? favorite)
    {
        if (favorite is null)
        {
            StatusText = "Please select a Bible favorite before projecting.";
            return;
        }

        SelectBibleFavorite(favorite);
        await ProjectCurrentBibleSelectionAsync();
    }

    private void SelectBibleFavorite(BibleFavoriteVerseViewModel favorite)
    {
        var verse = CreateBibleVerseResult(favorite);
        var existingVerse = BibleResults.FirstOrDefault(result => result.VerseId == verse.VerseId);
        if (existingVerse is null)
        {
            BibleResults.Add(verse);
            existingVerse = verse;
        }

        var existingNavigationItem = BibleNavigationItems.FirstOrDefault(item => item.Verse?.VerseId == existingVerse.VerseId);
        if (existingNavigationItem is null)
        {
            existingNavigationItem = BibleNavigationItemViewModel.ForVerse(existingVerse);
            BibleNavigationItems.Add(existingNavigationItem);
        }

        IsBibleMode = true;
        SelectedBibleNavigationItem = existingNavigationItem;
        SelectedBibleVerse = existingVerse;
        selectedBibleVerseIsFavorite = true;
        OnPropertyChanged(nameof(FavoriteButtonText));
        StatusText = $"Selected {favorite.ReferenceDisplay}.";
    }

    private void CopyBibleFavorite(BibleFavoriteVerseViewModel? favorite)
    {
        if (favorite is null)
        {
            StatusText = "Please select a Bible favorite before copying.";
            return;
        }

        Clipboard.SetText($"{favorite.ReferenceDisplay} {favorite.TranslationAbbreviation}{Environment.NewLine}{favorite.Text}");
        StatusText = $"Copied {favorite.ReferenceDisplay}.";
    }

    private static BibleVerseResultViewModel CreateBibleVerseResult(BibleFavoriteVerseViewModel favorite)
    {
        return new BibleVerseResultViewModel(new BibleSearchResult(
            favorite.VerseId,
            favorite.TranslationId,
            favorite.TranslationName,
            favorite.TranslationAbbreviation,
            favorite.BookId,
            favorite.BookName,
            favorite.BookOrder,
            favorite.Chapter,
            favorite.Verse,
            favorite.Text));
    }

    private async Task RemoveSavedFavoriteAsync(SavedParagraphViewModel? favorite)
    {
        if (favorite is null)
        {
            StatusText = "Please select a favorite to remove.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            await dbContext.FavoriteParagraphs
                .Where(row => row.Id == favorite.Id)
                .ExecuteDeleteAsync();

            await LoadFavoritesAsync();
            if (SelectedParagraph?.ParagraphId == favorite.ParagraphId)
            {
                SelectedParagraph.IsFavorite = false;
                OnPropertyChanged(nameof(FavoriteButtonText));
            }

            StatusText = "Favorite removed.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Remove favorite failed.", ex);
            StatusText = $"Favorite could not be removed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task RemoveBibleFavoriteAsync(BibleFavoriteVerseViewModel? favorite)
    {
        if (favorite is null)
        {
            StatusText = "Please select a Bible favorite to remove.";
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            await dbContext.BibleFavoriteVerses
                .Where(row => row.Id == favorite.Id)
                .ExecuteDeleteAsync();

            await LoadFavoritesAsync();
            if (SelectedBibleVerse?.VerseId == favorite.VerseId)
            {
                selectedBibleVerseIsFavorite = false;
                OnPropertyChanged(nameof(FavoriteButtonText));
            }

            StatusText = $"Removed {favorite.ReferenceDisplay} from favorites.";
        }
        catch (Exception ex)
        {
            App.LogStartupError("Remove Bible favorite failed.", ex);
            StatusText = $"Bible favorite could not be removed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task ProjectCurrentSelectionAsync(bool recordHistory)
    {
        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        var paragraph = SelectedParagraph;
        var snapshot = CreateSermonSnapshot(paragraph);

        if (recordHistory)
        {
            await RecordProjectionHistoryAsync(paragraph, SearchText);
        }

        if (!recordHistory)
        {
            StatusText = $"Projecting Paragraph {paragraph.ParagraphNumber}.";
        }

        ActiveProjectionContent = snapshot;
        ProjectRequested?.Invoke();
    }

    private Task ProjectCurrentBibleSelectionAsync()
    {
        if (SelectedBibleVerse is null)
        {
            StatusText = "Please select a Bible verse before projecting.";
            return Task.CompletedTask;
        }

        var verse = SelectedBibleVerse;
        var snapshot = CreateBibleSnapshot(verse);

        StatusText = $"Projecting {verse.ReferenceDisplay} ({verse.TranslationAbbreviation}).";
        ActiveProjectionContent = snapshot;
        ProjectRequested?.Invoke();
        return Task.CompletedTask;
    }

    private Task ProjectCurrentSongSelectionAsync()
    {
        if (SelectedSong is null || SelectedSongSection is null)
        {
            StatusText = "Please select a song section before projecting.";
            return Task.CompletedTask;
        }

        var song = SelectedSong;
        var section = SelectedSongSection;
        var snapshot = CreateSongSnapshot(song, section);

        StatusText = $"Projecting {song.Title} - {section.SectionLabel}.";
        ActiveProjectionContent = snapshot;
        ProjectRequested?.Invoke();
        return Task.CompletedTask;
    }

    private static ProjectedContentSnapshot CreateSermonSnapshot(ParagraphResultViewModel paragraph)
    {
        return new ProjectedContentSnapshot(
            ProjectionContentType.Sermon,
            paragraph.SermonTitle,
            $"Paragraph {paragraph.ParagraphNumber}",
            paragraph.FullParagraphText)
        {
            SourceId = paragraph.SermonId,
            ItemId = paragraph.ParagraphId,
            ItemOrder = paragraph.ParagraphNumber,
            ItemNumber = paragraph.ParagraphNumber,
            SourceYear = paragraph.Year > 0 ? paragraph.Year : null,
            SourceKey = paragraph.SermonCode,
            MetadataText = paragraph.MetadataLine
        };
    }

    private static ProjectedContentSnapshot CreateBibleSnapshot(BibleVerseResultViewModel verse)
    {
        return new ProjectedContentSnapshot(
            ProjectionContentType.Bible,
            verse.ReferenceDisplay,
            string.Empty,
            verse.Text)
        {
            SourceId = verse.TranslationId,
            ItemId = verse.VerseId,
            ItemOrder = verse.Verse,
            ItemNumber = verse.Verse,
            SourceKey = verse.TranslationAbbreviation
        };
    }

    private static ProjectedContentSnapshot CreateSongSnapshot(
        SongResultViewModel song,
        SongSectionViewModel section)
    {
        return new ProjectedContentSnapshot(
            ProjectionContentType.Song,
            song.Title,
            section.SectionLabel,
            section.Text)
        {
            SourceId = song.SongId,
            ItemId = section.SectionId,
            ItemOrder = section.SectionOrder,
            IsTitleSlide = section.SectionOrder <= 1,
            SourceKey = song.Title
        };
    }

    private async void ToggleFavorite()
    {
        if (IsBibleMode)
        {
            await ToggleBibleFavoriteAsync();
            return;
        }

        if (IsSongsMode)
        {
            StatusText = "Song favorites are not enabled yet.";
            return;
        }

        if (SelectedParagraph is null)
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var existingFavorite = await dbContext.FavoriteParagraphs
                .FirstOrDefaultAsync(
                    favorite => favorite.SermonParagraphId == SelectedParagraph.ParagraphId);

            if (existingFavorite is null)
            {
                dbContext.FavoriteParagraphs.Add(new FavoriteParagraph
                {
                    SermonParagraphId = SelectedParagraph.ParagraphId,
                    CreatedAt = DateTime.UtcNow,
                    Notes = string.Empty
                });

                SelectedParagraph.IsFavorite = true;
                StatusText = "Paragraph added to favorites.";
            }
            else
            {
                dbContext.FavoriteParagraphs.Remove(existingFavorite);
                SelectedParagraph.IsFavorite = false;
                StatusText = "Paragraph removed from favorites.";
            }

            await dbContext.SaveChangesAsync();
            await LoadFavoritesAsync();
            OnPropertyChanged(nameof(FavoriteButtonText));
        }
        catch (Exception ex)
        {
            StatusText = $"Favorite update failed: {ex.Message}";
        }
    }

    private async Task ToggleBibleFavoriteAsync()
    {
        if (SelectedBibleVerse is null)
        {
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var verseId = SelectedBibleVerse.VerseId;
            var existingFavorite = await dbContext.BibleFavoriteVerses
                .FirstOrDefaultAsync(favorite => favorite.BibleVerseId == verseId);

            if (existingFavorite is null)
            {
                dbContext.BibleFavoriteVerses.Add(new BibleFavoriteVerse
                {
                    BibleVerseId = verseId,
                    CreatedAt = DateTime.UtcNow,
                    Notes = string.Empty
                });

                selectedBibleVerseIsFavorite = true;
                StatusText = $"Added {SelectedBibleVerse.ReferenceDisplay} to favorites.";
            }
            else
            {
                dbContext.BibleFavoriteVerses.Remove(existingFavorite);
                selectedBibleVerseIsFavorite = false;
                StatusText = $"Removed {SelectedBibleVerse.ReferenceDisplay} from favorites.";
            }

            await dbContext.SaveChangesAsync();
            await LoadFavoritesAsync();
            OnPropertyChanged(nameof(FavoriteButtonText));
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible favorite update failed.", ex);
            StatusText = $"Bible favorite update failed: {ex.Message}";
        }
        finally
        {
            IsDatabaseOperationRunning = false;
        }
    }

    private async Task SelectSavedParagraphAsync(int paragraphId)
    {
        try
        {
            var paragraph = await LoadParagraphAsync(paragraphId);
            if (paragraph is null)
            {
                StatusText = "Saved paragraph could not be found.";
                return;
            }

            SetResults([paragraph], paragraph.ParagraphId);
            StatusText = $"Selected Paragraph {paragraph.ParagraphNumber}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Could not select saved paragraph: {ex.Message}";
        }
    }

    private async Task<ParagraphResultViewModel?> LoadParagraphAsync(int paragraphId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var row = await dbContext.SermonParagraphs
            .AsNoTracking()
            .Where(paragraph => paragraph.Id == paragraphId)
            .Select(paragraph => new
            {
                ParagraphId = paragraph.Id,
                paragraph.SermonId,
                paragraph.ParagraphNumber,
                paragraph.Text,
                paragraph.PageNumber,
                SermonTitle = paragraph.Sermon!.Title,
                paragraph.Sermon.SermonCode,
                paragraph.Sermon.Year,
                paragraph.Sermon.SourceFilePath,
                AuthorDisplayName = paragraph.Sermon.Author == null
                    ? string.Empty
                    : paragraph.Sermon.Author.DisplayName,
                SourceDisplayName = paragraph.Sermon.ContentSource == null
                    ? string.Empty
                    : paragraph.Sermon.ContentSource.DisplayName,
                SourceType = paragraph.Sermon.ContentSource == null
                    ? string.Empty
                    : paragraph.Sermon.ContentSource.SourceType,
                IsFavorite = paragraph.Favorites.Any()
            })
            .FirstOrDefaultAsync();

        if (row is null)
        {
            return null;
        }

        var result = new ParagraphResultViewModel(
            row.SermonId,
            row.ParagraphId,
            row.SermonTitle,
            row.SermonCode,
            row.Year,
            row.ParagraphNumber,
            CreatePreview(row.Text),
            row.Text,
            row.SourceFilePath,
            row.PageNumber,
            row.AuthorDisplayName,
            row.SourceDisplayName,
            row.SourceType)
        {
            IsFavorite = row.IsFavorite
        };

        return result;
    }

    private async Task<ParagraphResultViewModel?> LoadAdjacentParagraphAsync(
        ParagraphResultViewModel currentParagraph,
        int offset)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var query = dbContext.SermonParagraphs
            .AsNoTracking()
            .Where(paragraph => paragraph.SermonId == currentParagraph.SermonId);

        query = offset > 0
            ? query
                .Where(paragraph => paragraph.ParagraphNumber > currentParagraph.ParagraphNumber)
                .OrderBy(paragraph => paragraph.ParagraphNumber)
            : query
                .Where(paragraph => paragraph.ParagraphNumber < currentParagraph.ParagraphNumber)
                .OrderByDescending(paragraph => paragraph.ParagraphNumber);

        var paragraphId = await query
            .Select(paragraph => (int?)paragraph.Id)
            .FirstOrDefaultAsync();

        return paragraphId is null ? null : await LoadParagraphAsync(paragraphId.Value);
    }

    private async Task<List<ParagraphResultViewModel>> LoadSermonParagraphsAsync(
        int sermonId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var rows = await dbContext.SermonParagraphs
            .AsNoTracking()
            .Where(paragraph => paragraph.SermonId == sermonId)
            .OrderBy(paragraph => paragraph.ParagraphNumber)
            .Select(paragraph => new
            {
                ParagraphId = paragraph.Id,
                paragraph.SermonId,
                paragraph.ParagraphNumber,
                paragraph.Text,
                paragraph.PageNumber,
                SermonTitle = paragraph.Sermon!.Title,
                paragraph.Sermon.SermonCode,
                paragraph.Sermon.Year,
                paragraph.Sermon.SourceFilePath,
                AuthorDisplayName = paragraph.Sermon.Author == null
                    ? string.Empty
                    : paragraph.Sermon.Author.DisplayName,
                SourceDisplayName = paragraph.Sermon.ContentSource == null
                    ? string.Empty
                    : paragraph.Sermon.ContentSource.DisplayName,
                SourceType = paragraph.Sermon.ContentSource == null
                    ? string.Empty
                    : paragraph.Sermon.ContentSource.SourceType,
                IsFavorite = paragraph.Favorites.Any()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ParagraphResultViewModel(
                row.SermonId,
                row.ParagraphId,
                row.SermonTitle,
                row.SermonCode,
                row.Year,
                row.ParagraphNumber,
                CreatePreview(row.Text),
                row.Text,
                row.SourceFilePath,
                row.PageNumber,
                row.AuthorDisplayName,
                row.SourceDisplayName,
                row.SourceType)
            {
                IsFavorite = row.IsFavorite
            })
            .ToList();
    }

    private async Task LoadFavoritesAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var language = ContentLanguageCode;
        var bibleLanguage = SelectedUiLanguage.BibleLanguageName;

        var favorites = await dbContext.FavoriteParagraphs
            .AsNoTracking()
            .Where(favorite => favorite.SermonParagraph != null &&
                               favorite.SermonParagraph.Sermon != null &&
                               favorite.SermonParagraph.Sermon.Language == language)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Select(favorite => new
            {
                favorite.Id,
                favorite.CreatedAt,
                ParagraphId = favorite.SermonParagraphId,
                favorite.SermonParagraph!.ParagraphNumber,
                Text = favorite.SermonParagraph.Text,
                SermonTitle = favorite.SermonParagraph.Sermon!.Title,
                favorite.SermonParagraph.Sermon.SermonCode,
                favorite.SermonParagraph.Sermon.Year,
                SourceName = favorite.SermonParagraph.Sermon.ContentSource != null
                    ? favorite.SermonParagraph.Sermon.ContentSource.Name
                    : null,
                SourceDisplayName = favorite.SermonParagraph.Sermon.ContentSource != null
                    ? favorite.SermonParagraph.Sermon.ContentSource.DisplayName
                    : null,
                AuthorDisplayName = favorite.SermonParagraph.Sermon.Author != null
                    ? favorite.SermonParagraph.Sermon.Author.DisplayName
                    : null,
                AuthorFullName = favorite.SermonParagraph.Sermon.Author != null
                    ? favorite.SermonParagraph.Sermon.Author.FullName
                    : null
            })
            .ToListAsync();

        FavoriteParagraphs.Clear();
        foreach (var favorite in favorites.Where(favorite =>
                     !IsHiddenFromChurchUi(favorite.SourceName, favorite.SourceDisplayName) &&
                     !IsDeferredFrankLibrary(favorite.AuthorFullName, favorite.AuthorDisplayName)))
        {
            FavoriteParagraphs.Add(new SavedParagraphViewModel(
                favorite.Id,
                favorite.ParagraphId,
                favorite.SermonTitle,
                favorite.SermonCode,
                favorite.Year,
                favorite.ParagraphNumber,
                CreatePreview(favorite.Text),
                favorite.CreatedAt,
                Loc.T("Fav_KindFavorite")));
        }

        var bibleFavorites = await dbContext.BibleFavoriteVerses
            .AsNoTracking()
            .Where(favorite => favorite.BibleVerse != null &&
                               favorite.BibleVerse.BibleTranslation != null &&
                               favorite.BibleVerse.BibleTranslation.Language == bibleLanguage)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .Select(favorite => new
            {
                favorite.Id,
                favorite.CreatedAt,
                VerseId = favorite.BibleVerseId,
                favorite.BibleVerse!.TranslationId,
                TranslationName = favorite.BibleVerse.BibleTranslation!.Name,
                TranslationAbbreviation = favorite.BibleVerse.BibleTranslation.Abbreviation,
                favorite.BibleVerse.BookId,
                BookName = favorite.BibleVerse.BibleBook!.Name,
                favorite.BibleVerse.BibleBook.BookOrder,
                favorite.BibleVerse.Chapter,
                favorite.BibleVerse.Verse,
                favorite.BibleVerse.Text
            })
            .ToListAsync();

        BibleFavoriteVerses.Clear();
        foreach (var favorite in bibleFavorites)
        {
            BibleFavoriteVerses.Add(new BibleFavoriteVerseViewModel(
                favorite.Id,
                favorite.VerseId,
                favorite.TranslationId,
                favorite.TranslationName,
                favorite.TranslationAbbreviation,
                favorite.BookId,
                favorite.BookName,
                favorite.BookOrder,
                favorite.Chapter,
                favorite.Verse,
                favorite.Text,
                favorite.CreatedAt));
        }
    }

    private async Task ReloadAfterDatabaseRestoreAsync()
    {
        searchDebounce?.Cancel();
        selectedSermon = null;
        selectedParagraph = null;
        selectedFavoriteParagraph = null;
        selectedBibleFavoriteVerse = null;
        selectedHistoryParagraph = null;
        selectedContentSource = null;
        selectedBibleTranslation = null;
        selectedBibleVerse = null;
        selectedBibleNavigationItem = null;
        selectedSourceFilter = null;
        selectedBibleVerseIsFavorite = false;
        allParagraphResults = [];

        SermonResults.Clear();
        ParagraphResults.Clear();
        FavoriteParagraphs.Clear();
        BibleFavoriteVerses.Clear();
        ProjectionHistoryItems.Clear();
        ContentSources.Clear();
        BibleTranslations.Clear();
        BibleResults.Clear();
        BibleNavigationItems.Clear();
        SourceFilters.Clear();
        ResultCount = 0;
        IsBibleAvailable = false;
        IsBibleMode = false;
        SelectedSourceDetails = SourceDiagnosticsViewModel.None;

        OnPropertyChanged(nameof(SelectedSermon));
        OnPropertyChanged(nameof(SelectedParagraph));
        OnPropertyChanged(nameof(SelectedFavoriteParagraph));
        OnPropertyChanged(nameof(SelectedBibleFavoriteVerse));
        OnPropertyChanged(nameof(SelectedHistoryParagraph));
        OnPropertyChanged(nameof(SelectedContentSource));
        OnPropertyChanged(nameof(SelectedBibleTranslation));
        OnPropertyChanged(nameof(SelectedBibleVerse));
        OnPropertyChanged(nameof(SelectedBibleNavigationItem));
        OnPropertyChanged(nameof(SelectedSourceFilter));
        OnPropertyChanged(nameof(SelectedParagraphHeader));
        OnPropertyChanged(nameof(SelectedParagraphMeta));
        OnPropertyChanged(nameof(PreviewHeader));
        OnPropertyChanged(nameof(PreviewMeta));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(PreviewHighlightQuery));
        OnPropertyChanged(nameof(ProjectionParagraphTitle));
        OnPropertyChanged(nameof(ProjectionParagraphNumber));
        OnPropertyChanged(nameof(SelectedParagraphText));
        OnPropertyChanged(nameof(FavoriteButtonText));
        OnPropertyChanged(nameof(IsProjectionHistoryEmpty));
        OnPropertyChanged(nameof(IsFavoritesEmpty));
        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(IsSermonFavoritesEmpty));
        OnPropertyChanged(nameof(IsBibleFavoritesEmpty));
        OnPropertyChanged(nameof(IsBibleResultsEmpty));

        await InitializeAsync();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            await SearchNowAsync();
        }

        RaiseCommandStates();
    }

    private async Task<FilterLoadResult> LoadFilterOptionsAsync(
        MessageFlowDbContext dbContext,
        int? preferredAuthorId = null,
        int? preferredSourceId = null,
        int? preferredYear = null)
    {
        var language = ContentLanguageCode;
        var linkedSourceRows = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId != null &&
                             sermon.Language == language)
            .Select(sermon => new
            {
                sermon.ContentSource!.Id,
                sermon.ContentSource.DisplayName,
                sermon.ContentSource.Name,
                sermon.ContentSource.LocalFolderPath
            })
            .Distinct()
            .ToListAsync();

        var linkedSources = linkedSourceRows
            .Where(source => !IsHiddenFromChurchUi(source.Name, source.DisplayName, source.LocalFolderPath))
            .OrderBy(source => source.DisplayName)
            .ThenBy(source => source.Name)
            .Select(source => new FilterOption(source.Id, source.DisplayName))
            .ToList();

        var visibleSourceIds = linkedSources
            .Select(source => source.Value!.Value)
            .ToArray();

        var linkedAuthorIds = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId != null &&
                             sermon.Language == language &&
                             visibleSourceIds.Contains(sermon.ContentSourceId.Value))
            .Select(sermon => sermon.AuthorId)
            .Distinct()
            .OrderBy(authorId => authorId)
            .ToListAsync();

        var authorRows = linkedAuthorIds.Count == 0
            ? []
            : await dbContext.Authors
                .AsNoTracking()
                .Where(author => linkedAuthorIds.Contains(author.Id))
                .Select(author => new
                {
                    author.Id,
                    author.FullName,
                    author.DisplayName
                })
                .ToListAsync();

        var authorLabels = authorRows
            .Select(author => new FilterOption(
                author.Id,
                CreateAuthorLabel(author.DisplayName, author.FullName, author.Id)))
            .ToDictionary(author => author.Value!.Value);

        var linkedAuthors = linkedAuthorIds
            .Select(authorId => authorLabels.TryGetValue(authorId, out var author)
                ? author
                : new FilterOption(authorId, CreateMissingAuthorLabel(authorId)))
            .OrderBy(author => author.Label)
            .ToList();

        var years = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon =>
                sermon.Year > 0 &&
                sermon.Language == language &&
                sermon.ContentSourceId != null &&
                visibleSourceIds.Contains(sermon.ContentSourceId.Value))
            .Select(sermon => sermon.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        AuthorFilters.Clear();
        AuthorFilters.Add(new FilterOption(null, Loc.T("Filter_AllAuthors")));
        foreach (var author in linkedAuthors)
        {
            AuthorFilters.Add(author);
        }

        YearFilters.Clear();
        YearFilters.Add(new FilterOption(null, Loc.T("Filter_AllYears")));
        foreach (var year in years)
        {
            YearFilters.Add(new FilterOption(year, year.ToString()));
        }

        SourceFilters.Clear();
        SourceFilters.Add(new FilterOption(null, Loc.T("Filter_AllSources")));
        foreach (var source in linkedSources)
        {
            SourceFilters.Add(source);
        }

        selectedAuthor = AuthorFilters.FirstOrDefault(author => author.Value == preferredAuthorId) ?? AuthorFilters[0];
        selectedSourceFilter = SourceFilters.FirstOrDefault(source => source.Value == preferredSourceId) ?? SourceFilters[0];
        selectedYear = YearFilters.FirstOrDefault(year => year.Value == preferredYear) ?? YearFilters[0];
        OnPropertyChanged(nameof(SelectedAuthor));
        OnPropertyChanged(nameof(SelectedSourceFilter));
        OnPropertyChanged(nameof(SelectedYear));

        App.LogStartupMessage(
            $"Loaded filter data. Authors: {linkedAuthors.Count}. Sources: {linkedSources.Count}. Years: {years.Count}.");

        return new FilterLoadResult(linkedAuthors.Count, linkedSources.Count, years.Count);
    }

    private static async Task<bool> HasBrotherFrankPublicationMetadataDriftAsync(MessageFlowDbContext dbContext)
    {
        var publicationFolder = Path.Combine(
            Path.GetDirectoryName(MessageFlowDatabase.DefaultDatabasePath)!,
            "custom-library",
            "publications");

        var sourceRows = await dbContext.ContentSources
            .AsNoTracking()
            .Where(source =>
                source.Name.Contains("brother_frank") ||
                source.DisplayName.Contains("Brother Frank") ||
                source.DisplayName.Contains("Ewald Frank"))
            .Select(source => new
            {
                source.Id,
                source.DisplayName,
                source.SourceType,
                source.LocalFolderPath
            })
            .ToListAsync();

        if (sourceRows.Count == 0)
        {
            return false;
        }

        var author = await dbContext.Authors
            .AsNoTracking()
            .Where(item => item.FullName == "Ewald Frank" || item.DisplayName == "Brother Frank")
            .Select(item => new { item.Id, item.FullName, item.DisplayName })
            .FirstOrDefaultAsync();
        if (author is null ||
            !string.Equals(author.FullName, "Ewald Frank", StringComparison.Ordinal) ||
            !string.Equals(author.DisplayName, "Brother Frank", StringComparison.Ordinal))
        {
            return true;
        }

        var sourceIds = sourceRows.Select(source => source.Id).ToList();
        foreach (var source in sourceRows)
        {
            if (!string.Equals(source.DisplayName, "Brother Frank Publications", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(source.SourceType) ||
                string.Equals(source.SourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.SourceType, "Brother Frank", StringComparison.OrdinalIgnoreCase) ||
                source.LocalFolderPath?.Contains(
                    Path.Combine("custom-library", "sermons"),
                    StringComparison.OrdinalIgnoreCase) == true ||
                (string.IsNullOrWhiteSpace(source.LocalFolderPath) && Directory.Exists(publicationFolder)))
            {
                return true;
            }
        }

        return await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId != null && sourceIds.Contains(sermon.ContentSourceId.Value))
            .AnyAsync(sermon =>
                sermon.AuthorId != author.Id ||
                sermon.Title == "Ewald Frank - Revelation - english" ||
                sermon.Title == "Ewald Frank Revelation english" ||
                sermon.SermonCode == "BROTHER-FRANK" ||
                string.IsNullOrWhiteSpace(sermon.Title));
    }

    private static async Task EnsureBrotherFrankPublicationMetadataAsync(MessageFlowDbContext dbContext)
    {
        var sources = await dbContext.ContentSources
            .Where(source =>
                source.Name.Contains("brother_frank") ||
                source.DisplayName.Contains("Brother Frank") ||
                source.DisplayName.Contains("Ewald Frank"))
            .ToListAsync();

        if (sources.Count == 0)
        {
            return;
        }

        var changed = false;
        var author = await dbContext.Authors.FirstOrDefaultAsync(item => item.FullName == "Ewald Frank") ??
                     await dbContext.Authors.FirstOrDefaultAsync(item => item.DisplayName == "Brother Frank");

        if (author is null)
        {
            author = new Author
            {
                FullName = "Ewald Frank",
                DisplayName = "Brother Frank",
                Description = "Brother Frank local custom publication library."
            };
            dbContext.Authors.Add(author);
            changed = true;
        }
        else
        {
            if (!string.Equals(author.FullName, "Ewald Frank", StringComparison.Ordinal))
            {
                author.FullName = "Ewald Frank";
                changed = true;
            }

            if (!string.Equals(author.DisplayName, "Brother Frank", StringComparison.Ordinal))
            {
                author.DisplayName = "Brother Frank";
                changed = true;
            }
        }

        var publicationFolder = Path.Combine(
            Path.GetDirectoryName(MessageFlowDatabase.DefaultDatabasePath)!,
            "custom-library",
            "publications");

        foreach (var source in sources)
        {
            if (!string.Equals(source.DisplayName, "Brother Frank Publications", StringComparison.Ordinal))
            {
                source.DisplayName = "Brother Frank Publications";
                changed = true;
            }

            var sermons = await dbContext.Sermons
                .Where(sermon => sermon.ContentSourceId == source.Id)
                .ToListAsync();
            var hasCircularLetters = false;
            var context = new SourceMetadataContext(
                source.Id,
                source.Name,
                "Brother Frank Publications",
                source.SourceType);

            foreach (var sermon in sermons)
            {
                var metadataRoot = Path.GetDirectoryName(sermon.SourceFilePath) ??
                                   source.LocalFolderPath ??
                                   publicationFolder;
                var metadata = SermonMetadataParser.Parse(sermon.SourceFilePath, metadataRoot, context);
                hasCircularLetters = hasCircularLetters || IsCircularLetterMetadata(metadata);

                if (sermon.AuthorId != author.Id)
                {
                    sermon.AuthorId = author.Id;
                    changed = true;
                }

                if (!string.Equals(sermon.Title, metadata.Title, StringComparison.Ordinal))
                {
                    sermon.Title = metadata.Title;
                    changed = true;
                }

                if (!string.Equals(sermon.SermonCode, metadata.SermonCode, StringComparison.Ordinal))
                {
                    sermon.SermonCode = metadata.SermonCode;
                    changed = true;
                }

                if (sermon.Year != metadata.Year)
                {
                    sermon.Year = metadata.Year;
                    changed = true;
                }

                if (sermon.Date != metadata.Date)
                {
                    sermon.Date = metadata.Date;
                    changed = true;
                }

                if (!string.Equals(sermon.Language, metadata.Language, StringComparison.Ordinal))
                {
                    sermon.Language = metadata.Language;
                    changed = true;
                }
            }

            var repairedSourceType = hasCircularLetters ? "CircularLetter" : "Book";
            if (!string.Equals(source.SourceType, repairedSourceType, StringComparison.OrdinalIgnoreCase))
            {
                source.SourceType = repairedSourceType;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(source.LocalFolderPath) ||
                source.LocalFolderPath.Contains(
                    Path.Combine("custom-library", "sermons"),
                    StringComparison.OrdinalIgnoreCase))
            {
                source.LocalFolderPath = publicationFolder;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        Directory.CreateDirectory(publicationFolder);
        await dbContext.SaveChangesAsync();
        await MessageFlowDatabaseRepair.RebuildSearchIndexAsync(MessageFlowDatabase.DefaultDatabasePath, App.LogStartupMessage);
    }

    private async Task RefreshFilterOptionsPreservingSelectionAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        await LoadFilterOptionsAsync(dbContext, SelectedAuthor?.Value, SelectedSourceFilter?.Value, SelectedYear?.Value);
    }

    private async Task LoadBibleTranslationsAsync(int? preferredTranslationId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var bibleLanguage = SelectedUiLanguage.BibleLanguageName;
        var translations = await dbContext.BibleTranslations
            .AsNoTracking()
            .Where(translation => translation.Language == bibleLanguage)
            .OrderBy(translation => translation.Abbreviation)
            .ThenBy(translation => translation.Name)
            .Select(translation => new BibleTranslationOption(
                translation.Id,
                translation.Name,
                translation.Abbreviation,
                translation.Language))
            .ToListAsync();

        var preferredAbbreviation = SelectedUiLanguage.PreferredBibleAbbreviation;
        var existingSelectionId = preferredTranslationId ?? SelectedBibleTranslation?.Id;
        BibleTranslations.Clear();
        foreach (var translation in translations)
        {
            BibleTranslations.Add(translation);
        }

        SelectedBibleTranslation =
            BibleTranslations.FirstOrDefault(translation =>
                string.Equals(translation.Abbreviation, preferredAbbreviation, StringComparison.OrdinalIgnoreCase)) ??
            (existingSelectionId is null
                ? BibleTranslations.FirstOrDefault()
                : BibleTranslations.FirstOrDefault(translation => translation.Id == existingSelectionId.Value) ??
                  BibleTranslations.FirstOrDefault());

        currentBibleVerseCount = SelectedBibleTranslation is null
            ? 0
            : await dbContext.BibleVerses
                .AsNoTracking()
                .CountAsync(verse => verse.TranslationId == SelectedBibleTranslation.Id);
        OnPropertyChanged(nameof(CurrentBibleVerseCountDisplay));

        IsBibleAvailable = BibleTranslations.Count > 0;
        if (!IsBibleAvailable)
        {
            BibleResults.Clear();
            BibleNavigationItems.Clear();
            SelectedBibleNavigationItem = null;
            SelectedBibleVerse = null;
        }

        App.LogStartupMessage(
            $"Loaded Bible translations for {bibleLanguage}: {BibleTranslations.Count} " +
            $"[{string.Join(", ", BibleTranslations.Select(translation => translation.Abbreviation))}]; " +
            $"selected={SelectedBibleTranslation?.Abbreviation ?? "(none)"}.");
    }

    private async Task LoadContentSourcesAsync(int? preferredSourceId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var sources = await dbContext.ContentSources
            .AsNoTracking()
            .OrderBy(source => source.DisplayName)
            .ThenBy(source => source.Name)
            .Select(source => new ContentSourceViewModel(
                source.Id,
                source.Name,
                source.DisplayName,
                source.SourceType,
                source.Description,
                source.LocalFolderPath))
            .ToListAsync();

        ContentSources.Clear();
        foreach (var source in sources)
        {
            ContentSources.Add(source);
        }

        RefreshManageableContentSources();

        var visibleSources = ManageableContentSources.Count > 0
            ? ManageableContentSources
            : ContentSources;

        SelectedContentSource = preferredSourceId is null
            ? visibleSources.FirstOrDefault(source => source.Id == SelectedContentSource?.Id) ??
              visibleSources.FirstOrDefault()
            : visibleSources.FirstOrDefault(source => source.Id == preferredSourceId.Value) ??
              visibleSources.FirstOrDefault();

        if (sources.Count == 0)
        {
            SelectedSourceDetails = SourceDiagnosticsViewModel.None;
            StatusText = "No content sources configured yet.";
            return;
        }

        _ = LoadSelectedSourceDiagnosticsAsync(SelectedContentSource);
    }

    private void RefreshManageableContentSources()
    {
        var selectedSourceId = SelectedContentSource?.Id;

        ManageableContentSources.Clear();
        foreach (var source in ContentSources.Where(source =>
                     (ShowTestSourcesInManageSources || !LooksLikeTestSource(source)) &&
                     !IsDeferredFrankLibrary(source)))
        {
            ManageableContentSources.Add(source);
        }

        if (SelectedContentSource is not null &&
            ((!ShowTestSourcesInManageSources && LooksLikeTestSource(SelectedContentSource)) ||
             IsDeferredFrankLibrary(SelectedContentSource)))
        {
            SelectedContentSource = ManageableContentSources.FirstOrDefault();
            return;
        }

        if (SelectedContentSource is null && ManageableContentSources.Count > 0)
        {
            SelectedContentSource = selectedSourceId is null
                ? ManageableContentSources.FirstOrDefault()
                : ManageableContentSources.FirstOrDefault(source => source.Id == selectedSourceId.Value) ??
                  ManageableContentSources.FirstOrDefault();
        }
    }

    public void RefreshProjectionDisplayOptions()
    {
        var savedPreference = ProjectionDisplayService.LoadPreference();
        var currentPreference = SelectedProjectionDisplayOption?.PreferenceKey;
        var preferredKey = string.IsNullOrWhiteSpace(currentPreference)
            ? savedPreference
            : currentPreference;

        var options = ProjectionDisplayService.GetDisplayOptions();
        ProjectionDisplayOptions.Clear();
        foreach (var option in options)
        {
            ProjectionDisplayOptions.Add(option);
        }

        var selectedOption = ProjectionDisplayOptions.FirstOrDefault(option =>
                                 string.Equals(option.PreferenceKey, preferredKey, StringComparison.OrdinalIgnoreCase)) ??
                             ProjectionDisplayOptions.FirstOrDefault(option => option.IsAuto) ??
                             ProjectionDisplayOptions.FirstOrDefault();

        suppressProjectionDisplayPreferenceSave = true;
        try
        {
            SelectedProjectionDisplayOption = selectedOption;
        }
        finally
        {
            suppressProjectionDisplayPreferenceSave = false;
        }
    }

    private void RequestProjectionDisplayTest()
    {
        ProjectionTestRequested?.Invoke();
    }

    private void RequestProjectionPreview()
    {
        ProjectionPreviewRequested?.Invoke();
    }

    private void DecreaseProjectionTextSize()
    {
        ApplyProjectionTextSizeAdjustment(projectionFontSizeAdjustment - ProjectionFontAdjustmentStep);
        StatusText = "Projection text decreased by 2 DIPs.";
    }

    private void IncreaseProjectionTextSize()
    {
        ApplyProjectionTextSizeAdjustment(projectionFontSizeAdjustment + ProjectionFontAdjustmentStep);
        StatusText = projectionFontSizeAdjustment >= GetMaximumProjectionFontAdjustment()
            ? "Maximum projection text size."
            : "Projection text increased by 2 DIPs.";
    }

    private void ResetProjectionTextSize()
    {
        ApplyProjectionTextSizeAdjustment(0);
        projectionFontSizeUsesSafeFit = true;
        OnPropertyChanged(nameof(IsProjectionFontSizeUsingSafeFit));
        StatusText = "Projection text restored to the largest safe fit.";
    }

    private void RequestPreviousProjectionPage()
    {
        PreviousProjectionPageRequested?.Invoke();
    }

    private void RequestNextProjectionPage()
    {
        NextProjectionPageRequested?.Invoke();
    }

    private void ApplyProjectionTextSizeAdjustment(double requestedAdjustment)
    {
        projectionFontSizeUsesSafeFit = false;
        var nextAdjustment = Math.Clamp(
            requestedAdjustment,
            GetMinimumProjectionFontAdjustment(),
            GetMaximumProjectionFontAdjustment());

        if (Math.Abs(nextAdjustment - projectionFontSizeAdjustment) <= 0.1)
        {
            return;
        }

        projectionFontSizeAdjustment = nextAdjustment;
        OnPropertyChanged(nameof(ProjectionFontSize));
        OnPropertyChanged(nameof(ProjectionLineHeight));
        OnPropertyChanged(nameof(ProjectionFontSizeOffset));
        OnPropertyChanged(nameof(SermonProjectionFontSize));
        OnPropertyChanged(nameof(IsProjectionFontSizeUsingSafeFit));
        RaiseProjectionTextSizeCommandStates();

        StatusText = Math.Abs(projectionFontSizeAdjustment) <= 0.1
            ? "Projection text size reset to fit."
            : projectionFontSizeAdjustment > 0
                ? $"Projection text size increased by {projectionFontSizeAdjustment:0}."
                : $"Projection text size decreased by {Math.Abs(projectionFontSizeAdjustment):0}.";
    }

    private double GetMinimumProjectionFontAdjustment() =>
        IsLiveSermonProjection ? MinimumSermonProjectionFontAdjustment : MinimumProjectionFontAdjustment;

    private double GetMaximumProjectionFontAdjustment() =>
        IsLiveSermonProjection ? MaximumSermonProjectionFontAdjustment : MaximumProjectionFontAdjustment;

    private async Task LoadSelectedSourceDiagnosticsAsync(ContentSourceViewModel? source)
    {
        var version = Interlocked.Increment(ref sourceDetailsRequestVersion);
        if (source is null)
        {
            SelectedSourceDetails = SourceDiagnosticsViewModel.None;
            return;
        }

        SelectedSourceDetails = SourceDiagnosticsViewModel.Loading(source);

        try
        {
            var details = await BuildSourceDiagnosticsAsync(source);
            if (version == Volatile.Read(ref sourceDetailsRequestVersion))
            {
                SelectedSourceDetails = details;
            }
        }
        catch (Exception ex)
        {
            if (version == Volatile.Read(ref sourceDetailsRequestVersion))
            {
                SelectedSourceDetails = new SourceDiagnosticsViewModel(
                    source.DisplayName,
                    source.SourceTypeDisplay,
                    string.IsNullOrWhiteSpace(source.LocalFolderPath) ? "No local folder configured." : source.LocalFolderPath,
                    "Diagnostics Failed",
                    0,
                    0,
                    0,
                    "Could not load author details.",
                    LooksLikeTestSource(source),
                    1,
                    [$"Diagnostics failed: {ex.Message}"]);
            }
        }
    }

    private async Task<SourceDiagnosticsViewModel> BuildSourceDiagnosticsAsync(ContentSourceViewModel source)
    {
        var sourceContext = CreateSourceMetadataContext(source);
        var looksLikeTestSource = LooksLikeTestSource(source);
        var localFolderPath = string.IsNullOrWhiteSpace(source.LocalFolderPath)
            ? null
            : Path.GetFullPath(source.LocalFolderPath);
        var folderMissing = string.IsNullOrWhiteSpace(localFolderPath) || !Directory.Exists(localFolderPath);
        var scan = folderMissing
            ? new PdfSourceScanResult(
                new List<string>(),
                string.IsNullOrWhiteSpace(localFolderPath)
                    ? new List<string> { "No local folder configured." }
                    : new List<string> { $"Missing folder: {localFolderPath}" })
            : await Task.Run(() => ScanPdfFiles(localFolderPath!));

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var documentRows = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId == source.Id)
            .Select(sermon => new SourceDocumentDiagnosticsRow(
                sermon.Title,
                sermon.SermonCode,
                sermon.Year,
                sermon.Date,
                sermon.Location,
                sermon.Language,
                sermon.AuthorId,
                sermon.Author == null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(sermon.Author.DisplayName)
                        ? sermon.Author.FullName
                        : sermon.Author.DisplayName,
                sermon.SourceFilePath))
            .ToListAsync();

        var paragraphCount = await dbContext.SermonParagraphs
            .AsNoTracking()
            .Where(paragraph => paragraph.Sermon!.ContentSourceId == source.Id)
            .CountAsync();

        var linkedAuthors = documentRows
            .Select(row => row.AuthorDisplayName)
            .Where(author => !string.IsNullOrWhiteSpace(author))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(author => author)
            .ToList();
        var linkedAuthorDisplay = linkedAuthors.Count == 0
            ? "No imported author available."
            : string.Join(", ", linkedAuthors);

        var suspiciousMetadata = FindSuspiciousMetadata(source, sourceContext, documentRows);
        var status = DetermineSourceStatus(
            source,
            sourceContext,
            folderMissing,
            scan.PdfFilePaths.Count,
            looksLikeTestSource,
            suspiciousMetadata.Count);

        return new SourceDiagnosticsViewModel(
            source.DisplayName,
            source.SourceTypeDisplay,
            string.IsNullOrWhiteSpace(localFolderPath) ? "No local folder configured." : localFolderPath,
            status,
            documentRows.Count,
            paragraphCount,
            scan.PdfFilePaths.Count,
            linkedAuthorDisplay,
            looksLikeTestSource,
            suspiciousMetadata.Count,
            suspiciousMetadata.Samples);
    }

    private static SuspiciousMetadataResult FindSuspiciousMetadata(
        ContentSourceViewModel source,
        SourceMetadataContext sourceContext,
        IReadOnlyList<SourceDocumentDiagnosticsRow> documentRows)
    {
        var sourceRoot = string.IsNullOrWhiteSpace(source.LocalFolderPath)
            ? null
            : Path.GetFullPath(source.LocalFolderPath);
        var suspiciousCount = 0;
        var samples = new List<string>();

        foreach (var row in documentRows)
        {
            var reason = GetSuspiciousMetadataReason(row, sourceContext, sourceRoot);
            if (string.IsNullOrWhiteSpace(reason))
            {
                continue;
            }

            suspiciousCount++;
            if (samples.Count < 5)
            {
                samples.Add(reason);
            }
        }

        return new SuspiciousMetadataResult(suspiciousCount, samples);
    }

    private static string GetSuspiciousMetadataReason(
        SourceDocumentDiagnosticsRow row,
        SourceMetadataContext sourceContext,
        string? sourceRoot)
    {
        var fileName = Path.GetFileName(row.SourceFilePath);

        if (string.IsNullOrWhiteSpace(row.Title))
        {
            return $"{fileName}: title is empty.";
        }

        if (string.Equals(row.Title.Trim(), "en RB", StringComparison.OrdinalIgnoreCase))
        {
            return $"{fileName}: title is still \"en RB\".";
        }

        if (row.Title.Trim().Length < 4)
        {
            return $"{fileName}: title is shorter than 4 characters.";
        }

        var metadataRoot = sourceRoot ??
                           Path.GetDirectoryName(row.SourceFilePath) ??
                           Directory.GetCurrentDirectory();
        SermonMetadata expectedMetadata;
        try
        {
            expectedMetadata = SermonMetadataParser.Parse(row.SourceFilePath, metadataRoot, sourceContext);
        }
        catch (Exception ex)
        {
            return $"{fileName}: metadata parser failed ({ex.Message}).";
        }

        if (expectedMetadata.Year > 0 && row.Year != expectedMetadata.Year)
        {
            return $"{fileName}: year is {row.Year}, expected {expectedMetadata.Year}.";
        }

        if (IsCircularLetterMetadata(expectedMetadata) &&
            !row.Title.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase))
        {
            return $"{fileName}: circular-letter filename has non-circular title \"{row.Title}\".";
        }

        return string.Empty;
    }

    private static string DetermineSourceStatus(
        ContentSourceViewModel source,
        SourceMetadataContext sourceContext,
        bool folderMissing,
        int pdfFilesFound,
        bool looksLikeTestSource,
        int suspiciousMetadataCount)
    {
        if (looksLikeTestSource)
        {
            return "Test Source";
        }

        if (folderMissing)
        {
            return "Folder Missing";
        }

        if (pdfFilesFound == 0)
        {
            return "No PDFs Found";
        }

        if (suspiciousMetadataCount > 0)
        {
            return "Needs Metadata Repair";
        }

        if (SermonMetadataParser.IsEwaldFrankSource(sourceContext) &&
            string.Equals(source.SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase))
        {
            return "Production Ready";
        }

        return "Production Ready";
    }

    private async Task LoadProjectionHistoryAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        var language = ContentLanguageCode;

        var historyItems = await dbContext.ProjectionHistories
            .AsNoTracking()
            .Where(history => history.SermonParagraph != null &&
                              history.SermonParagraph.Sermon != null &&
                              history.SermonParagraph.Sermon.Language == language)
            .OrderByDescending(history => history.ProjectedAt)
            .ThenByDescending(history => history.Id)
            .Take(75)
            .Select(history => new
            {
                history.Id,
                history.ProjectedAt,
                ParagraphId = history.SermonParagraphId,
                history.SermonParagraph!.ParagraphNumber,
                Text = history.SermonParagraph.Text,
                SermonTitle = history.SermonParagraph.Sermon!.Title,
                history.SermonParagraph.Sermon.SermonCode,
                history.SermonParagraph.Sermon.Year,
                SourceName = history.SermonParagraph.Sermon.ContentSource != null
                    ? history.SermonParagraph.Sermon.ContentSource.Name
                    : null,
                SourceDisplayName = history.SermonParagraph.Sermon.ContentSource != null
                    ? history.SermonParagraph.Sermon.ContentSource.DisplayName
                    : null,
                AuthorDisplayName = history.SermonParagraph.Sermon.Author != null
                    ? history.SermonParagraph.Sermon.Author.DisplayName
                    : null,
                AuthorFullName = history.SermonParagraph.Sermon.Author != null
                    ? history.SermonParagraph.Sermon.Author.FullName
                    : null
            })
            .ToListAsync();

        ProjectionHistoryItems.Clear();
        foreach (var history in historyItems.Where(history =>
                     !IsHiddenFromChurchUi(history.SourceName, history.SourceDisplayName) &&
                     !IsDeferredFrankLibrary(history.AuthorFullName, history.AuthorDisplayName)))
        {
            ProjectionHistoryItems.Add(new SavedParagraphViewModel(
                history.Id,
                history.ParagraphId,
                history.SermonTitle,
                history.SermonCode,
                history.Year,
                history.ParagraphNumber,
                CreatePreview(history.Text),
                history.ProjectedAt,
                Loc.T("Fav_KindHistory")));
        }

        OnPropertyChanged(nameof(IsProjectionHistoryEmpty));
    }

    private async Task RefreshSelectedFavoriteStateAsync(int? paragraphId)
    {
        if (paragraphId is null || SelectedParagraph is null || SelectedParagraph.ParagraphId != paragraphId.Value)
        {
            OnPropertyChanged(nameof(FavoriteButtonText));
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            SelectedParagraph.IsFavorite = await dbContext.FavoriteParagraphs
                .AsNoTracking()
                .AnyAsync(favorite => favorite.SermonParagraphId == paragraphId.Value);

            OnPropertyChanged(nameof(FavoriteButtonText));
        }
        catch
        {
            OnPropertyChanged(nameof(FavoriteButtonText));
        }
    }

    private async Task RefreshSelectedBibleFavoriteStateAsync(int? verseId)
    {
        if (verseId is null || SelectedBibleVerse is null || SelectedBibleVerse.VerseId != verseId.Value)
        {
            selectedBibleVerseIsFavorite = false;
            OnPropertyChanged(nameof(FavoriteButtonText));
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var isFavorite = await dbContext.BibleFavoriteVerses
                .AsNoTracking()
                .AnyAsync(favorite => favorite.BibleVerseId == verseId.Value);

            if (SelectedBibleVerse?.VerseId != verseId.Value)
            {
                return;
            }

            selectedBibleVerseIsFavorite = isFavorite;
            OnPropertyChanged(nameof(FavoriteButtonText));
        }
        catch (Exception ex)
        {
            App.LogStartupError("Bible favorite state refresh failed.", ex);
            OnPropertyChanged(nameof(FavoriteButtonText));
        }
    }

    private async Task RecordProjectionHistoryAsync(
        ParagraphResultViewModel paragraph,
        string searchQuery)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            dbContext.ProjectionHistories.Add(new ProjectionHistory
            {
                SermonParagraphId = paragraph.ParagraphId,
                ProjectedAt = DateTime.UtcNow,
                SearchQuery = TrimTo(searchQuery.Trim(), 500)
            });

            await dbContext.SaveChangesAsync();
            await LoadProjectionHistoryAsync();
            StatusText = "Saved to projection history.";
        }
        catch (Exception ex)
        {
            StatusText = $"Projection history update failed: {ex.Message}";
        }
    }

    private void UpdateImportProgress(ImportProgress progress)
    {
        var position = progress.TotalFiles > 0 && progress.CurrentFile > 0
            ? $"File {progress.CurrentFile:N0} of {progress.TotalFiles:N0}: "
            : string.Empty;

        StatusText =
            $"{position}{progress.Message} Imported paragraphs: {progress.ImportedParagraphs:N0}. Skipped: {progress.SkippedFiles:N0}. Errors: {progress.ErrorCount:N0}.";
    }

    private static string CreateSourceName(string displayName)
    {
        var builder = new StringBuilder(displayName.Length);
        var previousWasSeparator = false;

        foreach (var character in displayName.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('_');
            previousWasSeparator = true;
        }

        var name = builder.ToString().Trim('_');
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "source";
        }

        return TrimTo(name, 120);
    }

    private static string CreatePreview(string text)
    {
        return ParagraphDisplayTextCleaner.CreatePreview(text);
    }

    private static string FormatCount(int count, string singular, string plural)
    {
        return count == 1 ? $"1 {singular}" : $"{count:N0} {plural}";
    }

    private string GetSearchResultsPanelTitle()
    {
        var selectedSourceType = SelectedSourceFilter?.Value is { } sourceId
            ? ContentSources.FirstOrDefault(source => source.Id == sourceId)?.SourceType
            : null;

        if (!string.IsNullOrWhiteSpace(selectedSourceType))
        {
            return GetSearchResultsPanelTitleForSourceTypes([selectedSourceType]);
        }

        var resultSourceTypes = allParagraphResults
            .Select(result => result.SourceType)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return GetSearchResultsPanelTitleForSourceTypes(resultSourceTypes);
    }

    private static string GetSearchResultsPanelTitleForSourceTypes(IReadOnlyCollection<string> sourceTypes)
    {
        if (sourceTypes.Count == 1)
        {
            var sourceType = sourceTypes.First();
            if (string.Equals(sourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase))
            {
                return Loc.T("Panel_CircularLetterResults");
            }

            if (string.Equals(sourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase))
            {
                return Loc.T("Panel_SermonResults");
            }
        }

        return Loc.T("Panel_SearchResults");
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string NormalizeFilePathForComparison(string filePath)
    {
        return Path.GetFullPath(filePath).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }

    private static bool CanImportPdfSourceType(string sourceType)
    {
        return string.Equals(sourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeTestSource(ContentSourceViewModel source)
    {
        return LooksLikeTestSource(source.Name, source.DisplayName, source.LocalFolderPath);
    }

    private static bool LooksLikeTestSource(string name, string displayName, string? localFolderPath)
    {
        return displayName.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("test", StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrWhiteSpace(localFolderPath) &&
                localFolderPath.Contains("Ewald Frank Test", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDeferredFrankLibrary(ContentSourceViewModel source)
    {
        return IsDeferredFrankLibrary(source.Name, source.DisplayName, source.LocalFolderPath);
    }

    private static bool IsHiddenFromChurchUi(string? name, string? displayName, string? path = null)
    {
        return LooksLikeTestSource(name ?? string.Empty, displayName ?? string.Empty, path) ||
               IsDeferredFrankLibrary(name, displayName, path);
    }

    private static bool IsDeferredFrankLibrary(string? name, string? displayName, string? path = null)
    {
        if (ShowBrotherFrankLibrary)
        {
            return false;
        }

        return ContainsFrankLibraryMarker(name) ||
               ContainsFrankLibraryMarker(displayName) ||
               ContainsFrankLibraryMarker(path);
    }

    private static bool ContainsFrankLibraryMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("brother_frank", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("ewald_frank", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Ewald Frank", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Brother Frank", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCircularLetterMetadata(SermonMetadata metadata)
    {
        return metadata.Title.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase) ||
               metadata.SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool WouldRepairSourceMetadata(
        SourceDocumentDiagnosticsRow sermon,
        SermonMetadata metadata,
        int? expectedAuthorId)
    {
        return !string.Equals(sermon.Title, metadata.Title, StringComparison.Ordinal) ||
               !string.Equals(sermon.SermonCode, metadata.SermonCode, StringComparison.Ordinal) ||
               sermon.Year != metadata.Year ||
               sermon.Date != metadata.Date ||
               !string.Equals(sermon.Location, metadata.Location, StringComparison.Ordinal) ||
               !string.Equals(sermon.Language, metadata.Language, StringComparison.Ordinal) ||
               expectedAuthorId is null ||
               sermon.AuthorId != expectedAuthorId.Value;
    }

    private static SourceMetadataContext CreateSourceMetadataContext(ContentSourceViewModel source)
    {
        return new SourceMetadataContext(
            source.Id,
            source.Name,
            source.DisplayName,
            source.SourceType);
    }

    private static async Task<int> EnsureSourceRepairAuthorAsync(
        MessageFlowDbContext dbContext,
        SourceMetadataContext sourceContext)
    {
        var authorMetadata = SermonMetadataParser.GetAuthorMetadata(sourceContext);
        var existingAuthor = await dbContext.Authors
            .FirstOrDefaultAsync(author => author.FullName == authorMetadata.FullName) ??
                             await dbContext.Authors.FirstOrDefaultAsync(
                                 author => author.DisplayName == authorMetadata.DisplayName);

        if (existingAuthor is not null)
        {
            var changed = false;
            if (!string.Equals(existingAuthor.FullName, authorMetadata.FullName, StringComparison.Ordinal))
            {
                existingAuthor.FullName = TrimTo(authorMetadata.FullName, 200);
                changed = true;
            }

            if (!string.Equals(existingAuthor.DisplayName, authorMetadata.DisplayName, StringComparison.Ordinal))
            {
                existingAuthor.DisplayName = TrimTo(authorMetadata.DisplayName, 120);
                changed = true;
            }

            if (!string.Equals(existingAuthor.Description, authorMetadata.Description, StringComparison.Ordinal))
            {
                existingAuthor.Description = TrimTo(authorMetadata.Description, 1000);
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync();
            }

            return existingAuthor.Id;
        }

        var author = new Author
        {
            FullName = TrimTo(authorMetadata.FullName, 200),
            DisplayName = TrimTo(authorMetadata.DisplayName, 120),
            Description = TrimTo(authorMetadata.Description, 1000)
        };

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync();

        return author.Id;
    }

    private static void BackupDatabaseFile(string databasePath, string backupPath)
    {
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("The MessageFlow database could not be found.", databasePath);
        }

        var backupDirectory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrWhiteSpace(backupDirectory))
        {
            Directory.CreateDirectory(backupDirectory);
        }

        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        SqliteConnection.ClearAllPools();

        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var destinationConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = backupPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        using var source = new SqliteConnection(sourceConnectionString);
        using var destination = new SqliteConnection(destinationConnectionString);
        source.Open();
        destination.Open();
        source.BackupDatabase(destination);
    }

    private static void RestoreDatabaseFile(string databasePath, string selectedBackupPath)
    {
        if (!File.Exists(selectedBackupPath))
        {
            throw new FileNotFoundException("The selected backup file could not be found.", selectedBackupPath);
        }

        MessageFlowDatabase.EnsureDatabaseDirectory(databasePath);
        if (string.Equals(
                Path.GetFullPath(databasePath),
                Path.GetFullPath(selectedBackupPath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Select a backup file other than the current MessageFlow database.");
        }

        var safetyBackupPath = CreateSafetyBackupPath(databasePath);
        BackupDatabaseFile(databasePath, safetyBackupPath);

        SqliteConnection.ClearAllPools();

        File.Copy(selectedBackupPath, databasePath, overwrite: true);
        DeleteIfExists($"{databasePath}-wal");
        DeleteIfExists($"{databasePath}-shm");

        SqliteConnection.ClearAllPools();
    }

    private static string CreateSafetyBackupPath(string databasePath)
    {
        var databaseDirectory = Path.GetDirectoryName(databasePath) ?? Directory.GetCurrentDirectory();
        var backupDirectory = Path.Combine(databaseDirectory, "backups");
        Directory.CreateDirectory(backupDirectory);
        return Path.Combine(
            backupDirectory,
            $"messageflow_safety_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private bool CanOpenLatestBackupFolder()
    {
        if (IsDatabaseOperationRunning || string.IsNullOrWhiteSpace(LatestBackupPath))
        {
            return false;
        }

        var backupFolder = Path.GetDirectoryName(LatestBackupPath);
        return !string.IsNullOrWhiteSpace(backupFolder) && Directory.Exists(backupFolder);
    }

    private static string CreateAuthorLabel(string displayName, string fullName, int authorId)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return CreateMissingAuthorLabel(authorId);
    }

    private static string CreateMissingAuthorLabel(int authorId)
    {
        return authorId == 1 ? "Brother Branham" : $"Author {authorId}";
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private void ClearSelectedSong()
    {
        var changed = false;

        if (selectedSong is not null)
        {
            selectedSong = null;
            OnPropertyChanged(nameof(SelectedSong));
            changed = true;
        }

        if (selectedSongSection is not null)
        {
            selectedSongSection = null;
            OnPropertyChanged(nameof(SelectedSongSection));
            changed = true;
        }

        if (changed)
        {
            RaiseCurrentSelectionProperties();
        }
    }

    private void ClearNonSongSelection()
    {
        if (selectedParagraph is not null && !isSermonReadingMode)
        {
            selectedParagraph = null;
            OnPropertyChanged(nameof(SelectedParagraph));
        }

        if (selectedBibleVerse is not null)
        {
            selectedBibleVerse = null;
            selectedBibleVerseIsFavorite = false;
            OnPropertyChanged(nameof(SelectedBibleVerse));
        }

        if (selectedBibleNavigationItem is not null)
        {
            selectedBibleNavigationItem = null;
            OnPropertyChanged(nameof(SelectedBibleNavigationItem));
        }
    }

    private void RaiseCurrentSelectionProperties()
    {
        OnPropertyChanged(nameof(SelectedParagraphHeader));
        OnPropertyChanged(nameof(SelectedParagraphMeta));
        OnPropertyChanged(nameof(PreviewHeader));
        OnPropertyChanged(nameof(PreviewMeta));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(ProjectionParagraphTitle));
        OnPropertyChanged(nameof(ProjectionParagraphNumber));
        OnPropertyChanged(nameof(SelectedParagraphText));
        OnPropertyChanged(nameof(HasProjectionPages));
        OnPropertyChanged(nameof(ProjectionPageIndicatorText));
        RaiseProjectionPageCommandStates();
    }

    private bool CanUseCurrentSelection()
    {
        return IsBibleMode
            ? SelectedBibleVerse is not null
            : IsSongsMode
                ? SelectedSongSection is not null
                : SelectedParagraph is not null;
    }

    private bool CanNavigateCurrentSelection()
    {
        if (IsBibleMode)
        {
            return SelectedBibleVerse is not null;
        }

        if (IsSongsMode)
        {
            return SelectedSongSection is not null;
        }

        return IsSermonReadingMode && SelectedParagraph is not null;
    }

    private bool CanNavigatePreviousCurrentSelection()
    {
        if (!CanNavigateCurrentSelection())
        {
            return false;
        }

        if (IsBibleMode)
        {
            return SelectedBibleVerse is not null;
        }

        if (IsSongsMode)
        {
            return SelectedSongSection is not null && SongSections.IndexOf(SelectedSongSection) > 0;
        }

        return SelectedParagraph is not null &&
               focusedSermonParagraphs.FindIndex(paragraph => paragraph.ParagraphId == SelectedParagraph.ParagraphId) > 0;
    }

    private bool CanNavigateNextCurrentSelection()
    {
        if (!CanNavigateCurrentSelection())
        {
            return false;
        }

        if (IsBibleMode)
        {
            return SelectedBibleVerse is not null;
        }

        if (IsSongsMode)
        {
            var currentIndex = SelectedSongSection is null ? -1 : SongSections.IndexOf(SelectedSongSection);
            return currentIndex >= 0 && currentIndex < SongSections.Count - 1;
        }

        if (SelectedParagraph is null)
        {
            return false;
        }

        var paragraphIndex = focusedSermonParagraphs.FindIndex(
            paragraph => paragraph.ParagraphId == SelectedParagraph.ParagraphId);
        return paragraphIndex >= 0 && paragraphIndex < focusedSermonParagraphs.Count - 1;
    }

    private bool CurrentSelectionMatchesActiveProjection()
    {
        return ActiveProjectionContent?.ContentType switch
        {
            ProjectionContentType.Sermon => IsSermonMode &&
                                            SelectedParagraph is not null &&
                                            ActiveProjectionContent.SourceId == SelectedParagraph.SermonId &&
                                            ActiveProjectionContent.ItemId == SelectedParagraph.ParagraphId,
            ProjectionContentType.Bible => IsBibleMode &&
                                           SelectedBibleVerse is not null &&
                                           ActiveProjectionContent.SourceId == SelectedBibleVerse.TranslationId &&
                                           ActiveProjectionContent.ItemId == SelectedBibleVerse.VerseId,
            ProjectionContentType.Song => IsSongsMode &&
                                          SelectedSong is not null &&
                                          SelectedSongSection is not null &&
                                          ActiveProjectionContent.SourceId == SelectedSong.SongId &&
                                          ActiveProjectionContent.ItemId == SelectedSongSection.SectionId,
            _ => false
        };
    }

    private void RaiseCommandStates()
    {
        PreviousParagraphCommand.RaiseCanExecuteChanged();
        NextParagraphCommand.RaiseCanExecuteChanged();
        CopyCommand.RaiseCanExecuteChanged();
        ProjectCommand.RaiseCanExecuteChanged();
        ToggleFavoriteCommand.RaiseCanExecuteChanged();
        BibleSearchCommand.RaiseCanExecuteChanged();
        BackupDatabaseCommand.RaiseCanExecuteChanged();
        RestoreDatabaseCommand.RaiseCanExecuteChanged();
        OpenBackupFolderCommand.RaiseCanExecuteChanged();
        AddNewSourceCommand.RaiseCanExecuteChanged();
        ManageSourcesCommand.RaiseCanExecuteChanged();
        ImportSourceCommand.RaiseCanExecuteChanged();
        RepairSourceMetadataCommand.RaiseCanExecuteChanged();
        RebuildSermonSearchIndexCommand.RaiseCanExecuteChanged();
        ImportBibleCommand.RaiseCanExecuteChanged();
        ClearHistoryCommand.RaiseCanExecuteChanged();
        VerifyProductionDataCommand.RaiseCanExecuteChanged();
        CleanupTestDataCommand.RaiseCanExecuteChanged();
        CleanupBrotherFrankCircularLettersCommand.RaiseCanExecuteChanged();
        TestProjectionDisplayCommand.RaiseCanExecuteChanged();
        OpenProjectionPreviewCommand.RaiseCanExecuteChanged();
        ProjectFavoriteCommand.RaiseCanExecuteChanged();
        RemoveFavoriteCommand.RaiseCanExecuteChanged();
        ProjectBibleFavoriteCommand.RaiseCanExecuteChanged();
        CopyBibleFavoriteCommand.RaiseCanExecuteChanged();
        RemoveBibleFavoriteCommand.RaiseCanExecuteChanged();
        ProjectHistoryCommand.RaiseCanExecuteChanged();
        RaiseProjectionTextSizeCommandStates();
    }

    private void RaiseProjectionTextSizeCommandStates()
    {
        DecreaseProjectionTextSizeCommand.RaiseCanExecuteChanged();
        IncreaseProjectionTextSizeCommand.RaiseCanExecuteChanged();
        ResetProjectionTextSizeCommand.RaiseCanExecuteChanged();
        RaiseProjectionPageCommandStates();
    }

    private void RaiseProjectionPageCommandStates()
    {
        PreviousProjectionPageCommand.RaiseCanExecuteChanged();
        NextProjectionPageCommand.RaiseCanExecuteChanged();
    }

    private void RaiseSermonWithinSearchCommandStates()
    {
        PreviousSermonWithinMatchCommand.RaiseCanExecuteChanged();
        NextSermonWithinMatchCommand.RaiseCanExecuteChanged();
    }

    private static async Task ApplyFavoriteStateAsync(
        MessageFlowDbContext dbContext,
        List<ParagraphResultViewModel> paragraphs,
        CancellationToken cancellationToken)
    {
        if (paragraphs.Count == 0)
        {
            return;
        }

        var paragraphIds = paragraphs.Select(paragraph => paragraph.ParagraphId).ToList();
        var favoriteIds = await dbContext.FavoriteParagraphs
            .AsNoTracking()
            .Where(favorite => paragraphIds.Contains(favorite.SermonParagraphId))
            .Select(favorite => favorite.SermonParagraphId)
            .ToListAsync(cancellationToken);
        if (favoriteIds.Count == 0)
        {
            return;
        }

        var favoriteSet = favoriteIds.ToHashSet();
        foreach (var paragraph in paragraphs)
        {
            paragraph.IsFavorite = favoriteSet.Contains(paragraph.ParagraphId);
        }
    }

    private sealed record SearchSnapshot(
        string QueryText,
        int? AuthorId,
        int? ContentSourceId,
        int? Year,
        int Version,
        bool ProjectBestResult,
        string Language)
    {
        public bool HasFilter => AuthorId is not null || ContentSourceId is not null || Year is not null;

        public bool IsFilterOnlyBrowse => string.IsNullOrWhiteSpace(QueryText) && HasFilter;
    }

    private sealed record BibleNavigationResult(
        IReadOnlyList<BibleNavigationItemViewModel> Items,
        IReadOnlyList<BibleVerseResultViewModel> Verses,
        string StatusText,
        bool AutoPreviewFirstVerse,
        int? SelectedVerseNumber = null);

    private sealed record PdfSourceScanResult(
        List<string> PdfFilePaths,
        List<string> InvalidOrMissingFiles);

    private sealed record SourceDocumentDiagnosticsRow(
        string Title,
        string SermonCode,
        int Year,
        DateTime? Date,
        string? Location,
        string Language,
        int AuthorId,
        string AuthorDisplayName,
        string SourceFilePath);

    private sealed record SuspiciousMetadataResult(
        int Count,
        IReadOnlyList<string> Samples);

    private sealed record SourceMetadataRepairPreview(
        int DocumentCount,
        int DocumentsToRepairCount,
        bool WouldChangeSourceType);

    private sealed record BrotherFrankCircularLetterCleanupPreview(
        int DocumentCount,
        int ParagraphCount,
        int FavoriteCount,
        int HistoryCount,
        IReadOnlyList<string> SampleTitles)
    {
        public static BrotherFrankCircularLetterCleanupPreview Empty { get; } = new(0, 0, 0, 0, []);
    }

    private readonly record struct FilterLoadResult(int LinkedAuthorCount, int LinkedSourceCount, int YearCount);
}
