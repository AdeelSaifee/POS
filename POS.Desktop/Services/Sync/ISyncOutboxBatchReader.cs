using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for assembling read-only batches of pending outbox events.
/// </summary>
public interface ISyncOutboxBatchReader
{
    /// <summary>
    /// Reads a batch of pending, active outbox events for the current provisioned terminal.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An assembled read-only SyncOutboxBatch.</returns>
    Task<SyncOutboxBatch> ReadPendingBatchAsync(CancellationToken cancellationToken = default);
}
