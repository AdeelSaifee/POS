using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Configuration options for the background outbox sync drain processor.
/// </summary>
public sealed class SyncProcessorOptions
{
    /// <summary>
    /// Gets or sets the batch size of events to query and sync in a single operation. Default is 50.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the poll interval in seconds for checking the sync outbox. Default is 10.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the initial backoff delay in seconds. Default is 2.
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum backoff delay in seconds. Default is 300 (5 minutes).
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the exponential backoff multiplier. Default is 2.0.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the maximum number of sync attempts before quarantining/dead-lettering a row. Default is 5.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Performs self-validation on options and returns a descriptive error message if invalid.
    /// </summary>
    /// <param name="errorMessage">The validation error message, or null if valid.</param>
    /// <returns>True if options are structurally valid; otherwise, false.</returns>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (BatchSize < 1 || BatchSize > 500)
        {
            errorMessage = "BatchSize must be between 1 and 500.";
            return false;
        }

        if (PollIntervalSeconds < 1 || PollIntervalSeconds > 3600)
        {
            errorMessage = "PollIntervalSeconds must be between 1 and 3600.";
            return false;
        }

        if (InitialBackoffSeconds < 1 || InitialBackoffSeconds > 3600)
        {
            errorMessage = "InitialBackoffSeconds must be between 1 and 3600.";
            return false;
        }

        if (MaxBackoffSeconds < 1 || MaxBackoffSeconds > 3600)
        {
            errorMessage = "MaxBackoffSeconds must be between 1 and 3600.";
            return false;
        }

        if (MaxBackoffSeconds < InitialBackoffSeconds)
        {
            errorMessage = "MaxBackoffSeconds cannot be less than InitialBackoffSeconds.";
            return false;
        }

        if (BackoffMultiplier < 1.0 || BackoffMultiplier > 10.0)
        {
            errorMessage = "BackoffMultiplier must be between 1.0 and 10.0.";
            return false;
        }

        if (MaxRetryAttempts < 1 || MaxRetryAttempts > 100)
        {
            errorMessage = "MaxRetryAttempts must be between 1 and 100.";
            return false;
        }

        return true;
    }
}
