using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageFlow.App.ViewModels;

namespace MessageFlow.App;

public partial class ProjectWindow : Window
{
    private const double MinimumParagraphFontSize = 34;
    private const double MaximumParagraphFontSize = 72;
    private const double FontStep = 2;

    private readonly MainViewModel viewModel;
    private readonly List<string> projectionPages = [];
    private bool isFullscreen;
    private bool updateQueued;
    private int currentPageIndex;
    private double restoreLeft;
    private double restoreTop;
    private double restoreWidth;
    private double restoreHeight;
    private WindowState restoreWindowState;
    private ResizeMode restoreResizeMode;

    public ProjectWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        WindowPlacement.FitToWorkArea(this, 1100, 720, 800, 520);

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        QueueProjectionUpdate(resetPage: true);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueueProjectionUpdate(resetPage: false);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right)
        {
            ShowNextProjectionItem();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Left)
        {
            ShowPreviousProjectionItem();
            e.Handled = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!isFullscreen && e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedParagraphText))
        {
            QueueProjectionUpdate(resetPage: true);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.ProjectionFontSize) ||
            e.PropertyName == nameof(MainViewModel.ProjectionLineHeight) ||
            e.PropertyName == nameof(MainViewModel.ProjectionParagraphTitle) ||
            e.PropertyName == nameof(MainViewModel.ProjectionParagraphNumber))
        {
            QueueProjectionUpdate(resetPage: false);
        }
    }

    private void QueueProjectionUpdate(bool resetPage)
    {
        if (resetPage)
        {
            currentPageIndex = 0;
        }

        if (updateQueued)
        {
            return;
        }

        updateQueued = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            updateQueued = false;
            UpdateProjectionLayout();
        }));
    }

    private void UpdateProjectionLayout()
    {
        var text = NormalizeProjectionText(viewModel.SelectedParagraphText);
        if (string.IsNullOrWhiteSpace(text) ||
            ParagraphStage.ActualWidth <= 0 ||
            ParagraphStage.ActualHeight <= 0)
        {
            projectionPages.Clear();
            ParagraphTextBlock.Text = text;
            PageIndicatorTextBlock.Text = string.Empty;
            return;
        }

        var availableSize = new Size(ParagraphStage.ActualWidth, ParagraphStage.ActualHeight);
        var preferredFontSize = Math.Clamp(viewModel.ProjectionFontSize, MinimumParagraphFontSize, MaximumParagraphFontSize);
        var fontSize = FindLargestFittingFontSize(text, availableSize, preferredFontSize);

        projectionPages.Clear();
        if (fontSize >= MinimumParagraphFontSize)
        {
            projectionPages.Add(text);
        }
        else
        {
            fontSize = MinimumParagraphFontSize;
            projectionPages.AddRange(SplitIntoProjectionPages(text, availableSize, fontSize));
        }

        if (projectionPages.Count == 0)
        {
            projectionPages.Add(text);
        }

        currentPageIndex = Math.Clamp(currentPageIndex, 0, projectionPages.Count - 1);
        ApplyProjectionPage(fontSize);
    }

    private void ApplyProjectionPage(double fontSize)
    {
        ParagraphTextBlock.FontSize = fontSize;
        ParagraphTextBlock.LineHeight = CalculateLineHeight(fontSize);
        ParagraphTextBlock.Text = projectionPages[currentPageIndex];
        PageIndicatorTextBlock.Text = projectionPages.Count > 1
            ? $"{currentPageIndex + 1}/{projectionPages.Count}"
            : string.Empty;
    }

    private double FindLargestFittingFontSize(string text, Size availableSize, double preferredFontSize)
    {
        for (var fontSize = preferredFontSize; fontSize >= MinimumParagraphFontSize; fontSize -= FontStep)
        {
            if (DoesTextFit(text, availableSize, fontSize))
            {
                return fontSize;
            }
        }

        return 0;
    }

    private IReadOnlyList<string> SplitIntoProjectionPages(string text, Size availableSize, double fontSize)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pages = new List<string>();
        var start = 0;

        while (start < words.Length)
        {
            var end = FindLargestFittingWordEnd(words, start, availableSize, fontSize);
            end = PreferSentenceBoundary(words, start, end);

            if (end <= start)
            {
                end = start + 1;
            }

            pages.Add(string.Join(' ', words[start..end]));
            start = end;
        }

        return pages;
    }

    private int FindLargestFittingWordEnd(
        string[] words,
        int start,
        Size availableSize,
        double fontSize)
    {
        var low = start + 1;
        var high = words.Length;
        var best = start + 1;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = string.Join(' ', words[start..middle]);

            if (DoesTextFit(candidate, availableSize, fontSize))
            {
                best = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return best;
    }

    private static int PreferSentenceBoundary(string[] words, int start, int end)
    {
        const int minimumWordsBeforeBreak = 8;

        for (var index = end - 1; index > start + minimumWordsBeforeBreak; index--)
        {
            if (EndsSentence(words[index]))
            {
                return index + 1;
            }
        }

        return end;
    }

    private bool DoesTextFit(string text, Size availableSize, double fontSize)
    {
        var measuringBlock = new TextBlock
        {
            Text = text,
            FontFamily = ParagraphTextBlock.FontFamily,
            FontWeight = ParagraphTextBlock.FontWeight,
            FontStyle = ParagraphTextBlock.FontStyle,
            FontStretch = ParagraphTextBlock.FontStretch,
            FontSize = fontSize,
            LineHeight = CalculateLineHeight(fontSize),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Width = availableSize.Width
        };

        measuringBlock.Measure(new Size(availableSize.Width, double.PositiveInfinity));

        return measuringBlock.DesiredSize.Width <= availableSize.Width + 1 &&
               measuringBlock.DesiredSize.Height <= availableSize.Height + 1;
    }

    private double CalculateLineHeight(double fontSize)
    {
        var ratio = viewModel.ProjectionFontSize > 0
            ? viewModel.ProjectionLineHeight / viewModel.ProjectionFontSize
            : 1.28;

        return fontSize * Math.Clamp(ratio, 1.18, 1.34);
    }

    private void ShowNextProjectionItem()
    {
        if (projectionPages.Count > 1 && currentPageIndex < projectionPages.Count - 1)
        {
            currentPageIndex++;
            ApplyProjectionPage(ParagraphTextBlock.FontSize);
            return;
        }

        if (viewModel.NextParagraphCommand.CanExecute(null))
        {
            viewModel.NextParagraphCommand.Execute(null);
        }
    }

    private void ShowPreviousProjectionItem()
    {
        if (projectionPages.Count > 1 && currentPageIndex > 0)
        {
            currentPageIndex--;
            ApplyProjectionPage(ParagraphTextBlock.FontSize);
            return;
        }

        if (viewModel.PreviousParagraphCommand.CanExecute(null))
        {
            viewModel.PreviousParagraphCommand.Execute(null);
        }
    }

    private static bool EndsSentence(string word)
    {
        var trimmed = word.TrimEnd('"', '\'', ')', ']', '}');
        return trimmed.EndsWith(".", StringComparison.Ordinal) ||
               trimmed.EndsWith("!", StringComparison.Ordinal) ||
               trimmed.EndsWith("?", StringComparison.Ordinal);
    }

    private static string NormalizeProjectionText(string text)
    {
        return string.Join(' ', text.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            restoreLeft = Left;
            restoreTop = Top;
            restoreWidth = Width;
            restoreHeight = Height;
            restoreWindowState = WindowState;
            restoreResizeMode = ResizeMode;

            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            isFullscreen = true;
            QueueProjectionUpdate(resetPage: false);
            return;
        }

        WindowState = WindowState.Normal;
        Left = restoreLeft;
        Top = restoreTop;
        Width = restoreWidth;
        Height = restoreHeight;
        ResizeMode = restoreResizeMode;
        WindowState = restoreWindowState == WindowState.Maximized ? WindowState.Normal : restoreWindowState;
        isFullscreen = false;
        QueueProjectionUpdate(resetPage: false);
    }

    protected override void OnClosed(EventArgs e)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }
}
