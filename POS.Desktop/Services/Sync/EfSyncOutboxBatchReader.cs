using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// EF Core implementation of the sync outbox batch reader.
/// </summary>
public sealed class EfSyncOutboxBatchReader : ISyncOutboxBatchReader
{
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _provisioningContext;
    private readonly SyncProcessorOptions _options;
    private readonly ILogger<EfSyncOutboxBatchReader> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EfSyncOutboxBatchReader"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    /// <param name="logger">The logger helper.</param>
    public EfSyncOutboxBatchReader(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options,
        ILogger<EfSyncOutboxBatchReader> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SyncOutboxBatch> ReadPendingBatchAsync(CancellationToken cancellationToken = default)
    {
        // 1. If terminal is not provisioned, return empty batch
        if (!_provisioningContext.IsProvisioned)
        {
            _logger.LogDebug("Batch read skipped: Terminal is not provisioned.");
            return new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        }

        // 2. Validate options before querying
        if (!_options.Validate(out var validationError))
        {
            _logger.LogError("Batch read failed: SyncProcessorOptions are invalid: {Error}.", validationError);
            return new SyncOutboxBatch(Array.Empty<SyncOutboxBatchItem>());
        }

        var currentLocationId = _provisioningContext.CurrentLocationId;
        var currentTerminalId = _provisioningContext.CurrentTerminalId;

        // 3. Query pending outbox rows with deterministic sequence order
        // Tenant filter is globally applied via PosLocalDbContext HasQueryFilter.
        var items = await _db.SyncOutbox
            .AsNoTracking()
            .Where(x => x.LocationId == currentLocationId &&
                        x.TerminalId == currentTerminalId &&
                        (x.Status == SyncOutboxStatus.Pending || x.Status == SyncOutboxStatus.Failed) &&
                        x.IsActive)
            .OrderBy(x => x.BusinessDate)
            .ThenBy(x => x.TerminalSequence)
            .ThenBy(x => x.Id)
            .Take(_options.BatchSize)
            .Select(x => new SyncOutboxBatchItem(
                x.Id,
                x.TenantId,
                x.LocationId,
                x.TerminalId,
                x.BusinessDate,
                x.TerminalSequence,
                x.EventType,
                x.EventId,
                x.PayloadJson,
                x.PayloadHash,
                x.IdempotencyKey,
                x.CorrelationId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SyncOutboxBatch(items);
    }
}
