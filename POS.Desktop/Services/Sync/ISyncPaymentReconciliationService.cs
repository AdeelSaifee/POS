using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for reconciling payment transactions from synchronized transaction batches.
/// </summary>
public interface ISyncPaymentReconciliationService
{
    /// <summary>
    /// Reconciles payments linked to the successfully synchronized orders.
    /// Updates matching LocalPayment and PaymentReconciliationQueue states inside the current DbContext transaction.
    /// </summary>
    /// <param name="orderIds">The IDs of the orders successfully acknowledged by Central.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReconcilePaymentsAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
}
