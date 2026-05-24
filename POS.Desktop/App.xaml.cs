using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Desktop.Configuration;
using POS.Desktop.Data;
using POS.Desktop.Shell;

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
        try
        {
            base.OnStartup(e);

            await _host.StartAsync();

            // Task 1.5.3: Branch startup on runtime presence
            var runtimeStatus = WebView2RuntimeGuard.GetRuntimeStatus();
            if (!runtimeStatus.IsAvailable)
            {
                MessageBox.Show(
                    WebView2RuntimeGuard.FallbackMessage,
                    WebView2RuntimeGuard.FallbackTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Application.Current.Shutdown(1);
                return;
            }

            // Task 1.4.1: Startup database migration/readiness hook
            await ApplyLocalDatabaseStartupAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (System.Exception)
        {
            // Note: Exception is already logged with details in ApplyLocalDatabaseStartupAsync
            
            MessageBox.Show(
                "A technical error occurred while preparing the local database. The application must shut down.\n\n" +
                "If this problem persists, please contact technical support.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Shutdown ensures the app does not remain in a zombie/partially-started state.
            Application.Current.Shutdown(1);
        }
    }

    /// <summary>
    /// Performs local database readiness checks and migrations before the UI is shown.
    /// </summary>
    private async Task ApplyLocalDatabaseStartupAsync()
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        var config = _host.Services.GetRequiredService<IConfiguration>();
        var applyMigrations = config.GetValue<bool>("Database:ApplyMigrationsOnStartup", true);

        await Task.Run(async () =>
        {
            try
            {
                using (var scope = _host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PosLocalDbContext>();

                    if (applyMigrations)
                    {
                        logger.LogInformation("Checking/applying local database migrations...");
                        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
                    }

                    // Task 1.4.7: Explicit connectivity check
                    if (!await dbContext.Database.CanConnectAsync().ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("Could not connect to the local SQLite database.");
                    }

                    logger.LogInformation("Local database connectivity verified.");
                }
            }
            catch (System.Exception ex)
            {
                logger.LogCritical(ex, "Local database readiness check failed. The application cannot continue startup.");
                throw;
            }
        });
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
