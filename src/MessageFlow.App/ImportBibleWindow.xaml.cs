using System.Windows;
using MessageFlow.App.ViewModels;
using Microsoft.Win32;

namespace MessageFlow.App;

public partial class ImportBibleWindow : Window
{
    public ImportBibleWindow()
    {
        InitializeComponent();
    }

    public BibleImportPreviewSummary? PreviewSummary { get; private set; }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Bible CSV",
            CheckFileExists = true,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            FilePathBox.Text = dialog.FileName;
            StartImportButton.IsEnabled = false;
            PreviewStatusText.Text = "Click Preview to inspect the CSV.";
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        BuildPreview(showErrors: true);
    }

    private void StartImport_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewSummary is null && !BuildPreview(showErrors: true))
        {
            return;
        }

        if (PreviewSummary?.VerseCount <= 0)
        {
            MessageBox.Show(
                "No valid Bible verses are ready to import.",
                "Import Bible",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private bool BuildPreview(bool showErrors)
    {
        try
        {
            var translationName = TranslationNameBox.Text.Trim();
            var abbreviation = AbbreviationBox.Text.Trim();
            var language = LanguageBox.Text.Trim();
            var description = DescriptionBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(translationName))
            {
                throw new InvalidOperationException("Translation name is required.");
            }

            if (string.IsNullOrWhiteSpace(abbreviation))
            {
                throw new InvalidOperationException("Abbreviation is required.");
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new InvalidOperationException("Language is required.");
            }

            PreviewSummary = BibleCsvImportPreviewBuilder.Build(
                translationName,
                abbreviation,
                language,
                description,
                FilePathBox.Text.Trim());

            DataContext = PreviewSummary;
            PreviewTranslationText.Text = $"{PreviewSummary.TranslationName} ({PreviewSummary.Abbreviation})";
            PreviewVerseCountText.Text = PreviewSummary.VerseCount.ToString("N0");
            PreviewInvalidCountText.Text = PreviewSummary.InvalidRowCount.ToString("N0");
            PreviewStatusText.Text = PreviewSummary.VerseCount == 0
                ? "No valid verses found."
                : "Preview ready.";
            StartImportButton.IsEnabled = PreviewSummary.VerseCount > 0;
            return true;
        }
        catch (Exception ex)
        {
            PreviewSummary = null;
            DataContext = null;
            StartImportButton.IsEnabled = false;
            PreviewStatusText.Text = "Preview failed.";

            if (showErrors)
            {
                MessageBox.Show(
                    ex.Message,
                    "Import Bible Preview",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return false;
        }
    }
}
