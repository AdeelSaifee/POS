using System;
using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Encapsulates the safe outcome of a synchronization ingest operation, returning typed results instead of bubbling exceptions.
/// </summary>
public sealed class SyncIngestClientResult
{
    /// <summary>
    /// Gets a value indicating whether the request was completed and acknowledged successfully by the API.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the central server sync acknowledgment response if the request succeeded.
    /// </summary>
    public SyncIngestResponse? Response { get; }

    /// <summary>
    /// Gets the categorized synchronization error details if the request failed.
    /// </summary>
    public SyncIngestClientError? Error { get; }

    private SyncIngestClientResult(bool success, SyncIngestResponse? response, SyncIngestClientError? error)
    {
        Success = success;
        Response = response;
        Error = error;
    }

    /// <summary>
    /// Creates a successful synchronization ingest result carrying the server acknowledgment response.
    /// </summary>
    /// <param name="response">The server response acknowledgment.</param>
    /// <returns>A successful <see cref="SyncIngestClientResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the response is null.</exception>
    public static SyncIngestClientResult Succeeded(SyncIngestResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new SyncIngestClientResult(true, response, null);
    }

    /// <summary>
    /// Creates a failed synchronization ingest result carrying the structured error details.
    /// </summary>
    /// <param name="error">The structured error details.</param>
    /// <returns>A failed <see cref="SyncIngestClientResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the error is null.</exception>
    public static SyncIngestClientResult Failed(SyncIngestClientError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new SyncIngestClientResult(false, null, error);
    }
}
