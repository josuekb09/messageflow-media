using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow(ImportPreviewSummary summary)
    {
        InitializeComponent();
        WindowPlacement.ConfigureDialog(this, 640, 560, 560, 500, canResize: false);
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
