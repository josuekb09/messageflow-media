using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MessageFlow.App.ViewModels;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfSize = System.Windows.Size;

namespace MessageFlow.App;

public partial class ProjectWindow : Window
{
    private const double SmallMinimumParagraphFontSize = 36;
    private const double MediumMinimumParagraphFontSize = 46;
    private const double LargeMinimumParagraphFontSize = 56;
    private const double ExtraLargeMinimumParagraphFontSize = 64;
    private const double SmallMaximumParagraphFontSize = 112;
    private const double MediumMaximumParagraphFontSize = 132;
    private const double LargeMaximumParagraphFontSize = 154;
    private const double ExtraLargeMaximumParagraphFontSize = 176;
    private const double FontStep = 2;
    private const double EmergencyMinimumFontSize = 12;
    private const double MaximumFitPrecision = 0.25;
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
        viewModel.PreviousProjectionPageRequested += ShowPreviousProjectionPage;
        viewModel.NextProjectionPageRequested += ShowNextProjectionPage;
    }

    public static ProjectWindow CreateTestWindow(MainViewModel viewModel)
    {
        return new ProjectWindow(viewModel, isTestProjection: true);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateProjectionSafeMargins();
        QueueProjectionUpdate(resetPage: true);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateProjectionSafeMargins();
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

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
            TryHandleProjectionTextSizeShortcut(e.Key))
        {
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Right or Key.Down or Key.Space)
        {
            ShowNextProjectionPage();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Up or Key.Back)
        {
            ShowPreviousProjectionPage();
            e.Handled = true;
        }
    }

    private bool TryHandleProjectionTextSizeShortcut(Key key)
    {
        var command = key switch
        {
            Key.Add or Key.OemPlus => viewModel.IncreaseProjectionTextSizeCommand,
            Key.Subtract or Key.OemMinus => viewModel.DecreaseProjectionTextSizeCommand,
            Key.D0 or Key.NumPad0 => viewModel.ResetProjectionTextSizeCommand,
            _ => null
        };

        if (command is null || !command.CanExecute(null))
        {
            return false;
        }

        command.Execute(null);
        return true;
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

        if (e.PropertyName == nameof(MainViewModel.ActiveProjectionContent))
        {
            QueueProjectionUpdate(resetPage: true);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.ProjectionFontSize) ||
            e.PropertyName == nameof(MainViewModel.ProjectionLineHeight) ||
            e.PropertyName == nameof(MainViewModel.ProjectionFontScale))
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

    private void UpdateProjectionSafeMargins()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var usesSingleScreenFit = !isTestProjection &&
                                  (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection);
        var usesTitleOnlySongLayout = IsSongTitleOnlySlide(GetProjectionTextForDisplay());
        var horizontalMargin = usesSingleScreenFit
            ? usesTitleOnlySongLayout
                ? Math.Clamp(ActualWidth * 0.04, 28, 78)
                : Math.Clamp(ActualWidth * 0.03, 24, 58)
            : Math.Clamp(ActualWidth * 0.045, 36, 86);
        var verticalMargin = usesSingleScreenFit
            ? usesTitleOnlySongLayout
                ? Math.Clamp(ActualHeight * 0.04, 24, 72)
                : Math.Clamp(ActualHeight * 0.03, 20, 58)
            : Math.Clamp(ActualHeight * 0.04, 28, 70);
        var safeMargin = new Thickness(
            horizontalMargin,
            verticalMargin,
            horizontalMargin,
            verticalMargin);

        if (Math.Abs(ProjectionRoot.Margin.Left - safeMargin.Left) > 0.5 ||
            Math.Abs(ProjectionRoot.Margin.Top - safeMargin.Top) > 0.5 ||
            Math.Abs(ProjectionRoot.Margin.Right - safeMargin.Right) > 0.5 ||
            Math.Abs(ProjectionRoot.Margin.Bottom - safeMargin.Bottom) > 0.5)
        {
            ProjectionRoot.Margin = safeMargin;
        }
    }

    private void UpdateProjectionLayout()
    {
        UpdateProjectionSafeMargins();
        ConfigureContentLayout();
        ProjectionRoot.UpdateLayout();

        var text = GetProjectionTextForDisplay();
        if (string.IsNullOrWhiteSpace(text) ||
            ParagraphStage.ActualWidth <= 0 ||
            ParagraphStage.ActualHeight <= 0)
        {
            projectionPages.Clear();
            ParagraphTextBlock.Text = text;
            PageIndicatorTextBlock.Text = string.Empty;
            viewModel.ClearProjectionPageState();
            return;
        }

        var availableSize = new WpfSize(ParagraphStage.ActualWidth, ParagraphStage.ActualHeight);

        if (!isTestProjection && (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection))
        {
            var fullAvailableSize = new WpfSize(
                Math.Max(0, ProjectionRoot.ActualWidth),
                Math.Max(0, ProjectionRoot.ActualHeight));
            UpdateSingleScreenProjection(text, fullAvailableSize);
            return;
        }

        // Sermons remain paginated because their paragraphs can be too long for a readable single screen.
        var minimumFontSize = GetMinimumReadableFontSize();
        var selectedFontSize = Math.Max(viewModel.ProjectionFontSize, minimumFontSize);
        var preferredFontSize = GetPreferredParagraphFontSize(text, selectedFontSize);
        var singlePageMinimumFontSize = ShouldPreferPaging(text, preferredFontSize, selectedFontSize)
            ? Math.Max(minimumFontSize, preferredFontSize - 10)
            : minimumFontSize;
        var fontSize = FindLargestFittingFontSize(
            text,
            availableSize,
            preferredFontSize,
            singlePageMinimumFontSize);

        projectionPages.Clear();
        if (fontSize >= singlePageMinimumFontSize)
        {
            projectionPages.Add(text);
        }
        else
        {
            fontSize = Math.Max(minimumFontSize, preferredFontSize);
            projectionPages.AddRange(SplitIntoProjectionPages(text, availableSize, fontSize));
        }

        if (projectionPages.Count == 0)
        {
            projectionPages.Add(text);
        }

        currentPageIndex = Math.Clamp(currentPageIndex, 0, projectionPages.Count - 1);
        ApplyProjectionPage(fontSize);
        ApplyResponsiveSermonTitle(fontSize);
    }

    private void ConfigureContentLayout()
    {
        var usesSingleScreenFit = !isTestProjection &&
                                  (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection);
        var usesSongLayout = !isTestProjection && viewModel.IsLiveSongProjection;
        var usesTitleOnlySongLayout = usesSongLayout && IsSongTitleOnlySlide(GetProjectionTextForDisplay());

        // Bible/song projection uses a compact header and single-screen fit for church readability.
        TitleTextBlock.Text = usesSongLayout
            ? string.Empty
            : isTestProjection
                ? ProjectionTestTitle
                : viewModel.ActiveProjectionContent?.Title ?? string.Empty;
        TitleTextBlock.Visibility = usesSongLayout ? Visibility.Collapsed : Visibility.Visible;
        TitleTextBlock.FontWeight = usesSingleScreenFit ? FontWeights.Bold : FontWeights.SemiBold;
        TitleTextBlock.FontSize = usesSingleScreenFit ? 48 : 30;
        TitleTextBlock.MaxHeight = usesSingleScreenFit ? double.PositiveInfinity : 96;
        TitleTextBlock.Margin = usesSongLayout
            ? new Thickness(0)
            : usesSingleScreenFit
            ? new Thickness(0, 0, 0, 12)
            : new Thickness(0);
        ParagraphNumberTextBlock.Margin = usesSingleScreenFit
            ? new Thickness(0, 0, 0, 8)
            : new Thickness(0, 12, 0, 28);
        ParagraphNumberTextBlock.Visibility = usesSongLayout ||
                                              (usesSingleScreenFit &&
                                               string.IsNullOrWhiteSpace(viewModel.ProjectionParagraphNumber))
            ? Visibility.Collapsed
            : Visibility.Visible;
        ParagraphTextBlock.FontWeight = usesSingleScreenFit ? FontWeights.Bold : FontWeights.SemiBold;
        ParagraphTextBlock.TextWrapping = TextWrapping.Wrap;
        PageIndicatorTextBlock.Visibility = usesSingleScreenFit
            ? Visibility.Collapsed
            : Visibility.Visible;
        ParagraphTextBlock.TextAlignment = usesTitleOnlySongLayout ? TextAlignment.Center : TextAlignment.Left;
        ParagraphTextBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        ParagraphTextBlock.VerticalAlignment = VerticalAlignment.Center;
    }

    private void UpdateSingleScreenProjection(string text, WpfSize availableSize)
    {
        if (viewModel.IsLiveSongProjection && IsSongTitleSlide())
        {
            UpdateSongTitleProjection(text, availableSize);
            return;
        }

        var minimumFontSize = viewModel.IsLiveBibleProjection ? 46d : 42d;
        var viewportMaximum = GetSingleScreenMaximumFontSize(availableSize);
        var maximumFit = FindMaximumFittingFontSize(
            text,
            availableSize,
            EmergencyMinimumFontSize,
            viewportMaximum);
        var manualScale = Math.Min(1d, viewModel.ProjectionFontScale);
        var fontSize = maximumFit > 0
            ? Math.Max(EmergencyMinimumFontSize, maximumFit * manualScale)
            : 0;

        if (fontSize <= 0)
        {
            fontSize = EmergencyMinimumFontSize;
            App.LogStartupMessage(
                $"Single-screen projection text exceeds the available area at {EmergencyMinimumFontSize:0}px " +
                $"({(viewModel.IsLiveBibleProjection ? "Bible" : "Song")}, {text.Length} characters).");
        }
        else if (fontSize < minimumFontSize)
        {
            App.LogStartupMessage(
                $"{(viewModel.IsLiveBibleProjection ? "Bible" : "Song")} projection required {fontSize:0.##}px, " +
                $"below the {minimumFontSize:0}px readable target, to keep all text on one screen.");
        }

        projectionPages.Clear();
        projectionPages.Add(text);
        currentPageIndex = 0;
        ApplyProjectionPage(fontSize);
        if (viewModel.IsLiveBibleProjection)
        {
            TitleTextBlock.FontSize = fontSize;
            TitleTextBlock.LineHeight = CalculateLineHeight(fontSize);
        }

        App.LogStartupMessage(
            $"{(viewModel.IsLiveBibleProjection ? "Bible" : "Song")} maximum-fit projection: " +
            $"{fontSize:0.##}px in {availableSize.Width:0}x{availableSize.Height:0}.");
    }

    private void UpdateSongTitleProjection(string text, WpfSize availableSize)
    {
        var viewportMaximum = GetSongTitleMaximumFontSize(availableSize);
        var maximumFit = FindMaximumFittingFontSize(
            text,
            availableSize,
            34,
            viewportMaximum);
        var manualScale = Math.Min(1d, viewModel.ProjectionFontScale);
        var fontSize = maximumFit > 0
            ? Math.Max(EmergencyMinimumFontSize, maximumFit * manualScale)
            : EmergencyMinimumFontSize;

        projectionPages.Clear();
        projectionPages.Add(text);
        currentPageIndex = 0;
        ApplyProjectionPage(fontSize);

        App.LogStartupMessage(
            $"Song title-fit projection: {fontSize:0.##}px in {availableSize.Width:0}x{availableSize.Height:0}.");
    }

    private static double GetSingleScreenMaximumFontSize(WpfSize availableSize)
    {
        var viewportBound = Math.Min(availableSize.Height * 0.62, availableSize.Width * 0.22);
        return Math.Clamp(viewportBound, 96, 360);
    }

    private static double GetSongTitleMaximumFontSize(WpfSize availableSize)
    {
        var viewportBound = Math.Min(availableSize.Height * 0.18, availableSize.Width * 0.07);
        return Math.Clamp(viewportBound, 54, 118);
    }

    private double FindMaximumFittingFontSize(
        string text,
        WpfSize availableSize,
        double minimumFontSize,
        double maximumFontSize)
    {
        if (!DoesTextFit(text, availableSize, minimumFontSize))
        {
            return 0;
        }

        var low = minimumFontSize;
        var high = maximumFontSize;
        while (high - low > MaximumFitPrecision)
        {
            var candidate = low + ((high - low) / 2);
            if (DoesProjectionFit(text, availableSize, candidate))
            {
                low = candidate;
            }
            else
            {
                high = candidate;
            }
        }

        return Math.Floor(low / MaximumFitPrecision) * MaximumFitPrecision;
    }

    private void ApplyProjectionPage(double fontSize)
    {
        ParagraphTextBlock.FontSize = fontSize;
        ParagraphTextBlock.LineHeight = CalculateLineHeight(fontSize);
        ParagraphTextBlock.Text = projectionPages[currentPageIndex];
        PageIndicatorTextBlock.Text = projectionPages.Count > 1
            ? $"Page {currentPageIndex + 1} of {projectionPages.Count}"
            : string.Empty;
        viewModel.ReportProjectionPageState(currentPageIndex, projectionPages.Count);
    }

    private void ApplyResponsiveSermonTitle(double bodyFontSize)
    {
        if (!isTestProjection && (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection))
        {
            return;
        }

        var headerFontSize = Math.Clamp(bodyFontSize * 0.68, 30, 72);
        TitleTextBlock.FontWeight = FontWeights.Bold;
        TitleTextBlock.FontSize = headerFontSize;
        TitleTextBlock.LineHeight = headerFontSize * 1.12;
        ParagraphNumberTextBlock.FontSize = Math.Clamp(bodyFontSize * 0.52, 24, 52);
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
            _ when viewModel.ProjectionFontSize >= 90 => ExtraLargeMinimumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 76 => LargeMinimumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 62 => MediumMinimumParagraphFontSize,
            _ => SmallMinimumParagraphFontSize
        };
    }

    private double GetPreferredParagraphFontSize(string text, double selectedFontSize)
    {
        var maximumFontSize = GetMaximumReadableFontSize();
        var wordCount = CountWords(text);

        if (text.Length <= 190 && wordCount <= 38)
        {
            return Math.Clamp(selectedFontSize * 1.7, selectedFontSize, maximumFontSize);
        }

        if (text.Length <= 320 && wordCount <= 58)
        {
            return Math.Clamp(selectedFontSize * 1.35, selectedFontSize, maximumFontSize);
        }

        return Math.Clamp(selectedFontSize, GetMinimumReadableFontSize(), maximumFontSize);
    }

    private static bool ShouldPreferPaging(string text, double preferredFontSize, double selectedFontSize)
    {
        var wordCount = CountWords(text);
        return text.Length > 420 ||
               wordCount > 70 ||
               (preferredFontSize > selectedFontSize + 8 && wordCount > 44);
    }

    private double GetMaximumReadableFontSize()
    {
        return viewModel.SelectedProjectionFontSize?.Label switch
        {
            "Small" => SmallMaximumParagraphFontSize,
            "Medium" => MediumMaximumParagraphFontSize,
            "Large" => LargeMaximumParagraphFontSize,
            "Extra Large" => ExtraLargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 90 => ExtraLargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 76 => LargeMaximumParagraphFontSize,
            _ when viewModel.ProjectionFontSize >= 62 => MediumMaximumParagraphFontSize,
            _ => SmallMaximumParagraphFontSize
        };
    }

    private IReadOnlyList<string> SplitIntoProjectionPages(string text, WpfSize availableSize, double fontSize)
    {
        var words = text.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
        const int minimumWordsOnLastPage = 10;

        for (var index = end - 1; index > start + minimumWordsBeforeBreak; index--)
        {
            if (EndsSentence(words[index]))
            {
                var remainingWords = words.Length - (index + 1);
                if (remainingWords > 0 && remainingWords < minimumWordsOnLastPage)
                {
                    continue;
                }

                return index + 1;
            }
        }

        return end;
    }

    private bool DoesTextFit(string text, WpfSize availableSize, double fontSize)
    {
        if (availableSize.Width <= 0 || availableSize.Height <= 0 || fontSize <= 0)
        {
            return false;
        }

        var measuringBlock = new TextBlock
        {
            Text = text,
            FontFamily = ParagraphTextBlock.FontFamily,
            FontWeight = ParagraphTextBlock.FontWeight,
            FontStyle = ParagraphTextBlock.FontStyle,
            FontStretch = ParagraphTextBlock.FontStretch,
            FontSize = fontSize,
            LineHeight = CalculateLineHeight(fontSize),
            TextAlignment = TextAlignment.Left,
            TextWrapping = ParagraphTextBlock.TextWrapping,
        };

        measuringBlock.Measure(new WpfSize(availableSize.Width, double.PositiveInfinity));
        var tolerance = GetDpiAwareFitTolerance();

        return measuringBlock.DesiredSize.Width <= availableSize.Width + tolerance.Width &&
               measuringBlock.DesiredSize.Height <= availableSize.Height + tolerance.Height;
    }

    private bool DoesProjectionFit(string text, WpfSize availableSize, double fontSize)
    {
        if (availableSize.Width <= 0 || availableSize.Height <= 0 || fontSize <= 0)
        {
            return false;
        }

        var title = TitleTextBlock.Visibility == Visibility.Visible ? TitleTextBlock.Text : string.Empty;
        var titleBlock = new TextBlock
        {
            Text = title,
            FontFamily = ParagraphTextBlock.FontFamily,
            FontWeight = FontWeights.Bold,
            FontStyle = ParagraphTextBlock.FontStyle,
            FontStretch = ParagraphTextBlock.FontStretch,
            FontSize = fontSize,
            LineHeight = CalculateLineHeight(fontSize),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        var bodyBlock = new TextBlock
        {
            Text = text,
            FontFamily = ParagraphTextBlock.FontFamily,
            FontWeight = FontWeights.Bold,
            FontStyle = ParagraphTextBlock.FontStyle,
            FontStretch = ParagraphTextBlock.FontStretch,
            FontSize = fontSize,
            LineHeight = CalculateLineHeight(fontSize),
            TextAlignment = TextAlignment.Left,
            TextWrapping = ParagraphTextBlock.TextWrapping,
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            titleBlock.Measure(new WpfSize(availableSize.Width, double.PositiveInfinity));
        }

        bodyBlock.Measure(new WpfSize(availableSize.Width, double.PositiveInfinity));
        var gap = string.IsNullOrWhiteSpace(title) ? 0 : Math.Clamp(availableSize.Height * 0.018, 10, 18);
        var tolerance = GetDpiAwareFitTolerance();
        return Math.Max(titleBlock.DesiredSize.Width, bodyBlock.DesiredSize.Width) <= availableSize.Width + tolerance.Width &&
               titleBlock.DesiredSize.Height + gap + bodyBlock.DesiredSize.Height <= availableSize.Height + tolerance.Height;
    }

    private WpfSize GetDpiAwareFitTolerance()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return new WpfSize(
            1d / Math.Max(1d, dpi.DpiScaleX),
            1d / Math.Max(1d, dpi.DpiScaleY));
    }

    private double CalculateLineHeight(double fontSize)
    {
        if (!isTestProjection && (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection))
        {
            return fontSize * (viewModel.IsLiveBibleProjection ? 1.2 : 1.18);
        }

        var ratio = viewModel.ProjectionFontSize > 0
            ? viewModel.ProjectionLineHeight / viewModel.ProjectionFontSize
            : 1.28;

        return fontSize * Math.Clamp(ratio, 1.18, 1.34);
    }

    private void ShowNextProjectionPage()
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
    }

    private void ShowPreviousProjectionPage()
    {
        if (isTestProjection)
        {
            return;
        }

        if (projectionPages.Count > 1 && currentPageIndex > 0)
        {
            currentPageIndex--;
            ApplyProjectionPage(ParagraphTextBlock.FontSize);
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
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var lines = normalized
            .Split('\n', StringSplitOptions.TrimEntries)
            .Select(line => string.Join(' ', line.Split(
                [' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }

    private string GetProjectionTextForDisplay()
    {
        var text = GetProjectionText();
        return !isTestProjection && viewModel.IsLiveSongProjection
            ? NormalizeProjectionLineEndings(text)
            : NormalizeProjectionText(text);
    }

    private static string NormalizeProjectionLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }

    private bool IsSongTitleOnlySlide(string text)
    {
        return IsSongTitleSlide();
    }

    private bool IsSongTitleSlide()
    {
        return !isTestProjection &&
               viewModel.IsLiveSongProjection &&
               viewModel.ActiveProjectionContent?.IsTitleSlide == true;
    }

    private static string CollapseHorizontalWhitespace(string value)
    {
        return string.Join(' ', value.Split(
            [' ', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
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
        viewModel.PreviousProjectionPageRequested -= ShowPreviousProjectionPage;
        viewModel.NextProjectionPageRequested -= ShowNextProjectionPage;
        viewModel.ClearProjectionPageState();
        base.OnClosed(e);
    }
}
