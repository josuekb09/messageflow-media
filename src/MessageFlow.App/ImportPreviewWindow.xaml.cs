using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow(ImportPreviewSummary summary)
    {
        InitializeComponent();
        DataContext = summary;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void StartImport_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
