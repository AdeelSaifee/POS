    using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for applying central sync ingest acknowledgments to the local SQLite database.
/// </summary>
public interface ISyncAckApplier
{
    /// <summary>
    /// Applies a successful Central API chunk acknowledgment, marking outbox rows as Acked and advancing the sync cursor monotonically.
    /// Executes both operations atomically within a single SQLite transaction.
    /// </summary>
    /// <param name="batch">The original pending outbox batch.</param>
    /// <param name="request">The deterministic chunk request.</param>
    /// <param name="response">The central ingest response acknowledgment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A validated SyncAckApplyResult outcome.</returns>
    Task<SyncAckApplyResult> ApplySuccessAsync(
        SyncOutboxBatch batch,
        SyncIngestRequest request,
        SyncIngestResponse response,
        CancellationToken cancellationToken = default);
}
