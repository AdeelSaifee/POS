using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Shared.Contracts;
using POS.Shared.Contracts.Sync;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// EF Core implementation of the Central ingestion acknowledgment applier.
/// </summary>
public sealed class EfSyncAckApplier : ISyncAckApplier
{
    private const string StreamNameOutboxPush = "push:outbox";
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _provisioningContext;
    private readonly SyncProcessorOptions _options;
    private readonly ISyncPaymentReconciliationService _reconciliationService;
    private readonly ILogger<EfSyncAckApplier> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EfSyncAckApplier"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="logger">The logger helper.</param>
    public EfSyncAckApplier(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        ILogger<EfSyncAckApplier> logger)
        : this(db, provisioningContext, new SyncProcessorOptions(), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EfSyncAckApplier"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    /// <param name="logger">The logger helper.</param>
    public EfSyncAckApplier(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options,
        ILogger<EfSyncAckApplier> logger)
        : this(db, provisioningContext, options, new SyncPaymentReconciliationService(db, provisioningContext, Microsoft.Extensions.Logging.Abstractions.NullLogger<SyncPaymentReconciliationService>.Instance), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EfSyncAckApplier"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    /// <param name="reconciliationService">The payment reconciliation service.</param>
    /// <param name="logger">The logger helper.</param>
    public EfSyncAckApplier(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options,
        ISyncPaymentReconciliationService reconciliationService,
        ILogger<EfSyncAckApplier> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _reconciliationService = reconciliationService ?? throw new ArgumentNullException(nameof(reconciliationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SyncAckApplyResult> ApplySuccessAsync(
        SyncOutboxBatch batch,
        SyncIngestRequest request,
        SyncIngestResponse response,
        CancellationToken cancellationToken = default)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (response == null) throw new ArgumentNullException(nameof(response));

        // 1. Verify terminal provisioning context
        if (!_provisioningContext.IsProvisioned)
        {
            return SyncAckApplyResult.Failed("UNCONFIGURED_TERMINAL", "Terminal session is not configured/provisioned.");
        }

        var tenantId = _provisioningContext.CurrentTenantId;
        var locationId = _provisioningContext.CurrentLocationId;
        var terminalId = _provisioningContext.CurrentTerminalId;

        // 2. Validate request identity matches current terminal
        if (request.TenantId != tenantId ||
            request.LocationId != locationId ||
            request.TerminalId != terminalId)
        {
            return SyncAckApplyResult.Failed("IDENTITY_MISMATCH", "The request identity fields do not match the current provisioned terminal context.");
        }

        // 3. Validate response matches request
        if (response.ChunkSequence != request.ChunkSequence)
        {
            return SyncAckApplyResult.Failed("SEQUENCE_MISMATCH", $"The response ChunkSequence '{response.ChunkSequence}' does not match request ChunkSequence '{request.ChunkSequence}'.");
        }

        if (response.ChunkIdempotencyKey != request.ChunkIdempotencyKey)
        {
            return SyncAckApplyResult.Failed("IDEMPOTENCY_KEY_MISMATCH", $"The response ChunkIdempotencyKey '{response.ChunkIdempotencyKey}' does not match request '{request.ChunkIdempotencyKey}'.");
        }

        if (!string.Equals(response.Status, "Received", StringComparison.OrdinalIgnoreCase))
        {
            return SyncAckApplyResult.Failed("INVALID_RESPONSE_STATUS", $"Central API response status is '{response.Status}'. Expected 'Received'.");
        }

        if (response.EventCount != request.Events.Count || response.Events.Count != request.Events.Count)
        {
            return SyncAckApplyResult.Failed("EVENT_COUNT_MISMATCH", $"The Central response event count '{response.Events.Count}' does not match request event count '{request.Events.Count}'.");
        }

        // 4. Validate all event acknowledgments in response match request details
        var ackMap = new Dictionary<Guid, SyncIngestEventAck>();
        foreach (var ack in response.Events)
        {
            if (ack == null)
            {
                return SyncAckApplyResult.Failed("INVALID_ACK_MATERIAL", "The response contains null event acknowledgments.");
            }

            if (!string.Equals(ack.Status, "Received", StringComparison.OrdinalIgnoreCase))
            {
                return SyncAckApplyResult.Failed("EVENT_ACK_FAILED", $"EventId '{ack.EventId}' was rejected by Central API with status '{ack.Status}'.");
            }

            if (ackMap.ContainsKey(ack.EventId))
            {
                return SyncAckApplyResult.Failed("DUPLICATE_EVENT_ACK", $"Duplicate EventId '{ack.EventId}' detected inside Central response events.");
            }

            ackMap[ack.EventId] = ack;
        }

        foreach (var ev in request.Events)
        {
            if (!ackMap.TryGetValue(ev.EventId, out var ack) ||
                ack.IdempotencyKey != ev.IdempotencyKey ||
                ack.TerminalSequence != ev.TerminalSequence)
            {
                return SyncAckApplyResult.Failed("EVENT_ACK_MISMATCH", $"Central response did not return a valid corresponding acknowledgment matching request EventId '{ev.EventId}'.");
            }
        }

        // 5. Execute atomic modifications inside an explicit SQLite transaction block
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Query outbox rows from local database
            var batchIds = batch.Items.Select(x => x.Id).ToList();
            var dbRows = await _db.SyncOutbox
                .Where(x => batchIds.Contains(x.Id) &&
                            x.LocationId == locationId &&
                            x.TerminalId == terminalId &&
                            (x.Status == SyncOutboxStatus.Pending || x.Status == SyncOutboxStatus.Failed) &&
                            x.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (dbRows.Count != request.Events.Count)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return SyncAckApplyResult.Failed("OUTBOX_ROWS_MISSING", $"Failed to locate all matching pending outbox rows in local database. Expected: {request.Events.Count}, Found: {dbRows.Count}.");
            }

            var rowMap = dbRows.ToDictionary(x => x.EventId);

            foreach (var ev in request.Events)
            {
                if (!rowMap.TryGetValue(ev.EventId, out var row) ||
                    row.IdempotencyKey != ev.IdempotencyKey ||
                    row.TerminalSequence != ev.TerminalSequence)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return SyncAckApplyResult.Failed("OUTBOX_ROW_MISMATCH", $"Local outbox record details mismatch for EventId '{ev.EventId}'.");
                }
            }

            var now = DateTimeOffset.UtcNow;

            // Update outbox rows
            foreach (var row in dbRows)
            {
                row.Status = SyncOutboxStatus.Acked;
                row.AckedOn = now;
                row.ChunkSequence = response.ChunkSequence;
                row.LastErrorCode = null;
                row.LastErrorMessage = null;
                row.UpdatedBy = "sync-processor";
                row.UpdatedOn = now;
            }

            // Update cursor monotonically
            var cursor = await _db.SyncCursors
                .FirstOrDefaultAsync(x => x.TerminalId == terminalId && x.StreamName == StreamNameOutboxPush, cancellationToken)
                .ConfigureAwait(false);

            if (cursor != null)
            {
                cursor.LocationId = request.LocationId;
                cursor.LastPushedChunkSequence = Math.Max(cursor.LastPushedChunkSequence ?? 0, response.ChunkSequence);
                cursor.LastAckedChunkSequence = Math.Max(cursor.LastAckedChunkSequence ?? 0, response.ChunkSequence);
                cursor.Status = SyncCursorStatus.Active;
                cursor.LastErrorCode = null;
                cursor.LastErrorMessage = null;
                cursor.UpdatedBy = "sync-processor";
                cursor.UpdatedOn = now;
            }
            else
            {
                cursor = new SyncCursor
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    LocationId = request.LocationId,
                    TerminalId = request.TerminalId,
                    StreamName = StreamNameOutboxPush,
                    LastPushedChunkSequence = response.ChunkSequence,
                    LastAckedChunkSequence = response.ChunkSequence,
                    Status = SyncCursorStatus.Active,
                    LastErrorCode = null,
                    LastErrorMessage = null,
                    IsActive = true,
                    CreatedBy = "sync-processor",
                    CreatedOn = now
                };
                _db.SyncCursors.Add(cursor);
            }

            // Reconcile payments for completed orders in this batch
            var completedOrderIds = dbRows
                .Where(x => string.Equals(x.EventType, "OrderCompleted", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.EventId)
                .ToList();

            if (completedOrderIds.Count > 0)
            {
                await _reconciliationService.ReconcilePaymentsAsync(completedOrderIds, cancellationToken).ConfigureAwait(false);
            }

            // Save modifications
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Commit atomic boundary
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return SyncAckApplyResult.Succeeded(dbRows.Count, response.ChunkSequence);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Exception caught during SyncAckApplier transaction save. Transaction rolled back.");
            return SyncAckApplyResult.Failed("TRANSACTION_FAILED", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<SyncAckApplyResult> ApplyFailureAsync(
        SyncOutboxBatch batch,
        SyncIngestRequest request,
        SyncIngestClientError? error,
        CancellationToken cancellationToken = default)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 1. Verify terminal provisioning context
        if (!_provisioningContext.IsProvisioned)
        {
            return SyncAckApplyResult.Failed("UNCONFIGURED_TERMINAL", "Terminal session is not configured/provisioned.");
        }

        var tenantId = _provisioningContext.CurrentTenantId;
        var locationId = _provisioningContext.CurrentLocationId;
        var terminalId = _provisioningContext.CurrentTerminalId;

        // 2. Validate request identity matches current terminal
        if (request.TenantId != tenantId ||
            request.LocationId != locationId ||
            request.TerminalId != terminalId)
        {
            return SyncAckApplyResult.Failed("IDENTITY_MISMATCH", "The request identity fields do not match the current provisioned terminal context.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var batchIds = batch.Items.Select(x => x.Id).ToList();
            var dbRows = await _db.SyncOutbox
                .Where(x => batchIds.Contains(x.Id) &&
                            x.LocationId == locationId &&
                            x.TerminalId == terminalId &&
                            (x.Status == SyncOutboxStatus.Pending || x.Status == SyncOutboxStatus.Failed) &&
                            x.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (dbRows.Count != request.Events.Count)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return SyncAckApplyResult.Failed("OUTBOX_ROWS_MISSING", $"Failed to locate all matching pending or failed outbox rows in local database. Expected: {request.Events.Count}, Found: {dbRows.Count}.");
            }

            var rowMap = dbRows.ToDictionary(x => x.EventId);

            foreach (var ev in request.Events)
            {
                if (!rowMap.TryGetValue(ev.EventId, out var row) ||
                    row.IdempotencyKey != ev.IdempotencyKey ||
                    row.TerminalSequence != ev.TerminalSequence)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return SyncAckApplyResult.Failed("OUTBOX_ROW_MISMATCH", $"Local outbox record details mismatch for EventId '{ev.EventId}'.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var errorCode = error?.Code ?? "GENERIC_FAILURE";
            var errorMessage = error?.Message ?? "An unknown failure occurred.";

            foreach (var row in dbRows)
            {
                row.AttemptCount++;
                row.LastAttemptOn = now;
                row.LastErrorCode = errorCode.Length > 80 ? errorCode.Substring(0, 80) : errorCode;
                row.LastErrorMessage = errorMessage.Length > 500 ? errorMessage.Substring(0, 500) : errorMessage;
                row.UpdatedBy = "sync-processor";
                row.UpdatedOn = now;

                if (row.AttemptCount >= _options.MaxRetryAttempts)
                {
                    row.Status = SyncOutboxStatus.DeadLetter;

                    var idempotencyKey = $"quarantine:syncoutbox:{row.Id}";
                    var journalExists = await _db.LocalRecoveryJournal
                        .AnyAsync(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey, cancellationToken)
                        .ConfigureAwait(false);

                    if (!journalExists)
                    {
                        var metadata = new Dictionary<string, object?>
                        {
                            { "OutboxId", row.Id },
                            { "EventType", row.EventType },
                            { "EventId", row.EventId },
                            { "BusinessDate", row.BusinessDate.ToString("yyyy-MM-dd") },
                            { "TerminalSequence", row.TerminalSequence },
                            { "LastErrorCode", row.LastErrorCode },
                            { "LastErrorMessage", row.LastErrorMessage },
                            { "AttemptCount", row.AttemptCount },
                            { "LastAttemptOn", row.LastAttemptOn?.ToString("o") },
                            { "Status", "DeadLetter" }
                        };
                        var statePayloadJson = JsonSerializer.Serialize(metadata);

                        var journalEntry = new LocalRecoveryJournal
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            LocationId = locationId,
                            TerminalId = terminalId,
                            ShiftId = null,
                            OrderId = null,
                            PaymentId = null,
                            RecoveryType = RecoveryType.SyncInFlight,
                            Status = RecoveryJournalStatus.Open,
                            RequiredAction = RequiredRecoveryAction.RetrySync,
                            StatePayloadJson = statePayloadJson,
                            IdempotencyKey = idempotencyKey,
                            CorrelationId = row.CorrelationId,
                            IsActive = true,
                            CreatedBy = "sync-processor",
                            CreatedOn = now
                        };
                        _db.LocalRecoveryJournal.Add(journalEntry);
                    }
                }
                else
                {
                    row.Status = SyncOutboxStatus.Failed;
                }
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return SyncAckApplyResult.Succeeded(dbRows.Count, request.ChunkSequence);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Exception caught during SyncAckApplier ApplyFailureAsync transaction save. Transaction rolled back.");
            return SyncAckApplyResult.Failed("TRANSACTION_FAILED", ex.Message);
        }
    }
}
