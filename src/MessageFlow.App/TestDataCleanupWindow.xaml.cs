using System.Windows;
using System.Windows.Controls;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class TestDataCleanupWindow : Window
{
    private const string ConfirmationPhrase = "I understand this removes test source data only.";

    public TestDataCleanupWindow(TestDataCleanupPreview preview)
    {
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 940, 700, 760, 540);
        DataContext = preview;
    }

    private void ConfirmationBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CleanupButton.IsEnabled = string.Equals(
            ConfirmationBox.Text.Trim(),
            ConfirmationPhrase,
            StringComparison.Ordinal);
    }

    private void Cleanup_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
