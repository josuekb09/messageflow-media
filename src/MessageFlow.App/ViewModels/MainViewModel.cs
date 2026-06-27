using System.Collections.ObjectModel;
using System.Windows;
using MessageFlow.App.Infrastructure;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    private ProjectionFontSizeOption? selectedProjectionFontSize;
    private string statusText = "Ready";
    private bool isProjectionOpen;
    private bool isSearching;
    private int resultCount;
    private List<ParagraphResultViewModel> allParagraphResults = [];

    public MainViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;

        PreviousParagraphCommand = new RelayCommand(SelectPreviousParagraph, () => SelectedParagraph is not null);
        NextParagraphCommand = new RelayCommand(SelectNextParagraph, () => SelectedParagraph is not null);
        CopyCommand = new RelayCommand(CopySelectedParagraph, () => SelectedParagraph is not null);
        ProjectCommand = new RelayCommand(ProjectSelectedParagraph);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => SelectedParagraph is not null);
        ClearSearchCommand = new RelayCommand(ClearSearch);

        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Small", 36, 48));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Medium", 48, 64));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Large", 60, 78));
        ProjectionFontSizes.Add(new ProjectionFontSizeOption("Extra Large", 76, 98));
        selectedProjectionFontSize = ProjectionFontSizes[1];
    }

    public event Action? ProjectRequested;

    public ObservableCollection<FilterOption> AuthorFilters { get; } = [];

    public ObservableCollection<FilterOption> YearFilters { get; } = [];

    public ObservableCollection<SermonResultViewModel> SermonResults { get; } = [];

    public ObservableCollection<ParagraphResultViewModel> ParagraphResults { get; } = [];

    public ObservableCollection<ProjectionFontSizeOption> ProjectionFontSizes { get; } = [];

    public RelayCommand PreviousParagraphCommand { get; }

    public RelayCommand NextParagraphCommand { get; }

    public RelayCommand CopyCommand { get; }

    public RelayCommand ProjectCommand { get; }

    public RelayCommand ToggleFavoriteCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

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
                RaiseCommandStates();
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

    public bool IsSearching
    {
        get => isSearching;
        set => SetProperty(ref isSearching, value);
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
        SelectedParagraph?.IsFavorite == true ? "Favorited" : "Add to Favorites";

    public async Task InitializeAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
        await dbContext.Database.MigrateAsync();

        AuthorFilters.Clear();
        AuthorFilters.Add(new FilterOption(null, "All authors"));

        var authors = await dbContext.Authors
            .AsNoTracking()
            .OrderBy(author => author.DisplayName)
            .Select(author => new FilterOption(author.Id, author.DisplayName))
            .ToListAsync();

        foreach (var author in authors)
        {
            AuthorFilters.Add(author);
        }

        YearFilters.Clear();
        YearFilters.Add(new FilterOption(null, "All years"));

        var years = await dbContext.Sermons
            .AsNoTracking()
            .Select(sermon => sermon.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        foreach (var year in years)
        {
            YearFilters.Add(new FilterOption(year, year.ToString()));
        }

        SelectedAuthor = AuthorFilters.FirstOrDefault();
        SelectedYear = YearFilters.FirstOrDefault();
        StatusText = "Type to search sermons and paragraphs.";
    }

    public async Task SearchNowAsync()
    {
        searchDebounce?.Cancel();
        await ExecuteSearchAsync(CancellationToken.None);
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

    private async Task ExecuteSearchAsync(CancellationToken cancellationToken)
    {
        var queryText = SearchText.Trim();
        var hasFilter = SelectedAuthor?.Value is not null || SelectedYear?.Value is not null;

        if (string.IsNullOrWhiteSpace(queryText) && !hasFilter)
        {
            SetResults([]);
            StatusText = "Type to search sermons and paragraphs.";
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

            SetResults(results.Select(result => new ParagraphResultViewModel(result)).ToList());
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

    private void SetResults(List<ParagraphResultViewModel> results)
    {
        allParagraphResults = results;
        ResultCount = allParagraphResults.Count;

        SermonResults.Clear();
        foreach (var sermon in allParagraphResults
                     .GroupBy(result => result.SermonId)
                     .Select(group => new SermonResultViewModel(
                         group.Key,
                         group.First().SermonTitle,
                         group.First().SermonCode,
                         group.First().Year,
                         group.Count()))
                     .OrderBy(sermon => sermon.Year)
                     .ThenBy(sermon => sermon.Title))
        {
            SermonResults.Add(sermon);
        }

        var firstSermon = SermonResults.FirstOrDefault();
        if (SelectedSermon == firstSermon)
        {
            RefreshParagraphResultsForSelectedSermon();
            return;
        }

        SelectedSermon = firstSermon;
    }

    private void RefreshParagraphResultsForSelectedSermon()
    {
        ParagraphResults.Clear();

        var paragraphs = SelectedSermon is null
            ? allParagraphResults
            : allParagraphResults.Where(paragraph => paragraph.SermonId == SelectedSermon.SermonId);

        foreach (var paragraph in paragraphs.OrderBy(paragraph => paragraph.ParagraphNumber))
        {
            ParagraphResults.Add(paragraph);
        }

        SelectedParagraph = ParagraphResults.FirstOrDefault();
    }

    private void SelectPreviousParagraph()
    {
        MoveSelection(-1);
    }

    private void SelectNextParagraph()
    {
        MoveSelection(1);
    }

    private void MoveSelection(int offset)
    {
        if (SelectedParagraph is null || ParagraphResults.Count == 0)
        {
            return;
        }

        var index = ParagraphResults.IndexOf(SelectedParagraph);
        if (index < 0)
        {
            SelectedParagraph = ParagraphResults.FirstOrDefault();
            return;
        }

        var nextIndex = Math.Clamp(index + offset, 0, ParagraphResults.Count - 1);
        SelectedParagraph = ParagraphResults[nextIndex];
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

    private void ProjectSelectedParagraph()
    {
        if (SelectedParagraph is null)
        {
            StatusText = "Please select a paragraph before projecting.";
            return;
        }

        ProjectRequested?.Invoke();
    }

    public void SetProjectionOpen(bool isOpen)
    {
        IsProjectionOpen = isOpen;
    }

    private void ToggleFavorite()
    {
        if (SelectedParagraph is null)
        {
            return;
        }

        SelectedParagraph.IsFavorite = !SelectedParagraph.IsFavorite;
        OnPropertyChanged(nameof(FavoriteButtonText));
        StatusText = SelectedParagraph.IsFavorite ? "Paragraph added to favorites." : "Paragraph removed from favorites.";
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
    }
}
