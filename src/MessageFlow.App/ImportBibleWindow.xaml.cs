using System.Windows;
using System.Windows.Controls;
using System.IO;
using MessageFlow.App.Localization;
using MessageFlow.App.ViewModels;
using Microsoft.Win32;

namespace MessageFlow.App;

public partial class ImportBibleWindow : Window
{
    private bool isUpdatingPreview;

    public ImportBibleWindow()
    {
        InitializeComponent();
        WindowPlacement.ConfigureDialog(this, 880, 700, 720, 560, canResize: true);
        SuggestedFileText.Visibility = Visibility.Visible;
    }

    public BibleImportPreviewSummary? PreviewSummary { get; private set; }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.T("ImportBible_SelectCsvDialog"),
            CheckFileExists = true,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && Directory.Exists(documents))
        {
            dialog.InitialDirectory = documents;
        }

        if (dialog.ShowDialog(this) == true)
        {
            FilePathBox.Text = dialog.FileName;
            ResetPreview(Loc.T("ImportBible_ClickPreview"));
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
                Loc.T("ImportBible_NoCleanPreview"),
                Loc.T("ImportBible_Title"),
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
                throw new InvalidOperationException(Loc.T("ImportBible_NameRequired"));
            }

            if (string.IsNullOrWhiteSpace(abbreviation))
            {
                throw new InvalidOperationException(Loc.T("ImportBible_AbbreviationRequired"));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new InvalidOperationException(Loc.T("ImportBible_LanguageRequired"));
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
                ? Loc.T("ImportBible_NoValidVerses")
                : canImport
                    ? Loc.T("ImportBible_PreviewReady")
                    : Loc.T("ImportBible_FixInvalidRows");
            StartImportButton.IsEnabled = canImport;
            isUpdatingPreview = false;
            return true;
        }
        catch (Exception ex)
        {
            ResetPreview(Loc.T("ImportBible_PreviewFailed"));

            if (showErrors)
            {
                MessageBox.Show(
                    ex.Message,
                    Loc.T("ImportBible_PreviewErrorTitle"),
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

        ResetPreview(Loc.T("ImportBible_ClickPreview"));
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
