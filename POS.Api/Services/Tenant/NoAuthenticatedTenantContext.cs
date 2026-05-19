using POS.Shared.Contracts;

namespace POS.Api.Services.Tenant;

/// <summary>
/// Temporary fail-closed runtime tenant context. This must be replaced with
/// real auth-derived tenant resolution before tenant-scoped endpoints use
/// the central DbContext for live data access.
/// </summary>
public sealed class NoAuthenticatedTenantContext : ICurrentTenantContext
{
    public int CurrentTenantId => 0;

    public bool HasTenant => false;

    public bool IsSystemScope => false;
}
