using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Desktop.Configuration;
using POS.Desktop.Data;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;

namespace POS.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IProvisionedTerminalContext, NoProvisionedTerminalContext>();

        services.AddDbContext<PosLocalDbContext>((serviceProvider, options) =>
        {
            var connectionString = LocalDatabaseConfigurationGuard.GetRequiredConnectionString();
            options.UseSqlite(connectionString);
        });
    }
}
