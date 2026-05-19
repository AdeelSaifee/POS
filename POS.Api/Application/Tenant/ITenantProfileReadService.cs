using POS.Api.Contracts.Tenant;

namespace POS.Api.Application.Tenant;

public interface ITenantProfileReadService : IApplicationService
{
    Task<TenantProfileDto?> GetCurrentTenantProfileAsync(CancellationToken cancellationToken);
}
