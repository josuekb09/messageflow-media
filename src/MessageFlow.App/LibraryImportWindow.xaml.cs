using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace MessageFlow.App;

public partial class LibraryImportWindow : Window
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly MainViewModel mainViewModel;

    public LibraryImportWindow(IServiceScopeFactory scopeFactory, MainViewModel mainViewModel)
    {
        this.scopeFactory = scopeFactory;
        this.mainViewModel = mainViewModel;
        InitializeComponent();
        DataContext = this;
        WindowPlacement.ConfigureDialog(this, 960, 680, 800, 560, canResize: true);
    }

    public ObservableCollection<LibraryImportCandidate> Candidates { get; } = [];

    private string SelectedContentType =>
        (ContentTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? LocalLibraryImportService.SermonType;

    private async void BrowseFiles_Click(object sender, RoutedEventArgs e)
    {
        var isSermon = SelectedContentType == LocalLibraryImportService.SermonType;
        var dialog = new OpenFileDialog
        {
            Title = isSermon ? "Choose Brother Frank text-based PDFs" : "Choose Song files",
            Multiselect = true,
            CheckFileExists = true,
            Filter = isSermon
                ? "PDF files (*.pdf)|*.pdf"
                : "Supported Song files (*.ppt;*.pptx;*.txt;*.pdf)|*.ppt;*.pptx;*.txt;*.pdf|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            await ScanFilesAsync(dialog.FileNames);
        }
    }

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose a local folder to scan. MessageFlow will not download files.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var extensions = SelectedContentType == LocalLibraryImportService.SermonType
            ? new HashSet<string>([".pdf"], StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>([".ppt", ".pptx", ".txt", ".pdf"], StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(dialog.SelectedPath, "*", SearchOption.TopDirectoryOnly)
            .Where(path => extensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        await ScanFilesAsync(files);
    }

    private async Task ScanFilesAsync(IEnumerable<string> files)
    {
        var paths = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Candidates.Clear();
        ImportButton.IsEnabled = false;
        StatusTextBlock.Text = $"Scanning {paths.Count:N0} local file(s)...";

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var service = new LocalLibraryImportService(dbContext);
            foreach (var path in paths)
            {
                Candidates.Add(await service.ScanAsync(path, SelectedContentType));
            }

            var ready = Candidates.Count(candidate => candidate.CanImport);
            var selected = Candidates.Count(candidate => candidate.IsSelected);
            StatusTextBlock.Text = $"Scan complete: {ready:N0} supported, {selected:N0} selected. Review titles, conflicts, and unsupported items before importing.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Library Import Scan", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = $"Scan failed: {ex.Message}";
        }
        finally
        {
            ImportButton.IsEnabled = true;
        }
    }

    private async void ImportSelected_Click(object sender, RoutedEventArgs e)
    {
        CandidatesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        CandidatesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var selected = Candidates.Where(candidate => candidate.IsSelected && candidate.CanImport).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Select at least one supported item to import.", "Library Import", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.Any(candidate => string.IsNullOrWhiteSpace(candidate.Title)))
        {
            MessageBox.Show(this, "Every selected item needs a title.", "Library Import", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ImportButton.IsEnabled = false;
        StatusTextBlock.Text = $"Importing {selected.Count:N0} selected item(s)...";
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageFlowDbContext>();
            var service = new LocalLibraryImportService(dbContext);
            var imported = await service.ImportAsync(selected);
            await mainViewModel.RefreshLibrariesAfterImportAsync();
            StatusTextBlock.Text = $"Imported {imported:N0} item(s). They are searchable now and remain available after restart.";
            MessageBox.Show(this, $"Imported {imported:N0} item(s) into MessageFlow-managed storage.", "Library Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed and the current item was rolled back.\n\n{ex.Message}", "Library Import", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = $"Import failed: {ex.Message}";
        }
        finally
        {
            ImportButton.IsEnabled = true;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
