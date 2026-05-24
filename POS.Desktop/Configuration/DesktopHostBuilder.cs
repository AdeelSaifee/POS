using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using POS.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using POS.Shared.Contracts;
using POS.Desktop.Services.Provisioning;
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
            .ConfigureServices((hostContext, services) =>
            {
                // UI Services
                services.AddSingleton<MainWindow>();

                // Register context first as DbContext depends on it
                services.AddScoped<IProvisionedTerminalContext, NoProvisionedTerminalContext>();

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
