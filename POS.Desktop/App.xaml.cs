using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POS.Desktop.Configuration;

namespace POS.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public IServiceProvider Services => _host.Services;

    public App()
    {
        _host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        // Task 1.4.1: Startup database migration/readiness hook
        await ApplyLocalDatabaseStartupAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// Performs local database readiness checks and migrations before the UI is shown.
    /// </summary>
    private async Task ApplyLocalDatabaseStartupAsync()
    {
        // TODO Task 1.4.2: Resolve PosLocalDbContext in a startup scope
        // TODO Task 1.4.3: Execute Database.Migrate()
        await Task.CompletedTask;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }
}
