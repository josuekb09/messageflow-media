using System.Windows;
using System.Windows.Input;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private ProjectWindow? projectWindow;

    public MainWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        viewModel.ProjectRequested += ShowProjectionWindow;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        await viewModel.InitializeAsync();
    }

    private void ShowProjectionWindow()
    {
        if (projectWindow is not null)
        {
            projectWindow.Activate();
            projectWindow.Focus();
            viewModel.SetProjectionOpen(true);
            return;
        }

        projectWindow = new ProjectWindow(viewModel);
        projectWindow.Closed += (_, _) =>
        {
            projectWindow = null;
            viewModel.SetProjectionOpen(false);
        };

        projectWindow.Show();
        viewModel.SetProjectionOpen(true);
        projectWindow.Activate();
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
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

        if (e.Key == Key.Escape && projectWindow is not null)
        {
            projectWindow.Close();
            e.Handled = true;
        }
    }
}
