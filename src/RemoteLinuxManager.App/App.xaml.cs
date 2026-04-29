using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RemoteLinuxManager.App.ViewModels;
using RemoteLinuxManager.Infrastructure.SshNet.DependencyInjection;

namespace RemoteLinuxManager.App;

public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddRemoteLinuxManagerInfrastructure();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
