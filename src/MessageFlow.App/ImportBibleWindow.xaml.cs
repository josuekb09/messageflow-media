using System.Windows;
using System.Windows.Controls;
using System.IO;
using MessageFlow.App.ViewModels;
using Microsoft.Win32;

namespace MessageFlow.App;

public partial class ImportBibleWindow : Window
{
    private const string SuggestedKjvFolder = @"D:\Bible\KJV";
    private const string SuggestedKjvFile = @"D:\Bible\KJV\kjv.csv";
    private bool isUpdatingPreview;

    public ImportBibleWindow()
    {
        InitializeComponent();
        SuggestedFileText.Visibility = File.Exists(SuggestedKjvFile)
            ? Visibility.Visible
            : Visibility.Collapsed;
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

        if (Directory.Exists(SuggestedKjvFolder))
        {
            dialog.InitialDirectory = SuggestedKjvFolder;
            if (File.Exists(SuggestedKjvFile))
            {
                dialog.FileName = Path.GetFileName(SuggestedKjvFile);
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            FilePathBox.Text = dialog.FileName;
            ResetPreview("Click Preview to inspect the CSV.");
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        BuildPreview(showErrors: true);
    }

    private void StartImport_Click(object sender, RoutedEventArgs e)
    {
        var preview = PreviewSummary;
        if (preview is null)
        {
            if (!BuildPreview(showErrors: true))
            {
                return;
            }

            preview = PreviewSummary;
        }

        if (preview is null)
        {
            return;
        }

        if (preview.VerseCount <= 0 || preview.InvalidRowCount > 0)
        {
            MessageBox.Show(
                "No clean Bible preview is ready to import.",
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

            isUpdatingPreview = true;
            DataContext = PreviewSummary;
            PreviewTranslationText.Text = $"{PreviewSummary.TranslationName} ({PreviewSummary.Abbreviation})";
            PreviewVerseCountText.Text = PreviewSummary.VerseCount.ToString("N0");
            PreviewInvalidCountText.Text = PreviewSummary.InvalidRowCount.ToString("N0");
            NoInvalidRowsPanel.Visibility = PreviewSummary.InvalidRowCount == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            InvalidRowsList.Visibility = PreviewSummary.InvalidRowCount == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

            var canImport = PreviewSummary.VerseCount > 0 && PreviewSummary.InvalidRowCount == 0;
            PreviewStatusText.Text = PreviewSummary.VerseCount == 0
                ? "No valid verses found."
                : canImport
                    ? "Preview ready."
                    : "Fix invalid rows before importing.";
            StartImportButton.IsEnabled = canImport;
            isUpdatingPreview = false;
            return true;
        }
        catch (Exception ex)
        {
            ResetPreview("Preview failed.");

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

    private void InputChanged(object sender, TextChangedEventArgs e)
    {
        if (isUpdatingPreview || StartImportButton is null)
        {
            return;
        }

        ResetPreview("Click Preview to inspect the CSV.");
    }

    private void ResetPreview(string statusText)
    {
        PreviewSummary = null;
        DataContext = null;
        StartImportButton.IsEnabled = false;
        PreviewStatusText.Text = statusText;
        PreviewTranslationText.Text = "-";
        PreviewVerseCountText.Text = "0";
        PreviewInvalidCountText.Text = "0";
        NoInvalidRowsPanel.Visibility = Visibility.Visible;
        InvalidRowsList.Visibility = Visibility.Collapsed;
    }
}
