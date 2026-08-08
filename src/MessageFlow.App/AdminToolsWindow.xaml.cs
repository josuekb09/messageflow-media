using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class AdminToolsWindow : Window
{
    private readonly MainViewModel viewModel;

    public AdminToolsWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 1120, 760, 860, 560);
        DataContext = viewModel;
    }

    private void OpenLibraryImport_Click(object sender, RoutedEventArgs e)
    {
        var window = new LibraryImportWindow(viewModel.ScopeFactory, viewModel)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
