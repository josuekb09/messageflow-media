using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ProductionVerificationWindow : Window
{
    public ProductionVerificationWindow(IReadOnlyList<ProductionVerificationItem> report)
    {
        InitializeComponent();
        WindowPlacement.ConfigureDialog(this, 720, 580, 640, 480, canResize: false);
        DataContext = report;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
