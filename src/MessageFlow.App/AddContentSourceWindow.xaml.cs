using System.IO;
using System.Windows;
using MessageFlow.App.ViewModels;
using Microsoft.Win32;

namespace MessageFlow.App;

public partial class AddContentSourceWindow : Window
{
    public AddContentSourceWindow()
    {
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 660, 640, 560, 500);
        SourceTypeBox.ItemsSource = ContentSourceTypeOption.All;
        SourceTypeBox.SelectedItem = ContentSourceTypeOption.All[0];
        UpdateFolderSuggestion();
        DisplayNameBox.Focus();
    }

    public string DisplayNameValue => DisplayNameBox.Text.Trim();

    public string SourceTypeValue =>
        SourceTypeBox.SelectedItem is ContentSourceTypeOption option
            ? option.Value
            : ContentSourceTypeOption.All[0].Value;

    public string DescriptionValue => DescriptionBox.Text.Trim();

    public string? LocalFolderPathValue
    {
        get
        {
            var value = LocalFolderPathBox.Text.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Local Source Folder",
            Multiselect = false
        };

        var currentPath = LocalFolderPathBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
        {
            dialog.InitialDirectory = currentPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            LocalFolderPathBox.Text = dialog.FolderName;
        }
    }

    private void SourceTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateFolderSuggestion();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ValidationMessage.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(DisplayNameValue))
        {
            ValidationMessage.Text = "Display Name is required.";
            DisplayNameBox.Focus();
            return;
        }

        if (SourceTypeBox.SelectedItem is not ContentSourceTypeOption)
        {
            ValidationMessage.Text = "Source Type is required.";
            SourceTypeBox.Focus();
            return;
        }

        if (RequiresLocalFolder(SourceTypeValue) &&
            string.IsNullOrWhiteSpace(LocalFolderPathBox.Text))
        {
            ValidationMessage.Text = "Local Folder Path is required for PDF source types.";
            LocalFolderPathBox.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(LocalFolderPathBox.Text) && !Directory.Exists(LocalFolderPathBox.Text.Trim()))
        {
            ValidationMessage.Text = "The selected local folder does not exist.";
            LocalFolderPathBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UpdateFolderSuggestion()
    {
        FolderSuggestionText.Text = SourceTypeValue switch
        {
            "CircularLetter" =>
                "Suggested folder example:\nD:\\Ewald Frank\\Circular Letters\\PDF",
            "SermonPdfCollection" =>
                "Suggested folder examples:\nD:\\Br William Marrion Branham\\PDF\nD:\\Ewald Frank\\Sermons\\PDF",
            _ =>
                "Choose a local folder when this source type needs files imported later."
        };
    }

    private static bool RequiresLocalFolder(string sourceType)
    {
        return string.Equals(sourceType, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase);
    }
}
