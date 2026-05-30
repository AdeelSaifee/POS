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
/// Scoped EF Core implementation of <see cref="ISyncStatusService"/> that queries local SQLite tables.
/// </summary>
public sealed class EfSyncStatusService : ISyncStatusService
{
    private const string StreamNameOutboxPush = "push:outbox";
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _provisioningContext;
    private readonly ISyncConnectivityService _connectivityService;
    private readonly ILogger<EfSyncStatusService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EfSyncStatusService"/>.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="connectivityService">The connectivity detection service.</param>
    /// <param name="logger">The logger helper.</param>
    public EfSyncStatusService(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        ISyncConnectivityService connectivityService,
        ILogger<EfSyncStatusService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying sync status metrics.");

        // 1. Check network connectivity cheaply (works even if unprovisioned)
        bool isOnline = await _connectivityService.IsConnectedAsync(cancellationToken).ConfigureAwait(false);

        // 2. If terminal is not provisioned, return safe unprovisioned state
        if (!_provisioningContext.IsProvisioned)
        {
            _logger.LogDebug("Sync status queried on an unprovisioned terminal. Returning safe default status.");
            return new SyncStatusDto(
                IsProvisioned: false,
                IsOnline: isOnline,
                PendingOutboxCount: 0,
                FailedOutboxCount: 0,
                DeadLetterOutboxCount: 0,
                RetryableOutboxCount: 0,
                PendingReconciliationCount: 0,
                OpenRecoveryJournalCount: 0,
                LastPushedChunkSequence: null,
                LastAckedChunkSequence: null,
                LastAckedOn: null,
                LastErrorCode: null);
        }

        var locationId = _provisioningContext.CurrentLocationId;
        var terminalId = _provisioningContext.CurrentTerminalId;

        // 3. Compute cheap metrics using AsNoTracking to avoid tracking overhead
        var pendingOutboxCount = await _db.SyncOutbox
            .AsNoTracking()
            .CountAsync(x => x.LocationId == locationId &&
                             x.TerminalId == terminalId &&
                             x.Status == SyncOutboxStatus.Pending &&
                             x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var failedOutboxCount = await _db.SyncOutbox
            .AsNoTracking()
            .CountAsync(x => x.LocationId == locationId &&
                             x.TerminalId == terminalId &&
                             x.Status == SyncOutboxStatus.Failed &&
                             x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var deadLetterOutboxCount = await _db.SyncOutbox
            .AsNoTracking()
            .CountAsync(x => x.LocationId == locationId &&
                             x.TerminalId == terminalId &&
                             x.Status == SyncOutboxStatus.DeadLetter &&
                             x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var pendingReconciliationCount = await _db.PaymentReconciliationQueue
            .AsNoTracking()
            .CountAsync(x => x.LocationId == locationId &&
                             x.TerminalId == terminalId &&
                             x.Status == PaymentReconciliationStatus.Pending &&
                             x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var openRecoveryJournalCount = await _db.LocalRecoveryJournal
            .AsNoTracking()
            .CountAsync(x => x.LocationId == locationId &&
                             x.TerminalId == terminalId &&
                             x.Status == RecoveryJournalStatus.Open &&
                             x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var retryableOutboxCount = pendingOutboxCount + failedOutboxCount;

        // 4. Retrieve push cursor sequences and status
        var cursor = await _db.SyncCursors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LocationId == locationId &&
                                      x.TerminalId == terminalId &&
                                      x.StreamName == StreamNameOutboxPush &&
                                      x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        long? lastPushedSequence = cursor?.LastPushedChunkSequence;
        long? lastAckedSequence = cursor?.LastAckedChunkSequence;
        string? lastErrorCode = cursor?.LastErrorCode;

        // Compute LastAckedOn from the latest active Acked SyncOutbox row
        DateTimeOffset? lastAckedOn = await _db.SyncOutbox
            .AsNoTracking()
            .Where(x => x.LocationId == locationId &&
                        x.TerminalId == terminalId &&
                        x.Status == SyncOutboxStatus.Acked &&
                        x.AckedOn != null &&
                        x.IsActive)
            .OrderByDescending(x => x.TerminalSequence)
            .Select(x => x.AckedOn)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // 5. If cursor has no active error code, fall back to the oldest active failed/dead-letter outbox error
        if (string.IsNullOrWhiteSpace(lastErrorCode))
        {
            lastErrorCode = await _db.SyncOutbox
                .AsNoTracking()
                .Where(x => x.LocationId == locationId &&
                            x.TerminalId == terminalId &&
                            (x.Status == SyncOutboxStatus.Failed || x.Status == SyncOutboxStatus.DeadLetter) &&
                            x.LastErrorCode != null &&
                            x.IsActive)
                .OrderBy(x => x.BusinessDate)
                .ThenBy(x => x.TerminalSequence)
                .Select(x => x.LastErrorCode)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogDebug(
            "Sync status calculated. Pending: {Pending}, Failed: {Failed}, DeadLetter: {DeadLetter}, Online: {Online}",
            pendingOutboxCount, failedOutboxCount, deadLetterOutboxCount, isOnline);

        return new SyncStatusDto(
            IsProvisioned: true,
            IsOnline: isOnline,
            PendingOutboxCount: pendingOutboxCount,
            FailedOutboxCount: failedOutboxCount,
            DeadLetterOutboxCount: deadLetterOutboxCount,
            RetryableOutboxCount: retryableOutboxCount,
            PendingReconciliationCount: pendingReconciliationCount,
            OpenRecoveryJournalCount: openRecoveryJournalCount,
            LastPushedChunkSequence: lastPushedSequence,
            LastAckedChunkSequence: lastAckedSequence,
            LastAckedOn: lastAckedOn,
            LastErrorCode: lastErrorCode);
    }
}
