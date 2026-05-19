using POS.Shared.Contracts;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Temporary fail-closed runtime provisioning context. This must be replaced
/// with secure provisioning-state resolution before local tenant-scoped data
/// access is enabled for real terminal use.
/// </summary>
public sealed class NoProvisionedTerminalContext : IProvisionedTerminalContext
{
    public int CurrentTenantId => 0;

    public int CurrentLocationId => 0;

    public int CurrentTerminalId => 0;

    public bool IsProvisioned => false;
}
