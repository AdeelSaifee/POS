using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for building deterministic sync ingest requests from outbox batches.
/// </summary>
public interface ISyncIngestRequestBuilder
{
    /// <summary>
    /// Builds a deterministic, API-ready SyncIngestRequest from the provided SyncOutboxBatch.
    /// </summary>
    /// <param name="batch">The outbox batch containing pending events.</param>
    /// <returns>A deterministic, validated SyncIngestRequest.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when batch is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when batch is structurally invalid or empty.</exception>
    SyncIngestRequest Build(SyncOutboxBatch batch);
}
