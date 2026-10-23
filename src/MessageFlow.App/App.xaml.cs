using System.IO;
using System.Windows;
using System.Windows.Threading;
using MessageFlow.App.Localization;
using MessageFlow.App.Themes;
using MessageFlow.App.ViewModels;
using MessageFlow.Core.Localization;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Global\MessageFlowMedia.SingleInstance";

    private ServiceProvider? serviceProvider;
    private bool exceptionHandlersRegistered;
    private Mutex? singleInstanceMutex;
    private bool ownsSingleInstanceMutex;

    public static readonly System.Diagnostics.Stopwatch StartupTimer = System.Diagnostics.Stopwatch.StartNew();

    public static void LogStartupError(string message, Exception exception)
    {
        WriteStartupLog($"{message}{Environment.NewLine}{exception}");
    }

    public static void LogStartupMessage(string message)
    {
        WriteStartupLog(message);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        RegisterExceptionHandlers();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        SplashWindow? splash = null;

        try
        {
            LogStartupMessage("MessageFlow startup beginning.");
            base.OnStartup(e);

            if (!TryAcquireSingleInstance())
            {
                LogStartupMessage("Another MessageFlow instance is already running; this one is exiting.");
                Localizer.Instance.SetLanguage(UiLanguagePreference.Load());
                MessageBox.Show(
                    Loc.T("Msg_AlreadyRunning"),
                    "MessageFlow",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            var databasePath = MessageFlowDatabase.DefaultDatabasePath;
            LogStartupMessage($"MessageFlow database path: {databasePath}");
            if (MessageFlowDatabase.IsInsideOneDrive(databasePath))
            {
                LogStartupMessage(
                    "WARNING: the database is inside a OneDrive folder. OneDrive can keep it cloud-only or lock it " +
                    "while syncing, which makes launch and search slow. Reinstall MessageFlow into the default local folder.");
            }

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    MessageFlowDatabase.CreateMissingDatabaseMessage(databasePath),
                    databasePath);
            }

            Localizer.Instance.SetLanguage(UiLanguagePreference.Load());
            AppTheme.Apply(UiThemePreference.LoadIsLight());

            splash = new SplashWindow();
            splash.Show();

            var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? string.Empty;
            if (!MessageFlowDatabase.DirectoryIsWritable(databaseDirectory) ||
                !MessageFlowDatabase.DatabaseFileIsWritable(databasePath))
            {
                LogStartupMessage($"Database location is not writable: {databasePath}");
                splash.CloseSplash();
                MessageBox.Show(
                    Loc.F("Msg_DataFolderNotWritable", Environment.NewLine, databasePath),
                    Loc.T("Msg_DataFolderNotWritableTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown(1);
                return;
            }

            serviceProvider = new ServiceCollection()
                .AddMessageFlowData(databasePath)
                .AddMessageFlowSearch()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>()
                .BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // The database repair and publication metadata checks run asynchronously in the background
            // so the main window is shown immediately (< 3 seconds) and the UI thread never blocks.
            var backgroundDbTask = Task.Run(async () =>
            {
                try
                {
                    await MessageFlowDatabaseRepair.RepairAsync(databasePath, LogStartupMessage);
                    await MainViewModel.RepairBrotherFrankPublicationMetadataAsync(scopeFactory);
                }
                catch (Exception ex)
                {
                    LogStartupError("Background database validation failed.", ex);
                }
            });

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.AttachBackgroundDatabaseTask(backgroundDbTask);
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Show the main window immediately with a responsive loading state
            mainWindow.Show();
            LogStartupMessage("MainWindow shown.");
            splash.CloseSplash();
            mainWindow.Activate();

            var initializationFailure = await mainWindow.Initialization;
            LogStartupMessage("MainWindow initialized and accepting input.");
            splash.CloseSplash();
            mainWindow.Activate();

            if (initializationFailure is not null)
            {
                MessageBox.Show(
                    mainWindow,
                    Loc.F("Msg_StartupFailed", Environment.NewLine, initializationFailure.Message),
                    Loc.T("Msg_StartupFailedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            splash?.CloseSplash();
            LogStartupError("MessageFlow failed during application startup.", ex);
            ShowStartupError("MessageFlow could not start.", ex);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        ReleaseSingleInstance();
        base.OnExit(e);
    }

    private bool TryAcquireSingleInstance()
    {
        // Global, so an installed copy and a dev build, or two Windows sessions, also exclude each other.
        singleInstanceMutex = new Mutex(initiallyOwned: false, SingleInstanceMutexName);
        try
        {
            ownsSingleInstanceMutex = singleInstanceMutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // The previous owner crashed without releasing it. Ownership passes to this instance.
            ownsSingleInstanceMutex = true;
        }

        return ownsSingleInstanceMutex;
    }

    private void ReleaseSingleInstance()
    {
        if (singleInstanceMutex is null)
        {
            return;
        }

        if (ownsSingleInstanceMutex)
        {
            singleInstanceMutex.ReleaseMutex();
            ownsSingleInstanceMutex = false;
        }

        singleInstanceMutex.Dispose();
        singleInstanceMutex = null;
    }

    private void RegisterExceptionHandlers()
    {
        if (exceptionHandlersRegistered)
        {
            return;
        }

        exceptionHandlersRegistered = true;

        DispatcherUnhandledException += (_, args) =>
        {
            // Handled, so a bug in one action never closes the app in front of the congregation.
            LogStartupError("Unhandled UI exception.", args.Exception);
            args.Handled = true;
            ShowRecoverableError(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                LogStartupError("Unhandled application exception.", exception);
                return;
            }

            WriteStartupLog($"Unhandled application exception: {args.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogStartupError("Unobserved background task exception.", args.Exception);
            args.SetObserved();
        };
    }

    private bool recoverableErrorShowing;

    /// <summary>
    /// Tells the operator, on the operator screen only, that an action failed while the app and
    /// the projection keep running. A repeating error shows one dialog at a time; the rest are
    /// only logged.
    /// </summary>
    private void ShowRecoverableError(Exception exception)
    {
        if (recoverableErrorShowing)
        {
            return;
        }

        recoverableErrorShowing = true;
        try
        {
            var owner = MainWindow is { IsLoaded: true, IsVisible: true } ? MainWindow : null;
            var message =
                $"Something went wrong, but MessageFlow is still running and the projection screen was not changed.{Environment.NewLine}{Environment.NewLine}" +
                $"{exception.Message}{Environment.NewLine}{Environment.NewLine}" +
                $"You can keep working. Details were saved to:{Environment.NewLine}{GetStartupLogPath()}";
            if (owner is null)
            {
                MessageBox.Show(message, "MessageFlow", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(owner, message, "MessageFlow", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch
        {
            // The error is already logged; a failing dialog must not raise another error.
        }
        finally
        {
            recoverableErrorShowing = false;
        }
    }

    private static void ShowStartupError(string heading, Exception exception)
    {
        MessageBox.Show(
            $"{heading}{Environment.NewLine}{Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}Details were written to:{Environment.NewLine}{GetStartupLogPath()}",
            "MessageFlow Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var logPath = GetStartupLogPath();
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [+{StartupTimer.ElapsedMilliseconds} ms] {message}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Last-resort startup logging must never take the app down.
        }
    }

    private static string GetStartupLogPath()
    {
        var solutionRoot = FindSolutionRoot();
        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            var solutionLogPath = Path.Combine(solutionRoot, "logs", "app-startup.log");
            if (MessageFlowDatabase.IsAllowedDataPath(solutionLogPath))
            {
                return solutionLogPath;
            }
        }

        var executableLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "app-startup.log");
        if (MessageFlowDatabase.IsAllowedDataPath(executableLogPath))
        {
            return executableLogPath;
        }

        return Path.Combine(MessageFlowDatabase.UserDataRoot, "logs", "app-startup.log");
    }

    private static string? FindSolutionRoot()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates)
        {
            var directory = new DirectoryInfo(candidate);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "MessageFlow.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
