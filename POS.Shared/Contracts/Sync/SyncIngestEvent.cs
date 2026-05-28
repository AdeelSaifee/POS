using System;

namespace POS.Shared.Contracts.Sync;

/// <summary>
/// Represents an individual outbox event generated on the POS terminal.
/// </summary>
public sealed record SyncIngestEvent(
    /// <summary>
    /// The business date of the POS shift when the event occurred.
    /// </summary>
    DateOnly BusinessDate,

    /// <summary>
    /// The sequential order of the outbox event on the terminal.
    /// </summary>
    long TerminalSequence,

    /// <summary>
    /// The type of the event (e.g. OrderCompleted, ShiftOpened). Max length: 80 characters.
    /// </summary>
    string EventType,

    /// <summary>
    /// The unique identifier of the event.
    /// </summary>
    Guid EventId,

    /// <summary>
    /// The serialized JSON payload of the event.
    /// </summary>
    string PayloadJson,

    /// <summary>
    /// The SHA-256 hash of PayloadJson to verify payload integrity. Max length: 128 characters.
    /// </summary>
    string PayloadHash,

    /// <summary>
    /// The unique idempotency key for this event. Max length: 100 characters.
    /// </summary>
    string IdempotencyKey,

    /// <summary>
    /// The correlation ID to track related events. Max length: 100 characters.
    /// </summary>
    string CorrelationId,

    /// <summary>
    /// The optional chunk sequence assigned when sync packet is prepared.
    /// </summary>
    long? ChunkSequence
);
