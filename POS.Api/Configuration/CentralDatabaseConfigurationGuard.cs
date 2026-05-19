namespace POS.Api.Configuration;

public static class CentralDatabaseConfigurationGuard
{
    public static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CentralDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing required connection string 'ConnectionStrings:CentralDatabase'.");
        }

        return connectionString;
    }
}
