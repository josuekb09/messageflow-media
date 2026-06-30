using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ManageSourcesWindow : Window
{
    public ManageSourcesWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 1080, 740, 840, 560);
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
