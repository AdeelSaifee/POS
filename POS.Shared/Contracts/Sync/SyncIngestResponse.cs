using System;
using System.Collections.Generic;

namespace POS.Shared.Contracts.Sync;

/// <summary>
/// Represents the central API response for an ingested chunk request.
/// </summary>
public sealed record SyncIngestResponse(
    /// <summary>
    /// The unique identifier of the central acknowledgment record.
    /// </summary>
    Guid AckId,

    /// <summary>
    /// The sequential order of the chunk from this terminal.
    /// </summary>
    long ChunkSequence,

    /// <summary>
    /// Unique idempotency key for this chunk batch. Max length: 120 characters.
    /// </summary>
    string ChunkIdempotencyKey,

    /// <summary>
    /// Ingestion status of the chunk (e.g. Success, Failed, PartiallyProcessed). Max length: 40 characters.
    /// </summary>
    string Status,

    /// <summary>
    /// The count of events processed in this chunk.
    /// </summary>
    int EventCount,

    /// <summary>
    /// List of individual event acknowledgements.
    /// </summary>
    IReadOnlyList<SyncIngestEventAck> Events,

    /// <summary>
    /// Optional error code if ingestion of the chunk failed. Max length: 80 characters.
    /// </summary>
    string? ErrorCode,

    /// <summary>
    /// Optional error message if ingestion of the chunk failed. Max length: 500 characters.
    /// </summary>
    string? ErrorMessage
);
