using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageFlow.App.ViewModels;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MessageFlow.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private ProjectWindow? projectWindow;
    private ProjectWindow? testProjectionWindow;
    private AdminToolsWindow? adminToolsWindow;

    public MainWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        viewModel.ProjectRequested += ShowProjectionWindow;
        viewModel.ProjectionTestRequested += ShowProjectionTestWindow;
        viewModel.ProjectionPreviewRequested += ShowWindowedProjectionPreview;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SearchBox.Focus();
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupError("MainWindow initialization failed.", ex);
            viewModel.StatusText = "Startup initialization failed. See logs\\app-startup.log.";

            MessageBox.Show(
                $"MessageFlow opened, but startup initialization failed.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details were written to logs\\app-startup.log.",
                "MessageFlow Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowProjectionWindow()
    {
        try
        {
            var displayTarget = viewModel.ResolveProjectionDisplayTarget();
            CloseTestProjectionWindow();

            if (projectWindow is null)
            {
                projectWindow = new ProjectWindow(viewModel);
                projectWindow.Closed += (_, _) =>
                {
                    projectWindow = null;
                    UpdateProjectionClosedStatus();
                };

                if (ProjectionDisplayService.ShouldUseWindowedPreview(displayTarget))
                {
                    ProjectionDisplayService.PrepareWindowedPreviewWindow(projectWindow, displayTarget);
                }
                else
                {
                    ProjectionDisplayService.PrepareFullscreenWindow(projectWindow, displayTarget);
                }

                projectWindow.Show();
            }

            var useWindowedPreview = ProjectionDisplayService.ShouldUseWindowedPreview(displayTarget);
            if (useWindowedPreview)
            {
                ProjectionDisplayService.ShowWindowedPreviewOnTarget(projectWindow, displayTarget);
            }
            else
            {
                ProjectionDisplayService.MaximizeOnTarget(projectWindow, displayTarget);
            }

            viewModel.ReportProjectionOpened(displayTarget, isTest: false, isWindowedPreview: useWindowedPreview);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Projection window could not be opened.", ex);
            viewModel.StatusText = $"Projection could not open: {ex.Message}";
            UpdateProjectionClosedStatus();
        }
    }

    private void ShowProjectionTestWindow()
    {
        try
        {
            var displayTarget = viewModel.ResolveProjectionDisplayTarget();
            CloseLiveProjectionWindow();

            if (testProjectionWindow is null)
            {
                testProjectionWindow = ProjectWindow.CreateTestWindow(viewModel);
                testProjectionWindow.Closed += (_, _) =>
                {
                    testProjectionWindow = null;
                    UpdateProjectionClosedStatus();
                };

                if (ProjectionDisplayService.ShouldUseWindowedPreview(displayTarget))
                {
                    ProjectionDisplayService.PrepareWindowedPreviewWindow(testProjectionWindow, displayTarget);
                }
                else
                {
                    ProjectionDisplayService.PrepareFullscreenWindow(testProjectionWindow, displayTarget);
                }

                testProjectionWindow.Show();
            }

            var useWindowedPreview = ProjectionDisplayService.ShouldUseWindowedPreview(displayTarget);
            if (useWindowedPreview)
            {
                ProjectionDisplayService.ShowWindowedPreviewOnTarget(testProjectionWindow, displayTarget);
            }
            else
            {
                ProjectionDisplayService.MaximizeOnTarget(testProjectionWindow, displayTarget);
            }

            viewModel.ReportProjectionOpened(displayTarget, isTest: true, isWindowedPreview: useWindowedPreview);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Projection test window could not be opened.", ex);
            viewModel.StatusText = $"Projection test could not open: {ex.Message}";
            UpdateProjectionClosedStatus();
        }
    }

    private void ShowWindowedProjectionPreview()
    {
        try
        {
            var displayTarget = viewModel.ResolveProjectionDisplayTarget();
            CloseTestProjectionWindow();

            if (projectWindow is null)
            {
                projectWindow = new ProjectWindow(viewModel);
                projectWindow.Closed += (_, _) =>
                {
                    projectWindow = null;
                    UpdateProjectionClosedStatus();
                };

                ProjectionDisplayService.PrepareWindowedPreviewWindow(projectWindow, displayTarget);
                projectWindow.Show();
            }

            ProjectionDisplayService.ShowWindowedPreviewOnTarget(projectWindow, displayTarget);
            viewModel.ReportProjectionOpened(displayTarget, isTest: false, isWindowedPreview: true);
        }
        catch (Exception ex)
        {
            App.LogStartupError("Projection preview window could not be opened.", ex);
            viewModel.StatusText = $"Projection preview could not open: {ex.Message}";
            UpdateProjectionClosedStatus();
        }
    }

    private void CloseLiveProjectionWindow()
    {
        projectWindow?.Close();
    }

    private void CloseTestProjectionWindow()
    {
        testProjectionWindow?.Close();
    }

    private void UpdateProjectionClosedStatus()
    {
        if (projectWindow is null && testProjectionWindow is null)
        {
            viewModel.SetProjectionOpen(false);
        }
    }

    private async void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
            TryHandleProjectionTextSizeShortcut(e.Key))
        {
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(LibraryTabs.SelectedItem, BibleTab) &&
            (BibleSearchBox.IsKeyboardFocusWithin || BibleNavigationList.IsKeyboardFocusWithin))
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                await viewModel.ActivateSelectedBibleNavigationItemAsync();
                return;
            }

            if (e.Key is Key.Down or Key.Up && BibleSearchBox.IsKeyboardFocusWithin)
            {
                MoveBibleNavigationSelection(e.Key == Key.Down ? 1 : -1);
                BibleNavigationList.Focus();
                e.Handled = true;
                return;
            }
        }

        if (Keyboard.Modifiers == ModifierKeys.None &&
            (e.Key == Key.Enter || e.Key == Key.Return) &&
            SearchBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            await viewModel.QuickProjectAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.A)
        {
            ShowAdminTools();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && (projectWindow is not null || testProjectionWindow is not null))
        {
            CloseLiveProjectionWindow();
            CloseTestProjectionWindow();
            e.Handled = true;
        }
    }

    private bool TryHandleProjectionTextSizeShortcut(Key key)
    {
        var command = key switch
        {
            Key.Add or Key.OemPlus => viewModel.IncreaseProjectionTextSizeCommand,
            Key.Subtract or Key.OemMinus => viewModel.DecreaseProjectionTextSizeCommand,
            Key.D0 or Key.NumPad0 => viewModel.ResetProjectionTextSizeCommand,
            _ => null
        };

        if (command is null || !command.CanExecute(null))
        {
            return false;
        }

        command.Execute(null);
        return true;
    }

    private void MoveBibleNavigationSelection(int offset)
    {
        var itemCount = viewModel.BibleNavigationItems.Count;
        if (itemCount == 0)
        {
            return;
        }

        var currentIndex = viewModel.SelectedBibleNavigationItem is null
            ? -1
            : viewModel.BibleNavigationItems.IndexOf(viewModel.SelectedBibleNavigationItem);
        var nextIndex = currentIndex < 0
            ? 0
            : Math.Clamp(currentIndex + offset, 0, itemCount - 1);

        viewModel.SelectedBibleNavigationItem = viewModel.BibleNavigationItems[nextIndex];
        BibleNavigationList.ScrollIntoView(viewModel.SelectedBibleNavigationItem);
    }

    private async void FavoritesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FavoritesList.SelectedItem is SavedParagraphViewModel savedParagraph)
        {
            await viewModel.ProjectSavedParagraphAsync(savedParagraph);
        }
    }

    private async void BibleFavoritesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (BibleFavoritesList.SelectedItem is BibleFavoriteVerseViewModel favoriteVerse)
        {
            await viewModel.ProjectBibleFavoriteAsync(favoriteVerse);
        }
    }

    private async void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryList.SelectedItem is SavedParagraphViewModel savedParagraph)
        {
            await viewModel.ProjectSavedParagraphAsync(savedParagraph);
        }
    }

    private async void BibleNavigationList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(BibleNavigationList, source) is not ListBoxItem item ||
            item.DataContext is not BibleNavigationItemViewModel navigationItem)
        {
            return;
        }

        viewModel.SelectedBibleNavigationItem = navigationItem;
        await viewModel.ActivateSelectedBibleNavigationItemAsync();
    }

    private async void LibraryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, LibraryTabs))
        {
            return;
        }

        viewModel.SetBibleMode(ReferenceEquals(LibraryTabs.SelectedItem, BibleTab));

        if (ReferenceEquals(LibraryTabs.SelectedItem, HistoryTab))
        {
            await viewModel.RefreshProjectionHistoryAsync();
        }
    }

    private void Admin_Click(object sender, RoutedEventArgs e)
    {
        ShowAdminTools();
    }

    private void ShowAdminTools()
    {
        if (adminToolsWindow is not null)
        {
            adminToolsWindow.Activate();
            adminToolsWindow.Focus();
            return;
        }

        adminToolsWindow = new AdminToolsWindow(viewModel)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        adminToolsWindow.Closed += (_, _) => adminToolsWindow = null;
        adminToolsWindow.Show();
        adminToolsWindow.Activate();
    }
}
