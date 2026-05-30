using System;
using System.Collections.Generic;
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
/// Service that handles reconciliation updates for synchronized payments inside SQLite transactions.
/// </summary>
public sealed class SyncPaymentReconciliationService : ISyncPaymentReconciliationService
{
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _provisioningContext;
    private readonly ILogger<SyncPaymentReconciliationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncPaymentReconciliationService"/>.
    /// </summary>
    /// <param name="db">The local database context.</param>
    /// <param name="provisioningContext">The terminal provisioning context.</param>
    /// <param name="logger">The logger helper.</param>
    public SyncPaymentReconciliationService(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        ILogger<SyncPaymentReconciliationService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ReconcilePaymentsAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Count == 0)
        {
            return;
        }

        if (!_provisioningContext.IsProvisioned)
        {
            _logger.LogWarning("SyncPaymentReconciliationService: Cannot reconcile payments. Terminal is not provisioned.");
            return;
        }

        var locationId = _provisioningContext.CurrentLocationId;
        var terminalId = _provisioningContext.CurrentTerminalId;

        // Fetch LocalPayments requiring reconciliation for the given orders
        var dbPayments = await _db.LocalPayments
            .Where(p => orderIds.Contains(p.OrderId) &&
                        p.LocationId == locationId &&
                        p.TerminalId == terminalId &&
                        p.RequiresReconciliation &&
                        p.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (dbPayments.Count == 0)
        {
            return;
        }

        var paymentIds = dbPayments.Select(p => p.Id).ToList();

        // Fetch corresponding PaymentReconciliationQueue rows in Pending or Checking status
        var dbQueueRows = await _db.PaymentReconciliationQueue
            .Where(q => paymentIds.Contains(q.PaymentId) &&
                        q.LocationId == locationId &&
                        q.TerminalId == terminalId &&
                        q.IsActive &&
                        (q.Status == PaymentReconciliationStatus.Pending || q.Status == PaymentReconciliationStatus.Checking))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var queueMap = dbQueueRows.ToDictionary(q => q.PaymentId);
        var now = DateTimeOffset.UtcNow;

        foreach (var payment in dbPayments)
        {
            if (queueMap.TryGetValue(payment.Id, out var queueRow))
            {
                // Verify payment/order/queue context matches
                if (queueRow.OrderId != payment.OrderId || queueRow.TenderMethodId != payment.TenderMethodId)
                {
                    _logger.LogWarning(
                        "SyncPaymentReconciliationService: Context mismatch for payment {PaymentId}. Queue OrderId: {QueueOrderId}, Payment OrderId: {PaymentOrderId}",
                        payment.Id, queueRow.OrderId, payment.OrderId);
                    continue;
                }

                // Update LocalPayment
                payment.RequiresReconciliation = false;
                payment.ReconciledOn = now;
                payment.UpdatedBy = "sync-processor";
                payment.UpdatedOn = now;

                // Update Queue Row
                queueRow.Status = PaymentReconciliationStatus.ResolvedCaptured;
                queueRow.LastAttemptOn = now;
                queueRow.LastResultCode = "SYNC_ACK";
                queueRow.LastResultMessage = "Payment successfully uploaded and acknowledged by Central.";
                queueRow.UpdatedBy = "sync-processor";
                queueRow.UpdatedOn = now;
            }
            else
            {
                _logger.LogInformation(
                    "SyncPaymentReconciliationService: No matching pending/checking reconciliation queue entry found for payment {PaymentId}. Skipping reconciliation update for this payment.",
                    payment.Id);
            }
        }
    }
}
