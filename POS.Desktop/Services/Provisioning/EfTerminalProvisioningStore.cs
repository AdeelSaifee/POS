using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Shared.Contracts;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Entity Framework Core implementation of <see cref="ITerminalProvisioningStore"/> using SQLite.
/// </summary>
public sealed class EfTerminalProvisioningStore : ITerminalProvisioningStore
{
    private readonly PosLocalDbContext _dbContext;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;

    public EfTerminalProvisioningStore(
        PosLocalDbContext dbContext,
        IProvisionedTerminalContext provisionedTerminalContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
    }

    public async Task<ProvisioningRecord> GetProvisioningRecordAsync(CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TerminalProvisioning
            .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (entity == null)
        {
            return ProvisioningRecord.Unprovisioned;
        }

        return new ProvisioningRecord(entity.TenantId, entity.LocationId, entity.TerminalId, entity.UpdatedAt);
    }

    public async Task<ProvisioningResult> ProvisionTerminalAsync(int tenantId, int locationId, int terminalId, CancellationToken cancellationToken)
    {
        // 1. Validation
        if (tenantId <= 0 || locationId <= 0 || terminalId <= 0)
        {
            return new ProvisioningResult(false, "INVALID_PAYLOAD", "IDs must be positive integers.");
        }

        // 2. Fetch existing row (Id = 1 invariant)
        var entity = await _dbContext.TerminalProvisioning
            .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

        var updatedAt = DateTimeOffset.UtcNow;

        if (entity != null)
        {
            // Check if already provisioned with different details
            var isFullyProvisioned = entity.TenantId.HasValue && entity.TenantId.Value > 0 &&
                                     entity.LocationId.HasValue && entity.LocationId.Value > 0 &&
                                     entity.TerminalId.HasValue && entity.TerminalId.Value > 0;

            if (isFullyProvisioned &&
                (entity.TenantId != tenantId ||
                 entity.LocationId != locationId ||
                 entity.TerminalId != terminalId))
            {
                return new ProvisioningResult(false, "REPROVISION_BLOCKED", "Terminal is already provisioned with different details.");
            }

            // Update existing record safely
            entity.TenantId = tenantId;
            entity.LocationId = locationId;
            entity.TerminalId = terminalId;
            entity.UpdatedAt = updatedAt;
        }
        else
        {
            // Create a new provisioning record
            entity = new TerminalProvisioning
            {
                Id = 1,
                TenantId = tenantId,
                LocationId = locationId,
                TerminalId = terminalId,
                UpdatedAt = updatedAt
            };
            _dbContext.TerminalProvisioning.Add(entity);
        }

        // 3. Save to database
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Update the in-memory context so that subsequent scoped database contexts immediately resolve the correct tenant ID
        var newRecord = new ProvisioningRecord(tenantId, locationId, terminalId, updatedAt);
        if (_provisionedTerminalContext is ProvisionedTerminalContext concreteContext)
        {
            concreteContext.UpdateState(newRecord);
        }

        return new ProvisioningResult(true);
    }
}
