using System;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Configuration options for the desktop authenticated sync client.
/// </summary>
public sealed class SyncClientOptions
{
    private string _ingestPath = "/api/sync/ingest";
    private int _timeoutSeconds = 15;
    private int _clockSkewSeconds = 300;

    /// <summary>
    /// Gets or sets the base URL of the sync API.
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path of the sync ingest endpoint. Default is "/api/sync/ingest".
    /// </summary>
    public string IngestPath
    {
        get => _ingestPath;
        set => _ingestPath = value;
    }

    /// <summary>
    /// Gets or sets the request timeout in seconds. Default is 15 seconds.
    /// </summary>
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => _timeoutSeconds = value;
    }

    /// <summary>
    /// Gets or sets the token clock skew tolerance in seconds. Default is 300 seconds (5 minutes).
    /// </summary>
    public int ClockSkewSeconds
    {
        get => _clockSkewSeconds;
        set => _clockSkewSeconds = value;
    }

    /// <summary>
    /// Performs self-validation on options and returns a descriptive error message if invalid.
    /// </summary>
    /// <param name="errorMessage">The validation error message, or null if valid.</param>
    /// <returns>True if options are structurally valid; otherwise, false.</returns>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            errorMessage = "ApiBaseUrl cannot be null, empty, or whitespace.";
            return false;
        }

        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "ApiBaseUrl must be a valid absolute URI starting with http:// or https://.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(IngestPath))
        {
            errorMessage = "IngestPath cannot be null, empty, or whitespace.";
            return false;
        }

        if (!IngestPath.StartsWith('/'))
        {
            errorMessage = "IngestPath must start with a leading slash '/'.";
            return false;
        }

        if (TimeoutSeconds <= 0)
        {
            errorMessage = "TimeoutSeconds must be a positive, non-zero integer.";
            return false;
        }

        if (TimeoutSeconds > 300)
        {
            errorMessage = "TimeoutSeconds must be bounded (maximum 300 seconds).";
            return false;
        }

        if (ClockSkewSeconds < 0)
        {
            errorMessage = "ClockSkewSeconds must be zero or a positive integer.";
            return false;
        }

        if (ClockSkewSeconds > 1800)
        {
            errorMessage = "ClockSkewSeconds must be bounded (maximum 1800 seconds).";
            return false;
        }

        return true;
    }
}
