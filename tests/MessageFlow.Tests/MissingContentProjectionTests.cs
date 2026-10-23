using System.IO;
using System.Reflection;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Tests;

/// <summary>
/// A missing sermon paragraph, verse or song must never put anything on the projection screen:
/// the screen keeps what it showed and the operator is told why in the status bar.
/// </summary>
public sealed class MissingContentProjectionTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"mf-tests-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider provider;

    public MissingContentProjectionTests()
    {
        provider = new ServiceCollection()
            .AddDbContext<MessageFlowDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"))
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>().Database.EnsureCreated();
    }

    public void Dispose()
    {
        provider.Dispose();
        File.Delete(databasePath);
    }

    private MainViewModel CreateViewModel() => new(provider.GetRequiredService<IServiceScopeFactory>());

    private static ProjectedContentSnapshot LiveVerse() =>
        new(ProjectionContentType.Bible, "John 3:16", string.Empty, "For God so loved the world.");

    private static void SetLive(MainViewModel viewModel, ProjectedContentSnapshot snapshot) =>
        typeof(MainViewModel).GetProperty(nameof(MainViewModel.ActiveProjectionContent))!.SetValue(viewModel, snapshot);

    [Fact]
    public void A_missing_sermon_paragraph_is_not_projected_and_the_previous_selection_is_not_projected_instead()
    {
        Sta.Run(async () =>
        {
            var viewModel = CreateViewModel();
            var live = LiveVerse();
            SetLive(viewModel, live);

            // A favorite or history entry whose paragraph is no longer in the library.
            var saved = new SavedParagraphViewModel(1, 987654, "Removed Sermon", "00-0000", 1965, 12, "Preview", DateTime.UtcNow, "Favorite");

            await viewModel.ProjectSavedParagraphAsync(saved);

            Assert.Same(live, viewModel.ActiveProjectionContent);
            Assert.Contains("Nothing was projected", viewModel.StatusText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void A_missing_bible_verse_text_is_not_projected()
    {
        Sta.Run(async () =>
        {
            var viewModel = CreateViewModel();
            var live = LiveVerse();
            SetLive(viewModel, live);

            var favorite = new BibleFavoriteVerseViewModel(
                1, 31102, 1, "King James Version", "KJV", 66, "Revelation", 66, 22, 21, string.Empty, DateTime.UtcNow);

            await viewModel.ProjectBibleFavoriteAsync(favorite);

            Assert.Same(live, viewModel.ActiveProjectionContent);
            Assert.Contains("Nothing was projected", viewModel.StatusText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void A_missing_song_section_is_not_projected()
    {
        Sta.Run(async () =>
        {
            var viewModel = CreateViewModel();
            var live = LiveVerse();
            SetLive(viewModel, live);

            // A song that is not in the library has no sections to project.
            viewModel.SelectedSong = new SongResultViewModel(new SongSearchResult(
                424242, null, "Missing Song", "Songs", "missing.pptx", "missing.pptx", string.Empty, "Slide 1", string.Empty));
            await Task.Delay(200);
            await InvokeProjectSongAsync(viewModel);

            Assert.Same(live, viewModel.ActiveProjectionContent);

            // A section whose stored text is empty is refused the same way.
            viewModel.SelectedSongSection = new SongSectionViewModel(1, 424242, 1, "Verse", "Slide 1", string.Empty);
            await InvokeProjectSongAsync(viewModel);

            Assert.Same(live, viewModel.ActiveProjectionContent);
            Assert.Contains("Nothing was projected", viewModel.StatusText, StringComparison.Ordinal);
        });
    }

    private static Task InvokeProjectSongAsync(MainViewModel viewModel) =>
        (Task)typeof(MainViewModel)
            .GetMethod("ProjectCurrentSongSelectionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewModel, null)!;
}
