using System.IO;
using System.Text.Json;

namespace POS.Desktop.Configuration;

public static class LocalDatabaseConfigurationGuard
{
    public static string GetRequiredConnectionString()
    {
        var configPath = ResolveAppSettingsPath();

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) ||
            !connectionStrings.TryGetProperty("LocalDatabase", out var localDatabase) ||
            string.IsNullOrWhiteSpace(localDatabase.GetString()))
        {
            throw new InvalidOperationException(
                "Missing required configuration key 'ConnectionStrings:LocalDatabase' in POS.Desktop appsettings.");
        }

        return localDatabase.GetString()!;
    }

    private static string ResolveAppSettingsPath()
    {
        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(baseDirectoryPath))
        {
            return baseDirectoryPath;
        }

        var projectDirectoryPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.json"));

        if (File.Exists(projectDirectoryPath))
        {
            return projectDirectoryPath;
        }

        throw new InvalidOperationException(
            "Missing POS.Desktop appsettings.json required for runtime local database configuration.");
    }
}
