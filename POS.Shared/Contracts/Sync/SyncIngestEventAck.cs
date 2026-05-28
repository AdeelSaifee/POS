using System;

namespace POS.Shared.Contracts.Sync;

/// <summary>
/// Represents the acknowledgement status of an individual event within an ingested chunk.
/// </summary>
public sealed record SyncIngestEventAck(
    /// <summary>
    /// The unique identifier of the event.
    /// </summary>
    Guid EventId,

    /// <summary>
    /// The idempotency key of the event. Max length: 100 characters.
    /// </summary>
    string IdempotencyKey,

    /// <summary>
    /// The sequential order of the outbox event on the terminal.
    /// </summary>
    long TerminalSequence,

    /// <summary>
    /// The ingestion status of the event (e.g. Success, Failed, Ignored). Max length: 40 characters.
    /// </summary>
    string Status,

    /// <summary>
    /// Optional error code if ingestion of the event failed. Max length: 80 characters.
    /// </summary>
    string? ErrorCode,

    /// <summary>
    /// Optional error message if ingestion of the event failed. Max length: 500 characters.
    /// </summary>
    string? ErrorMessage
);
