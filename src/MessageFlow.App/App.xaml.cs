using System.IO;
using System.Windows;
using System.Windows.Threading;
using MessageFlow.App.Localization;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterExceptionHandlers();
        ShutdownMode = ShutdownMode.OnMainWindowClose;

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

            MessageFlowDatabaseRepair
                .RepairAsync(databasePath, LogStartupMessage)
                .GetAwaiter()
                .GetResult();

            Localizer.Instance.SetLanguage(UiLanguagePreference.Load());

            serviceProvider = new ServiceCollection()
                .AddMessageFlowData(databasePath)
                .AddMessageFlowSearch()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>()
                .BuildServiceProvider();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
            LogStartupMessage("MainWindow shown.");
        }
        catch (Exception ex)
        {
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
        return Path.Combine(FindSolutionRoot(), "logs", "app-startup.log");
    }

    private static string FindSolutionRoot()
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

        return Directory.GetCurrentDirectory();
    }
}
