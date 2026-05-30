using System.Collections.Generic;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A read-only representation of an assembled batch of sync outbox items.
/// </summary>
public sealed record SyncOutboxBatch(
    IReadOnlyList<SyncOutboxBatchItem> Items)
{
    /// <summary>
    /// Gets a value indicating whether the batch contains any items.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Gets the number of items in the batch.
    /// </summary>
    public int Count => Items.Count;
}
