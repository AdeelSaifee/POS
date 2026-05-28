using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Contracts.Sync;

namespace POS.Api.Application.Sync;

/// <summary>
/// Defines the service interface for ingesting terminal sync batches centrally.
/// </summary>
public interface ISyncIngestService
{
    /// <summary>
    /// Processes and persists the batch of events ingested from a POS terminal.
    /// </summary>
    /// <param name="identity">The claims-derived device identity.</param>
    /// <param name="request">The ingest payload request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured ingestion response acknowledgment.</returns>
    Task<SyncIngestResponse> IngestAsync(
        SyncIngestIdentity identity,
        SyncIngestRequest request,
        CancellationToken cancellationToken = default);
}
