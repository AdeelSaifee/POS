using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Safe read model representing a quarantined sync outbox event.
/// Excludes business transaction payload to avoid leaking sensitive information.
/// </summary>
public record QuarantinedItemDto(
    Guid OutboxId,
    int TenantId,
    int LocationId,
    int TerminalId,
    DateOnly BusinessDate,
    long TerminalSequence,
    string EventType,
    Guid EventId,
    string? LastErrorCode,
    string? LastErrorMessage,
    int AttemptCount,
    DateTimeOffset? LastAttemptOn);

/// <summary>
/// Scoped service implementation that queries DeadLetter outbox rows.
/// </summary>
public sealed class SyncQuarantineService : ISyncQuarantineService
{
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncQuarantineService"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="context">The terminal provisioning context.</param>
    public SyncQuarantineService(PosLocalDbContext db, IProvisionedTerminalContext context)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuarantinedItemDto>> GetQuarantinedItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.IsProvisioned)
        {
            return Array.Empty<QuarantinedItemDto>();
        }

        var locationId = _context.CurrentLocationId;
        var terminalId = _context.CurrentTerminalId;

        return await _db.SyncOutbox
            .AsNoTracking()
            .Where(x => x.LocationId == locationId &&
                        x.TerminalId == terminalId &&
                        x.Status == SyncOutboxStatus.DeadLetter &&
                        x.IsActive)
            .OrderBy(x => x.BusinessDate)
            .ThenBy(x => x.TerminalSequence)
            .ThenBy(x => x.Id)
            .Select(x => new QuarantinedItemDto(
                x.Id,
                x.TenantId,
                x.LocationId,
                x.TerminalId,
                x.BusinessDate,
                x.TerminalSequence,
                x.EventType,
                x.EventId,
                x.LastErrorCode,
                x.LastErrorMessage,
                x.AttemptCount,
                x.LastAttemptOn))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
