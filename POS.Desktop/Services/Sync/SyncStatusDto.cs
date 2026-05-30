using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A safe, minimal, and read-only DTO representing the synchronization status of the terminal.
/// Excludes sensitive payload data, keys, or raw message secrets to prevent information leakage.
/// </summary>
public sealed record SyncStatusDto(
    bool IsProvisioned,
    bool IsOnline,
    int PendingOutboxCount,
    int FailedOutboxCount,
    int DeadLetterOutboxCount,
    int RetryableOutboxCount,
    int PendingReconciliationCount,
    int OpenRecoveryJournalCount,
    long? LastPushedChunkSequence,
    long? LastAckedChunkSequence,
    DateTimeOffset? LastAckedOn,
    string? LastErrorCode);
