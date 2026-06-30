using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ManageSourcesWindow : Window
{
    public ManageSourcesWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
