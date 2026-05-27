using Microsoft.Extensions.Configuration;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Helper to load the provisioning record from the configuration at startup.
/// </summary>
public static class ProvisioningConfigLoader
{
    /// <summary>
    /// Loads the provisioning state from configuration.
    /// Expects values under section "Provisioning" with keys: "TenantId", "LocationId", and "TerminalId".
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>A <see cref="ProvisioningRecord"/> representing the loaded configuration.</returns>
    public static ProvisioningRecord Load(IConfiguration configuration)
    {
        if (configuration == null)
        {
            return ProvisioningRecord.Unprovisioned;
        }

        var section = configuration.GetSection("Provisioning");
        if (!section.Exists())
        {
            return ProvisioningRecord.Unprovisioned;
        }

        var tenantIdRaw = section["TenantId"];
        var locationIdRaw = section["LocationId"];
        var terminalIdRaw = section["TerminalId"];

        // If all values are null/empty, treat as completely unprovisioned
        if (string.IsNullOrWhiteSpace(tenantIdRaw) &&
            string.IsNullOrWhiteSpace(locationIdRaw) &&
            string.IsNullOrWhiteSpace(terminalIdRaw))
        {
            return ProvisioningRecord.Unprovisioned;
        }

        // Parse values safely. If missing or invalid, they will result in null,
        // which creates a half-provisioned/invalid record, causing the context to fail closed.
        int? tenantId = int.TryParse(tenantIdRaw, out var tId) ? tId : null;
        int? locationId = int.TryParse(locationIdRaw, out var lId) ? lId : null;
        int? terminalId = int.TryParse(terminalIdRaw, out var termId) ? termId : null;

        return new ProvisioningRecord(tenantId, locationId, terminalId);
    }
}
