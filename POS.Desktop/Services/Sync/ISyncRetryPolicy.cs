using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for a synchronization retry policy, determining transient failures
/// and calculating backoff delay values.
/// </summary>
public interface ISyncRetryPolicy
{
    /// <summary>
    /// Determines if a synchronization client error category is transient and should trigger backoff.
    /// </summary>
    /// <param name="errorType">The sync ingest client error type.</param>
    /// <returns>True if transient; otherwise, false.</returns>
    bool IsTransient(SyncIngestClientErrorType errorType);

    /// <summary>
    /// Computes the exponential backoff delay based on the consecutive failure count.
    /// </summary>
    /// <param name="consecutiveFailureCount">The number of repeated sync failures encountered.</param>
    /// <returns>A TimeSpan representing the delay before the next retry sweep.</returns>
    TimeSpan CalculateBackoff(int consecutiveFailureCount);
}
