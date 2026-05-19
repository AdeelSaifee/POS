using Microsoft.EntityFrameworkCore;
using POS.Api.Contracts.Tenant;
using POS.Api.Data;
using POS.Shared.Contracts;

namespace POS.Api.Application.Tenant;

public sealed class TenantProfileReadService : ITenantProfileReadService
{
    private readonly PosCentralDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenantContext;

    public TenantProfileReadService(
        PosCentralDbContext dbContext,
        ICurrentTenantContext currentTenantContext)
    {
        _dbContext = dbContext;
        _currentTenantContext = currentTenantContext;
    }

    public async Task<TenantProfileDto?> GetCurrentTenantProfileAsync(CancellationToken cancellationToken)
    {
        if (!_currentTenantContext.HasTenant)
        {
            return null;
        }

        return await _dbContext.Companies
            .AsNoTracking()
            .Select(x => new TenantProfileDto(
                x.Id,
                x.Code,
                x.Name,
                x.LogoUrl,
                x.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
