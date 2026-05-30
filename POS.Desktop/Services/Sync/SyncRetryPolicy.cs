using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Default implementation of <see cref="ISyncRetryPolicy"/> providing capped exponential backoff.
/// </summary>
public sealed class SyncRetryPolicy : ISyncRetryPolicy
{
    private readonly SyncProcessorOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncRetryPolicy"/>.
    /// </summary>
    /// <param name="options">The sync processor options configuration.</param>
    public SyncRetryPolicy(SyncProcessorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool IsTransient(SyncIngestClientErrorType errorType)
    {
        return errorType switch
        {
            SyncIngestClientErrorType.Offline => true,
            SyncIngestClientErrorType.Timeout => true,
            SyncIngestClientErrorType.ServerError => true,
            SyncIngestClientErrorType.Unexpected => true,
            SyncIngestClientErrorType.Unauthorized => true,
            _ => false
        };
    }

    /// <inheritdoc />
    public TimeSpan CalculateBackoff(int consecutiveFailureCount)
    {
        if (consecutiveFailureCount <= 0)
        {
            return TimeSpan.FromSeconds(_options.PollIntervalSeconds);
        }

        // Cap input to prevent overflow inside Math.Pow double calculation
        var cappedAttempts = Math.Min(consecutiveFailureCount, 30);
        var delaySeconds = _options.InitialBackoffSeconds * Math.Pow(_options.BackoffMultiplier, cappedAttempts - 1);

        if (delaySeconds > _options.MaxBackoffSeconds || double.IsInfinity(delaySeconds) || double.IsNaN(delaySeconds))
        {
            return TimeSpan.FromSeconds(_options.MaxBackoffSeconds);
        }

        return TimeSpan.FromSeconds(delaySeconds);
    }
}
