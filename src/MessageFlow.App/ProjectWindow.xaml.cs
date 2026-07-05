using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using MessageFlow.App.ViewModels;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfSize = System.Windows.Size;

namespace MessageFlow.App;

public partial class ProjectWindow : Window
{
    private const double SmallMinimumParagraphFontSize = 34;
    private const double MediumMinimumParagraphFontSize = 42;
    private const double LargeMinimumParagraphFontSize = 50;
    private const double ExtraLargeMinimumParagraphFontSize = 58;
    private const double SmallMaximumParagraphFontSize = 72;
    private const double MediumMaximumParagraphFontSize = 90;
    private const double LargeMaximumParagraphFontSize = 108;
    private const double ExtraLargeMaximumParagraphFontSize = 122;
    private const double FontStep = 2;
    private const string ProjectionTestTitle = "MessageFlow Projection Test";
    private const string ProjectionTestText = "If you can see this on the TV, projection is ready.";

    private readonly MainViewModel viewModel;
    private readonly bool isTestProjection;
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
        : this(viewModel, isTestProjection: false)
    {
    }

    private ProjectWindow(MainViewModel viewModel, bool isTestProjection)
    {
        this.viewModel = viewModel;
        this.isTestProjection = isTestProjection;
        DataContext = viewModel;
        InitializeComponent();

        if (isTestProjection)
        {
            Title = ProjectionTestTitle;
            TitleTextBlock.Text = ProjectionTestTitle;
            ParagraphNumberTextBlock.Text = string.Empty;
        }

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public static ProjectWindow CreateTestWindow(MainViewModel viewModel)
    {
        return new ProjectWindow(viewModel, isTestProjection: true);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        QueueProjectionUpdate(resetPage: true);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueueProjectionUpdate(resetPage: false);
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
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

        if (e.Key is Key.Right or Key.Down or Key.Space)
        {
            ShowNextProjectionItem();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Up or Key.Back)
        {
            ShowPreviousProjectionItem();
            e.Handled = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsFullscreen && e.ButtonState == MouseButtonState.Pressed)
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
        if (isTestProjection)
        {
            if (e.PropertyName == nameof(MainViewModel.ProjectionFontSize) ||
                e.PropertyName == nameof(MainViewModel.ProjectionLineHeight))
            {
                QueueProjectionUpdate(resetPage: false);
            }

            return;
        }

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
        var text = NormalizeProjectionText(GetProjectionText());
        if (string.IsNullOrWhiteSpace(text) ||
            ParagraphStage.ActualWidth <= 0 ||
            ParagraphStage.ActualHeight <= 0)
        {
            projectionPages.Clear();
            ParagraphTextBlock.Text = text;
            PageIndicatorTextBlock.Text = string.Empty;
            return;
        }

        var availableSize = new WpfSize(ParagraphStage.ActualWidth, ParagraphStage.ActualHeight);
        var minimumFontSize = GetMinimumReadableFontSize();
        var selectedFontSize = Math.Max(viewModel.ProjectionFontSize, minimumFontSize);
        var preferredFontSize = GetPreferredParagraphFontSize(text, selectedFontSize);
        var fontSize = FindLargestFittingFontSize(text, availableSize, preferredFontSize, minimumFontSize);

        projectionPages.Clear();
        if (fontSize >= minimumFontSize)
        {
            projectionPages.Add(text);
        }
        else
        {
            fontSize = minimumFontSize;
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
            ? $"Page {currentPageIndex + 1} of {projectionPages.Count}"
            : string.Empty;
    }

    private double FindLargestFittingFontSize(
        string text,
        WpfSize availableSize,
        double preferredFontSize,
        double minimumFontSize)
    {
        for (var fontSize = preferredFontSize; fontSize >= minimumFontSize; fontSize -= FontStep)
        {
            if (DoesTextFit(text, availableSize, fontSize))
            {
                return fontSize;
            }
        }

        return 0;
    }

    private double GetMinimumReadableFontSize()
    {
        return viewModel.SelectedProjectionFontSize?.Label switch
        {
            "Small" => SmallMinimumParagraphFontSize,
            "Medium" => MediumMinimumParagraphFontSize,
            "Large" => LargeMinimumParagraphFontSize,
            "Extra Large" => ExtraLargeMinimumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 70 => ExtraLargeMinimumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 58 => LargeMinimumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 46 => MediumMinimumParagraphFontSize,
            _ => SmallMinimumParagraphFontSize
        };
    }

    private double GetPreferredParagraphFontSize(string text, double selectedFontSize)
    {
        var maximumFontSize = GetMaximumReadableFontSize();
        var wordCount = CountWords(text);

        if (text.Length <= 165 && wordCount <= 34)
        {
            return Math.Clamp(selectedFontSize * 1.45, selectedFontSize, maximumFontSize);
        }

        if (text.Length <= 280 && wordCount <= 52)
        {
            return Math.Clamp(selectedFontSize * 1.25, selectedFontSize, maximumFontSize);
        }

        return Math.Clamp(selectedFontSize, GetMinimumReadableFontSize(), maximumFontSize);
    }

    private double GetMaximumReadableFontSize()
    {
        return viewModel.SelectedProjectionFontSize?.Label switch
        {
            "Small" => SmallMaximumParagraphFontSize,
            "Medium" => MediumMaximumParagraphFontSize,
            "Large" => LargeMaximumParagraphFontSize,
            "Extra Large" => ExtraLargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 80 => ExtraLargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 68 => LargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 56 => MediumMaximumParagraphFontSize,
            _ => SmallMaximumParagraphFontSize
        };
    }

    private IReadOnlyList<string> SplitIntoProjectionPages(string text, WpfSize availableSize, double fontSize)
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
        WpfSize availableSize,
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

    private bool DoesTextFit(string text, WpfSize availableSize, double fontSize)
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

        measuringBlock.Measure(new WpfSize(availableSize.Width, double.PositiveInfinity));

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
        if (isTestProjection)
        {
            return;
        }

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
        if (isTestProjection)
        {
            return;
        }

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

    private static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private string GetProjectionText()
    {
        return isTestProjection ? ProjectionTestText : viewModel.SelectedParagraphText;
    }

    private void ToggleFullscreen()
    {
        if (!IsFullscreen)
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

        EnsureRestoreBounds();
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

    private bool IsFullscreen => isFullscreen || WindowState == WindowState.Maximized;

    private void EnsureRestoreBounds()
    {
        if (restoreWidth > 0 && restoreHeight > 0)
        {
            return;
        }

        var screen = Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        var workArea = screen.WorkingArea;
        var availableWidth = Math.Max(360, workArea.Width - 96);
        var availableHeight = Math.Max(320, workArea.Height - 96);

        restoreWidth = Math.Min(1100, availableWidth);
        restoreHeight = Math.Min(720, availableHeight);
        restoreLeft = workArea.Left + ((workArea.Width - restoreWidth) / 2);
        restoreTop = workArea.Top + ((workArea.Height - restoreHeight) / 2);
        restoreWindowState = WindowState.Normal;
        restoreResizeMode = ResizeMode.NoResize;
    }

    protected override void OnClosed(EventArgs e)
    {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }
}
