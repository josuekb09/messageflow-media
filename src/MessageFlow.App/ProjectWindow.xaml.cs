using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MessageFlow.App.Services;
using MessageFlow.App.ViewModels;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MessageFlow.App;

public partial class ProjectWindow : Window
{
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
        currentPageIndex = 0;
        UpdateProjectionLayout();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateProjectionLayout();
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        UpdateProjectionLayout();
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
            if (projectionPages.Count > 1 && currentPageIndex < projectionPages.Count - 1)
            {
                ShowNextProjectionPage();
            }
            else if (viewModel.NextParagraphCommand.CanExecute(null))
            {
                viewModel.NextParagraphCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Up or Key.Back)
        {
            if (projectionPages.Count > 1 && currentPageIndex > 0)
            {
                ShowPreviousProjectionPage();
            }
            else if (viewModel.PreviousParagraphCommand.CanExecute(null))
            {
                viewModel.PreviousParagraphCommand.Execute(null);
            }

            e.Handled = true;
            return;
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
        if (e.PropertyName == nameof(MainViewModel.ActiveProjectionContent))
        {
            currentPageIndex = 0;
            QueueProjectionUpdate();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.ProjectionFontSize) ||
            e.PropertyName == nameof(MainViewModel.ProjectionLineHeight) ||
            e.PropertyName == nameof(MainViewModel.ProjectionFontSizeOffset) ||
            e.PropertyName == nameof(MainViewModel.ProjectionFontScaleMultiplier) ||
            e.PropertyName == nameof(MainViewModel.IsProjectionFontSizeUsingSafeFit))
        {
            QueueProjectionUpdate();
        }
    }

    private void QueueProjectionUpdate()
    {
        if (updateQueued)
        {
            return;
        }

        updateQueued = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            updateQueued = false;
            UpdateProjectionLayout();
        }), DispatcherPriority.Loaded);
    }

    private void UpdateProjectionLayout()
    {
        var winWidth = ActualWidth > 50 ? ActualWidth : Width;
        var winHeight = ActualHeight > 50 ? ActualHeight : Height;

        if (winWidth <= 100 || winHeight <= 100)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var dpiScaleX = Math.Max(1.0, dpi.DpiScaleX);
        var dpiScaleY = Math.Max(1.0, dpi.DpiScaleY);

        string title;
        string subtitle;
        string bodyText;
        ProjectionContentType contentType;
        var isTitleSlide = false;

        if (isTestProjection)
        {
            title = ProjectionTestTitle;
            subtitle = string.Empty;
            bodyText = ProjectionTestText;
            contentType = ProjectionContentType.Sermon;
        }
        else
        {
            var content = viewModel.ActiveProjectionContent;
            if (content is null)
            {
                projectionPages.Clear();
                ParagraphTextBlock.Text = string.Empty;
                TitleTextBlock.Text = string.Empty;
                ParagraphNumberTextBlock.Text = string.Empty;
                PageIndicatorTextBlock.Text = string.Empty;
                PageIndicatorTextBlock.Visibility = Visibility.Collapsed;
                viewModel.ClearProjectionPageState();
                return;
            }

            contentType = content.ContentType;
            title = content.Title;
            subtitle = contentType == ProjectionContentType.Sermon ? viewModel.ProjectionParagraphNumber : string.Empty;
            bodyText = content.BodyText;
            isTitleSlide = content.IsTitleSlide;
        }

        var layout = ProjectionAutoFitEngine.CalculateLayout(
            contentType,
            title,
            subtitle,
            bodyText,
            winWidth,
            winHeight,
            dpiScaleX,
            dpiScaleY,
            viewModel.ProjectionFontScaleMultiplier,
            isTitleSlide);

        // Apply margins and header layout
        ProjectionRoot.Margin = layout.RootMargin;

        TitleTextBlock.FontSize = layout.TitleFontSize;
        TitleTextBlock.LineHeight = layout.TitleLineHeight;
        TitleTextBlock.Text = (isTitleSlide || contentType != ProjectionContentType.Song) ? title : string.Empty;
        TitleTextBlock.Visibility = string.IsNullOrWhiteSpace(TitleTextBlock.Text) ? Visibility.Collapsed : Visibility.Visible;
        TitleTextBlock.FontWeight = contentType == ProjectionContentType.Sermon ? FontWeights.SemiBold : FontWeights.Bold;

        ParagraphNumberTextBlock.FontSize = layout.NumberFontSize;
        ParagraphNumberTextBlock.Text = subtitle;
        ParagraphNumberTextBlock.Visibility = string.IsNullOrWhiteSpace(subtitle) ? Visibility.Collapsed : Visibility.Visible;

        PageIndicatorTextBlock.FontSize = layout.IndicatorFontSize;

        // Apply pages
        projectionPages.Clear();
        projectionPages.AddRange(layout.Pages);
        currentPageIndex = Math.Clamp(currentPageIndex, 0, projectionPages.Count - 1);

        ParagraphTextBlock.FontSize = layout.FontSize;
        ParagraphTextBlock.LineHeight = layout.LineHeight;
        ParagraphTextBlock.Text = projectionPages[currentPageIndex];
        ParagraphTextBlock.TextAlignment = contentType == ProjectionContentType.Song ? TextAlignment.Center : TextAlignment.Left;
        ParagraphTextBlock.FontWeight = contentType == ProjectionContentType.Sermon ? FontWeights.SemiBold : FontWeights.Bold;

        if (layout.IsPaginated && projectionPages.Count > 1)
        {
            PageIndicatorTextBlock.Visibility = Visibility.Visible;
            PageIndicatorTextBlock.Text = $"{currentPageIndex + 1} of {projectionPages.Count}";
        }
        else
        {
            PageIndicatorTextBlock.Visibility = Visibility.Collapsed;
            PageIndicatorTextBlock.Text = string.Empty;
        }

        viewModel.ReportProjectionPageState(currentPageIndex, projectionPages.Count);
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
            ParagraphTextBlock.Text = projectionPages[currentPageIndex];
            PageIndicatorTextBlock.Text = $"{currentPageIndex + 1} of {projectionPages.Count}";
            viewModel.ReportProjectionPageState(currentPageIndex, projectionPages.Count);
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
            ParagraphTextBlock.Text = projectionPages[currentPageIndex];
            PageIndicatorTextBlock.Text = $"{currentPageIndex + 1} of {projectionPages.Count}";
            viewModel.ReportProjectionPageState(currentPageIndex, projectionPages.Count);
        }
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
            QueueProjectionUpdate();
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
        QueueProjectionUpdate();
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

    private void ConfigureContentLayout()
    {
        var usesSingleScreenFit = !isTestProjection &&
                                  (viewModel.IsLiveBibleProjection || viewModel.IsLiveSongProjection);
        var usesSongLayout = !isTestProjection && viewModel.IsLiveSongProjection;

        TitleTextBlock.Text = usesSongLayout
            ? string.Empty
            : isTestProjection
                ? ProjectionTestTitle
                : viewModel.ActiveProjectionContent?.Title ?? string.Empty;
        TitleTextBlock.Visibility = usesSongLayout ? Visibility.Collapsed : Visibility.Visible;
        TitleTextBlock.FontWeight = usesSingleScreenFit ? FontWeights.Bold : FontWeights.SemiBold;
        TitleTextBlock.FontSize = usesSingleScreenFit ? 48 : 38;
        TitleTextBlock.MaxHeight = usesSingleScreenFit ? double.PositiveInfinity : 96;
        TitleTextBlock.Margin = usesSongLayout
            ? new Thickness(0)
            : usesSingleScreenFit
            ? new Thickness(0, 0, 0, 12)
            : new Thickness(0);

        ParagraphNumberTextBlock.Margin = usesSingleScreenFit
            ? new Thickness(0, 0, 0, 8)
            : new Thickness(0, 8, 0, 18);
        ParagraphNumberTextBlock.FontSize = usesSingleScreenFit ? 22 : 28;
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
        ParagraphTextBlock.TextAlignment = usesSongLayout ? TextAlignment.Center : TextAlignment.Left;
        ParagraphTextBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        ParagraphTextBlock.VerticalAlignment = VerticalAlignment.Center;
    }

    private bool IsSongTitleOnlySlide(string text) => IsSongTitleSlide();

    private bool IsSongTitleSlide() =>
        !isTestProjection &&
        viewModel.IsLiveSongProjection &&
        viewModel.ActiveProjectionContent?.IsTitleSlide == true;

    private string GetProjectionTextForDisplay() =>
        isTestProjection ? ProjectionTestText : viewModel.SelectedParagraphText;

    private double CalculateLineHeight(double fontSize) =>
        fontSize * 1.14;

    private double GetSafeSingleScreenFontSize(double ceiling) =>
        ceiling * viewModel.ProjectionFontScaleMultiplier;

    private double FindMaximumFittingFontSize(string text, Size available, double minSize, double maxSize)
    {
        var contentType = viewModel.IsLiveSongProjection ? ProjectionContentType.Song :
                          viewModel.IsLiveBibleProjection ? ProjectionContentType.Bible :
                          ProjectionContentType.Sermon;
        return ProjectionAutoFitEngine.FindMaxFittingFontSize(
            text,
            available.Width,
            available.Height,
            minSize,
            maxSize,
            ParagraphTextBlock.FontWeight,
            ParagraphTextBlock.TextAlignment,
            contentType);
    }

    private bool DoesTextFit(string text, Size available, double fontSize)
    {
        var contentType = viewModel.IsLiveSongProjection ? ProjectionContentType.Song :
                          viewModel.IsLiveBibleProjection ? ProjectionContentType.Bible :
                          ProjectionContentType.Sermon;
        return ProjectionAutoFitEngine.DoesTextFit(
            text,
            available.Width,
            available.Height,
            fontSize,
            ParagraphTextBlock.FontWeight,
            ParagraphTextBlock.TextAlignment,
            contentType);
    }

    private IReadOnlyList<string> SplitIntoProjectionPages(string text, Size available, double fontSize)
    {
        var contentType = viewModel.IsLiveSongProjection ? ProjectionContentType.Song :
                          viewModel.IsLiveBibleProjection ? ProjectionContentType.Bible :
                          ProjectionContentType.Sermon;
        return ProjectionAutoFitEngine.SplitIntoCleanPages(
            text,
            available.Width,
            available.Height,
            fontSize,
            ParagraphTextBlock.FontWeight,
            ParagraphTextBlock.TextAlignment,
            contentType);
    }

    private static double GetBibleReferenceFontSize(Size availableSize) =>
        Math.Clamp(availableSize.Height * 0.085, 38, 78);
}
