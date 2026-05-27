using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using POS.Shared.Contracts;
using POS.Desktop.Data.Seeding;
using POS.Desktop.Services.Catalog;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Auth;
using POS.Desktop.Shell;
using System.IO;

namespace POS.Desktop.Configuration;

/// <summary>
/// Factory for creating and configuring the .NET Generic Host for the WPF application.
/// </summary>
public static class DesktopHostBuilder
{
    /// <summary>
    /// Creates a pre-configured host builder.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A configured IHostBuilder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging((context, logging) =>
            {
                // Task 1.5.4: Configure minimal shell-level logging sinks.
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();

                // Task 1.5.6: Add minimal file logging for shell diagnostics.
                var logDir = context.Configuration["Logging:File:Directory"] ?? "IMAGYN/POS/Desktop/Logs";
                var logFile = context.Configuration["Logging:File:FileName"] ?? "pos-desktop.log";
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var fullLogDirPath = Path.Combine(localAppData, logDir);

                if (!Directory.Exists(fullLogDirPath))
                {
                    Directory.CreateDirectory(fullLogDirPath);
                }

                var fullLogPath = Path.Combine(fullLogDirPath, logFile);
                logging.AddProvider(new MinimalFileLoggerProvider(fullLogPath));
            })
            .ConfigureServices((hostContext, services) =>
            {
                // UI Services
                services.AddSingleton<MainWindow>();
                services.AddSingleton<PosWebMessageRouter>();

                // Business Services
                services.AddSingleton<ISessionService, OperatorSessionService>();
                services.AddSingleton<IAuthService, StubAuthService>();
                services.AddScoped<ILocalCatalogSeeder, LocalCatalogSeeder>();
                services.AddScoped<ICatalogService, CatalogService>();
                services.AddScoped<ITerminalProvisioningStore, EfTerminalProvisioningStore>();
                services.AddSingleton<TerminalProvisioningStartupLoader>();

                // Register context first as DbContext depends on it.
                // ProvisioningConfigLoader seeds the context with the appsettings.json value (normally
                // Unprovisioned). The durable SQLite state is loaded after migrations by
                // TerminalProvisioningStartupLoader, which updates this singleton in-place.
                var provisioningRecord = ProvisioningConfigLoader.Load(hostContext.Configuration);
                services.AddSingleton<IProvisionedTerminalContext>(new ProvisionedTerminalContext(provisioningRecord));

                services.AddDbContext<PosLocalDbContext>((serviceProvider, options) =>
                {
                    var dataFolder = hostContext.Configuration["Database:DataFolder"] ?? "IMAGYN/POS/Desktop/Data";
                    var dbName = hostContext.Configuration["Database:DatabaseName"] ?? "pos_local.db";
                    
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var fullDataDirPath = Path.Combine(localAppData, dataFolder);

                    if (!Directory.Exists(fullDataDirPath))
                    {
                        Directory.CreateDirectory(fullDataDirPath);
                    }

                    var dbPath = Path.Combine(fullDataDirPath, dbName);
                    var connectionString = $"Data Source={dbPath}";

                    options.UseSqlite(connectionString);
                });
            });
    }
}
