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
        SourceTypeBox.ItemsSource = ContentSourceTypeOption.All;
        SourceTypeBox.SelectedItem = ContentSourceTypeOption.All[0];
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

        if (string.Equals(SourceTypeValue, "SermonPdfCollection", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(LocalFolderPathBox.Text))
        {
            ValidationMessage.Text = "Local Folder Path is required for sermon PDF collections.";
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
}
