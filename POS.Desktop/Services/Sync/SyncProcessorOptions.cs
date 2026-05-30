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

        return true;
    }
}
