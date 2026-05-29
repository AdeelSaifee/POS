using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the client contract for submitting local outbox batches to the central API sync ingest endpoint.
/// </summary>
public interface ISyncIngestClient
{
    /// <summary>
    /// Transmits a bulk ingest request to the central server asynchronously.
    /// </summary>
    /// <param name="request">The chunk batch request containing POS outbox events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured result containing either the acknowledged response or a typed network/protocol error.</returns>
    Task<SyncIngestClientResult> IngestAsync(
        SyncIngestRequest request,
        CancellationToken cancellationToken = default);
}
