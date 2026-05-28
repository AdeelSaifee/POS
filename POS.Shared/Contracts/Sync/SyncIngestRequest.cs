using System.Collections.Generic;

namespace POS.Shared.Contracts.Sync;

/// <summary>
/// Represents a batch request containing terminal events to be synced to the central server.
/// </summary>
public sealed record SyncIngestRequest(
    /// <summary>
    /// The ID of the Tenant that generated the batch.
    /// </summary>
    int TenantId,

    /// <summary>
    /// The ID of the Location that generated the batch.
    /// </summary>
    int LocationId,

    /// <summary>
    /// The ID of the Terminal that generated the batch.
    /// </summary>
    int TerminalId,

    /// <summary>
    /// The sequential order of the chunk from this terminal.
    /// </summary>
    long ChunkSequence,

    /// <summary>
    /// Unique idempotency key for this chunk batch. Max length: 120 characters.
    /// </summary>
    string ChunkIdempotencyKey,

    /// <summary>
    /// The SHA-256 hash of the entire request payload to verify packet integrity. Max length: 128 characters.
    /// </summary>
    string RequestHash,

    /// <summary>
    /// Correlation ID of the chunk to trace it centrally. Max length: 100 characters.
    /// </summary>
    string CorrelationId,

    /// <summary>
    /// List of individual outbox events contained in this chunk.
    /// </summary>
    IReadOnlyList<SyncIngestEvent> Events
);
