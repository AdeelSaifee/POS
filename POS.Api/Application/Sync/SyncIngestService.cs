using System;
using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Contracts.Sync;

namespace POS.Api.Application.Sync;

/// <summary>
/// Service implementation for ingesting terminal sync batches centrally.
/// </summary>
public sealed class SyncIngestService : ISyncIngestService
{
    /// <inheritdoc />
    public Task<SyncIngestResponse> IngestAsync(
        SyncIngestIdentity identity,
        SyncIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Sync ingest persistence, validation, and idempotency handling are deferred to Tasks 6.1.5, 6.1.6, and 6.1.8 in subsequent implementation groups.");
    }
}
