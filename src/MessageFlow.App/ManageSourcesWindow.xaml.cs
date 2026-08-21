using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ManageSourcesWindow : Window
{
    public ManageSourcesWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        WindowPlacement.ConfigureDialog(this, 960, 680, 760, 540, canResize: true);
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
