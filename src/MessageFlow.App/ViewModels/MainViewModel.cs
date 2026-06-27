using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using MessageFlow.App.Infrastructure;
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

public sealed class MainViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;
    private CancellationTokenSource? searchDebounce;
    private string searchText = string.Empty;
    private FilterOption? selectedAuthor;
    private FilterOption? selectedYear;
    private SermonResultViewModel? selectedSermon;
    private ParagraphResultViewModel? selectedParagraph;
    private SavedParagraphViewModel? selectedFavoriteParagraph;
    private SavedParagraphViewModel? selectedHistoryParagraph;
    private ContentSourceViewModel? selectedContentSource;
    private ProjectionFontSizeOption? selectedProjectionFontSize;
    private string statusText = "Ready";
    private string? latestBackupPath;
    private bool isProjectionOpen;
    private bool isSearching;
    private bool isDatabaseOperationRunning;
    private int resultCount;
    private List<ParagraphResultViewModel> allParagraphResults = [];

    public MainViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;

        AuthorFilters.Add(new FilterOption(null, "All authors"));
        YearFilters.Add(new FilterOption(null, "All years"));
        selectedAuthor = AuthorFilters[0];
        selectedYear = YearFilters[0];

        PreviousParagraphCommand = new RelayCommand(SelectPreviousParagraph, () => SelectedParagraph is not null);
        NextParagraphCommand = new RelayCommand(SelectNextParagraph, () => SelectedParagraph is not null);
        CopyCommand = new RelayCommand(CopySelectedParagraph, () => SelectedParagraph is not null);
        ProjectCommand = new RelayCommand(ProjectSelectedParagraph);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => SelectedParagraph is not null);
        ClearSearchCommand = new RelayCommand(ClearSearch);
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
        ImportSourceCommand = new RelayCommand(
            () => _ = ImportSelectedSourceAsync(),
            () => SelectedContentSource is not null && !IsDatabaseOperationRunning);

        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Small", 36, 48));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Medium", 48, 64));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Large", 60, 78));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Extra Large", 76, 98));
        selectedProjectionFontSize = ProjectionFontSizes.First(option => option.Label == "Medium");
    }

    public event Action? ProjectRequested;

    public ObservableCollection<FilterOption> AuthorFilters { get; } = [];

    public ObservableCollection<FilterOption> YearFilters { get; } = [];

    public ObservableCollection<SermonResultViewModel> SermonResults { get; } = [];

    public ObservableCollection<ParagraphResultViewModel> ParagraphResults { get; } = [];

    public ObservableCollection<SavedParagraphViewModel> FavoriteParagraphs { get; } = [];

    public ObservableCollection<SavedParagraphViewModel> ProjectionHistoryItems { get; } = [];

    public ObservableCollection<ContentSourceViewModel> ContentSources { get; } = [];

    public ObservableCollection<ProjectionFontSizeOption> ProjectionFontSizes { get; } = [];

    public RelayCommand PreviousParagraphCommand { get; }

    public RelayCommand NextParagraphCommand { get; }

    public RelayCommand CopyCommand { get; }

    public RelayCommand ProjectCommand { get; }

    public RelayCommand ToggleFavoriteCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public RelayCommand BackupDatabaseCommand { get; }

    public RelayCommand RestoreDatabaseCommand { get; }

    public RelayCommand OpenBackupFolderCommand { get; }

    public RelayCommand AddNewSourceCommand { get; }

    public RelayCommand ImportSourceCommand { get; }

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
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
                RefreshParagraphResultsForSelectedSermon();
            }
        }
    }

    public ParagraphResultViewModel? SelectedParagraph
    {
        get => selectedParagraph;
        set
        {
            if (SetProperty(ref selectedParagraph, value))
            {
                OnPropertyChanged(nameof(SelectedParagraphHeader));
                OnPropertyChanged(nameof(SelectedParagraphMeta));
                OnPropertyChanged(nameof(ProjectionParagraphTitle));
                OnPropertyChanged(nameof(ProjectionParagraphNumber));
                OnPropertyChanged(nameof(SelectedParagraphText));
                OnPropertyChanged(nameof(FavoriteButtonText));
                _ = RefreshSelectedFavoriteStateAsync(value?.ParagraphId);
                if (IsProjectionOpen && value is not null)
                {
                    _ = RecordProjectionHistoryAsync(value, SearchText);
                }

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
            }
        }
    }

    public ProjectionFontSizeOption? SelectedProjectionFontSize
    {
        get => selectedProjectionFontSize;
        set
        {
            if (SetProperty(ref selectedProjectionFontSize, value))
            {
                OnPropertyChanged(nameof(ProjectionFontSize));
                OnPropertyChanged(nameof(ProjectionLineHeight));
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
        set => SetProperty(ref isSearching, value);
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
                ImportSourceCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
        set => SetProperty(ref resultCount, value);
    }

    public string ProjectionStatusText =>
        IsProjectionOpen ? "Projection: Open" : "Projection: Closed";

    public string SelectedParagraphHeader =>
        SelectedParagraph is null
            ? "No paragraph selected"
            : $"{SelectedParagraph.SermonTitle}";

    public string SelectedParagraphMeta =>
        SelectedParagraph is null
            ? "Search and select a paragraph to preview it here."
            : $"{SelectedParagraph.SermonCode} | {SelectedParagraph.Year} | Paragraph {SelectedParagraph.ParagraphNumber}";

    public string ProjectionParagraphTitle =>
        SelectedParagraph?.SermonTitle ?? "MessageFlow";

    public string ProjectionParagraphNumber =>
        SelectedParagraph is null ? string.Empty : $"Paragraph {SelectedParagraph.ParagraphNumber}";

    public string SelectedParagraphText =>
        SelectedParagraph?.FullParagraphText ?? string.Empty;

    public double ProjectionFontSize =>
        SelectedProjectionFontSize?.FontSize ?? 48;

    public double ProjectionLineHeight =>
        SelectedProjectionFontSize?.LineHeight ?? 64;

    public string FavoriteButtonText =>
        SelectedParagraph?.IsFavorite == true ? "Remove Favorite" : "Add Favorite";

    public bool IsProjectionHistoryEmpty => ProjectionHistoryItems.Count == 0;

    public async Task InitializeAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var startupMessages = new List<string>();

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

        StatusText = startupMessages.Count == 0
            ? "Type to search sermons and paragraphs."
            : string.Join(' ', startupMessages);
    }

    public Task RefreshProjectionHistoryAsync()
    {
        return LoadProjectionHistoryAsync();
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
            "Restoring will replace the current database. Continue?",
            "Restore MessageFlow Database",
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
            Owner = Application.Current.MainWindow
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

    private async Task ImportSelectedSourceAsync()
    {
        var source = SelectedContentSource;
        if (source is null)
        {
            StatusText = "Select a source before importing.";
            return;
        }

        if (!string.Equals(source.SourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase))
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
            StatusText = "The selected source folder could not be found.";
            MessageBox.Show(
                "The selected source folder could not be found. Edit or recreate the source with a valid local folder.",
                "MessageFlow Sources",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            $"Import local PDF files from:{Environment.NewLine}{source.LocalFolderPath}{Environment.NewLine}{Environment.NewLine}Existing imported files will be skipped.",
            $"Import {source.DisplayName}",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsDatabaseOperationRunning = true;
            StatusText = $"Scanning PDFs for {source.DisplayName}...";

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

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                await ExecuteSearchAsync(CancellationToken.None);
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

    public async Task SearchNowAsync()
    {
        searchDebounce?.Cancel();
        await ExecuteSearchAsync(CancellationToken.None);
    }

    public async Task QuickProjectAsync()
    {
        searchDebounce?.Cancel();
        await ExecuteSearchAsync(CancellationToken.None, projectBestResult: true);
    }

    private void QueueSearch()
    {
        searchDebounce?.Cancel();
        searchDebounce = new CancellationTokenSource();
        var cancellationToken = searchDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, cancellationToken);
                var operation = Application.Current.Dispatcher.InvokeAsync(
                    () => ExecuteSearchAsync(cancellationToken));

                await operation.Task.Unwrap();
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);
    }

    private async Task ExecuteSearchAsync(
        CancellationToken cancellationToken,
        bool projectBestResult = false)
    {
        var queryText = SearchText.Trim();
        var hasFilter = SelectedAuthor?.Value is not null || SelectedYear?.Value is not null;

        if (string.IsNullOrWhiteSpace(queryText) && !hasFilter)
        {
            SetResults([]);
            StatusText = projectBestResult
                ? "No matching paragraph found."
                : "Type to search sermons and paragraphs.";
            return;
        }

        try
        {
            IsSearching = true;
            StatusText = "Searching...";

            await using var scope = scopeFactory.CreateAsyncScope();
            var searchService = scope.ServiceProvider.GetRequiredService<ISermonSearchService>();

            var hasSelectedFilter = SelectedAuthor?.Value is not null || SelectedYear?.Value is not null;
            var results = hasSelectedFilter
                ? await searchService.SearchAsync(
                    new SermonSearchQuery(
                        AuthorId: SelectedAuthor?.Value,
                        SearchText: string.IsNullOrWhiteSpace(queryText) ? null : queryText,
                        Year: SelectedYear?.Value,
                        MaxResults: 200),
                    cancellationToken)
                : await searchService.SearchAsync(queryText, 200, cancellationToken);

            var resultViewModels = results
                .Select(result => new ParagraphResultViewModel(result))
                .ToList();
            var preferredParagraphId = resultViewModels.FirstOrDefault()?.ParagraphId;

            SetResults(resultViewModels, preferredParagraphId);

            if (projectBestResult)
            {
                if (SelectedParagraph is null)
                {
                    StatusText = "No matching paragraph found.";
                    return;
                }

                await ProjectCurrentSelectionAsync(recordHistory: !IsProjectionOpen);
                return;
            }

            StatusText = ResultCount == 0 ? "No results found." : $"{ResultCount} paragraph results.";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetResults([]);
            StatusText = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private void SetResults(
        List<ParagraphResultViewModel> results,
        int? preferredParagraphId = null)
    {
        allParagraphResults = results;
        ResultCount = allParagraphResults.Count;
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
                             group.Count())
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
    }

    private async void SelectPreviousParagraph()
    {
        await MoveSelectionAsync(-1);
    }

    private async void SelectNextParagraph()
    {
        await MoveSelectionAsync(1);
    }

    private async Task MoveSelectionAsync(int offset)
    {
        var currentParagraph = SelectedParagraph;
        if (currentParagraph is null)
        {
            return;
        }

        try
        {
            var adjacentParagraph = await LoadAdjacentParagraphAsync(currentParagraph, offset);
            if (adjacentParagraph is null)
            {
                StatusText = offset > 0
                    ? "Already at the last paragraph."
                    : "Already at the first paragraph.";
                return;
            }

            var sermonParagraphs = await LoadSermonParagraphsAsync(currentParagraph.SermonId);

            ParagraphResults.Clear();
            foreach (var paragraph in sermonParagraphs)
            {
                ParagraphResults.Add(paragraph);
            }

            SelectedParagraph = ParagraphResults.FirstOrDefault(
                                    paragraph => paragraph.ParagraphId == adjacentParagraph.ParagraphId) ??
                                adjacentParagraph;

            StatusText = $"Selected Paragraph {SelectedParagraph.ParagraphNumber}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Could not move paragraph selection: {ex.Message}";
        }
    }

    private void CopySelectedParagraph()
    {
        if (SelectedParagraph is null)
        {
            return;
        }

        Clipboard.SetText(SelectedParagraph.FullParagraphText);
        StatusText = "Paragraph copied.";
    }

    private async void ProjectSelectedParagraph()
    {
        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        await ProjectCurrentSelectionAsync(recordHistory: true);
    }

    public void SetProjectionOpen(bool isOpen)
    {
        IsProjectionOpen = isOpen;
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

    private async Task ProjectCurrentSelectionAsync(bool recordHistory)
    {
        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        if (recordHistory)
        {
            await RecordProjectionHistoryAsync(SelectedParagraph, SearchText);
        }

        if (!recordHistory)
        {
            StatusText = $"Projecting Paragraph {SelectedParagraph.ParagraphNumber}.";
        }

        ProjectRequested?.Invoke();
    }

    private async void ToggleFavorite()
    {
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
            row.PageNumber)
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

    private async Task<List<ParagraphResultViewModel>> LoadSermonParagraphsAsync(int sermonId)
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
                IsFavorite = paragraph.Favorites.Any()
            })
            .ToListAsync();

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
                row.PageNumber)
            {
                IsFavorite = row.IsFavorite
            })
            .ToList();
    }

    private async Task LoadFavoritesAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var favorites = await dbContext.FavoriteParagraphs
            .AsNoTracking()
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
                favorite.SermonParagraph.Sermon.Year
            })
            .ToListAsync();

        FavoriteParagraphs.Clear();
        foreach (var favorite in favorites)
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
                "Favorite"));
        }
    }

    private async Task ReloadAfterDatabaseRestoreAsync()
    {
        searchDebounce?.Cancel();
        selectedSermon = null;
        selectedParagraph = null;
        selectedFavoriteParagraph = null;
        selectedHistoryParagraph = null;
        selectedContentSource = null;
        allParagraphResults = [];

        SermonResults.Clear();
        ParagraphResults.Clear();
        FavoriteParagraphs.Clear();
        ProjectionHistoryItems.Clear();
        ContentSources.Clear();
        ResultCount = 0;

        OnPropertyChanged(nameof(SelectedSermon));
        OnPropertyChanged(nameof(SelectedParagraph));
        OnPropertyChanged(nameof(SelectedFavoriteParagraph));
        OnPropertyChanged(nameof(SelectedHistoryParagraph));
        OnPropertyChanged(nameof(SelectedContentSource));
        OnPropertyChanged(nameof(SelectedParagraphHeader));
        OnPropertyChanged(nameof(SelectedParagraphMeta));
        OnPropertyChanged(nameof(ProjectionParagraphTitle));
        OnPropertyChanged(nameof(ProjectionParagraphNumber));
        OnPropertyChanged(nameof(SelectedParagraphText));
        OnPropertyChanged(nameof(FavoriteButtonText));
        OnPropertyChanged(nameof(IsProjectionHistoryEmpty));

        await InitializeAsync();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            await ExecuteSearchAsync(CancellationToken.None);
        }

        RaiseCommandStates();
    }

    private async Task<FilterLoadResult> LoadFilterOptionsAsync(
        MessageFlowDbContext dbContext,
        int? preferredAuthorId = null,
        int? preferredYear = null)
    {
        var linkedAuthorIds = await dbContext.Sermons
            .AsNoTracking()
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
            .Where(sermon => sermon.Year > 0)
            .Select(sermon => sermon.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        AuthorFilters.Clear();
        AuthorFilters.Add(new FilterOption(null, "All authors"));
        foreach (var author in linkedAuthors)
        {
            AuthorFilters.Add(author);
        }

        YearFilters.Clear();
        YearFilters.Add(new FilterOption(null, "All years"));
        foreach (var year in years)
        {
            YearFilters.Add(new FilterOption(year, year.ToString()));
        }

        selectedAuthor = AuthorFilters.FirstOrDefault(author => author.Value == preferredAuthorId) ?? AuthorFilters[0];
        selectedYear = YearFilters.FirstOrDefault(year => year.Value == preferredYear) ?? YearFilters[0];
        OnPropertyChanged(nameof(SelectedAuthor));
        OnPropertyChanged(nameof(SelectedYear));

        App.LogStartupMessage(
            $"Loaded filter data. Authors: {linkedAuthors.Count}. Years: {years.Count}.");

        return new FilterLoadResult(linkedAuthors.Count, years.Count);
    }

    private async Task RefreshFilterOptionsPreservingSelectionAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        await LoadFilterOptionsAsync(dbContext, SelectedAuthor?.Value, SelectedYear?.Value);
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

        SelectedContentSource = preferredSourceId is null
            ? ContentSources.FirstOrDefault(source => source.Id == SelectedContentSource?.Id) ??
              ContentSources.FirstOrDefault()
            : ContentSources.FirstOrDefault(source => source.Id == preferredSourceId.Value) ??
              ContentSources.FirstOrDefault();

        if (sources.Count == 0)
        {
            StatusText = "No content sources configured yet.";
        }
    }

    private async Task LoadProjectionHistoryAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();

        var historyItems = await dbContext.ProjectionHistories
            .AsNoTracking()
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
                history.SermonParagraph.Sermon.Year
            })
            .ToListAsync();

        ProjectionHistoryItems.Clear();
        foreach (var history in historyItems)
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
                "History"));
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
        const int maxLength = 160;

        var preview = string.Join(' ', text.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return preview.Length <= maxLength
            ? preview
            : $"{preview[..maxLength].TrimEnd()}...";
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
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

    private void RaiseCommandStates()
    {
        PreviousParagraphCommand.RaiseCanExecuteChanged();
        NextParagraphCommand.RaiseCanExecuteChanged();
        CopyCommand.RaiseCanExecuteChanged();
        ProjectCommand.RaiseCanExecuteChanged();
        ToggleFavoriteCommand.RaiseCanExecuteChanged();
        BackupDatabaseCommand.RaiseCanExecuteChanged();
        RestoreDatabaseCommand.RaiseCanExecuteChanged();
        OpenBackupFolderCommand.RaiseCanExecuteChanged();
        AddNewSourceCommand.RaiseCanExecuteChanged();
        ImportSourceCommand.RaiseCanExecuteChanged();
    }

    private readonly record struct FilterLoadResult(int LinkedAuthorCount, int YearCount);
}
