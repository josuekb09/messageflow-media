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
    private ServiceProvider? serviceProvider;
    private bool exceptionHandlersRegistered;

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

        // The splash is the first window, so WPF makes it MainWindow. Explicit shutdown until the
        // real main window takes over keeps closing the splash from ending the application.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        SplashWindow? splash = null;

        try
        {
            LogStartupMessage("MessageFlow startup beginning.");
            base.OnStartup(e);

            var databasePath = MessageFlowDatabase.DefaultDatabasePath;
            LogStartupMessage($"MessageFlow database path: {databasePath}");

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    MessageFlowDatabase.CreateMissingDatabaseMessage(databasePath),
                    databasePath);
            }

            // Loaded before the write check so its message appears in the operator's language.
            // It only reads a small settings file and falls back to the default on any error.
            Localizer.Instance.SetLanguage(UiLanguagePreference.Load());

            // Applied before the splash so it opens in the operator's theme. It only reads a small
            // settings file and swaps one resource dictionary.
            AppTheme.Apply(UiThemePreference.LoadIsLight());

            splash = new SplashWindow();
            splash.Show();

            // Fail now, in plain words, rather than minutes later mid-service with a SQLite error.
            // This is one tiny file write and one file open, not a database open.
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

            // The disk-bound repair and inventory touch no UI, so they run off the UI thread and the
            // splash stays responsive through a cold-disk start. Everything after resumes on the UI thread.
            await Task.Run(async () =>
            {
                await MessageFlowDatabaseRepair.RepairAsync(databasePath, LogStartupMessage);
                MessageFlowDatabase.WriteLibraryInventory(databasePath, LogStartupMessage);
            });

            serviceProvider = new ServiceCollection()
                .AddMessageFlowData(databasePath)
                .AddMessageFlowSearch()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>()
                .BuildServiceProvider();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            LogStartupMessage("MainWindow shown.");
            splash.CloseSplash();
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
        base.OnExit(e);
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
            LogStartupError("Unhandled UI exception.", args.Exception);
            ShowStartupError("MessageFlow encountered an unexpected error.", args.Exception);
            args.Handled = true;
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

    private static void ShowStartupError(string heading, Exception exception)
    {
        MessageBox.Show(
            $"{heading}{Environment.NewLine}{Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}Details were written to logs\\app-startup.log.",
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
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{Environment.NewLine}");
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
