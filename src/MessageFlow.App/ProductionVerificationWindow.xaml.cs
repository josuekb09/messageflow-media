using System.Windows;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ProductionVerificationWindow : Window
{
    public ProductionVerificationWindow(IReadOnlyList<ProductionVerificationItem> report)
    {
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 900, 680, 700, 520);
        DataContext = report;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
