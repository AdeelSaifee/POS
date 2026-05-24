using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Desktop.Configuration;
using POS.Desktop.Data;

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
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        var config = _host.Services.GetRequiredService<IConfiguration>();
        var applyMigrations = config.GetValue<bool>("Database:ApplyMigrationsOnStartup", true);

        try
        {
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PosLocalDbContext>();

                if (applyMigrations)
                {
                    logger.LogInformation("Checking/applying local database migrations...");
                    await dbContext.Database.MigrateAsync();
                }

                // Task 1.4.7: Explicit connectivity check
                if (!await dbContext.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException("Could not connect to the local SQLite database.");
                }

                logger.LogInformation("Local database connectivity verified.");
            }
        }
        catch (System.Exception ex)
        {
            logger.LogCritical(ex, "Local database readiness check failed.");
            throw; // Propagate to let existing top-level handler show MessageBox
        }
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
