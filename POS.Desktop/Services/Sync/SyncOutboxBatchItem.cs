using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A read-only projection of a single SyncOutbox record.
/// </summary>
public sealed record SyncOutboxBatchItem(
    Guid Id,
    int TenantId,
    int LocationId,
    int TerminalId,
    DateOnly BusinessDate,
    long TerminalSequence,
    string EventType,
    Guid EventId,
    string PayloadJson,
    string PayloadHash,
    string IdempotencyKey,
    string CorrelationId);
