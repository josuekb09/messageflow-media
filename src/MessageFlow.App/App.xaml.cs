using System.Windows;
using MessageFlow.App.ViewModels;
using MessageFlow.Data;
using MessageFlow.Search;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.App;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        serviceProvider = new ServiceCollection()
            .AddMessageFlowData()
            .AddMessageFlowSearch()
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainWindow>()
            .BuildServiceProvider();

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
