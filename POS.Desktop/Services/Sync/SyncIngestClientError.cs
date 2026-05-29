namespace POS.Desktop.Services.Sync;

/// <summary>
/// Specifies the distinct categories of synchronization failures.
/// </summary>
public enum SyncIngestClientErrorType
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The synchronization options were invalid or not bound.
    /// </summary>
    Configuration = 1,

    /// <summary>
    /// The host is unreachable or there is no network connectivity (socket/connection failures).
    /// </summary>
    Offline = 2,

    /// <summary>
    /// The synchronization request timed out before receiving a server response.
    /// </summary>
    Timeout = 3,

    /// <summary>
    /// Authentication bearer JWT is missing, signature-invalid, or expired.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// The authenticated device lacks permissions (PosDevice policy block) or tenant/location claims are invalid.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// An idempotency key, sequence constraint, or batch duplicate conflict occurred (409 Conflict).
    /// </summary>
    Conflict = 6,

    /// <summary>
    /// The request payload was rejected as malformed or mismatched with identity claims (400 Bad Request).
    /// </summary>
    Validation = 7,

    /// <summary>
    /// An unexpected server-side error occurred (500 Internal Server Error) or the feature is unimplemented (501).
    /// </summary>
    ServerError = 8,

    /// <summary>
    /// An unexpected exception or protocol violation occurred on the client side.
    /// </summary>
    Unexpected = 9
}

/// <summary>
/// A structurally safe, non-leaking representation of a synchronization client error.
/// </summary>
public sealed record SyncIngestClientError(
    SyncIngestClientErrorType ErrorType,
    string Message,
    string? Code = null
)
{
    /// <summary>
    /// A pre-defined instance representing no error.
    /// </summary>
    public static SyncIngestClientError None { get; } = new(SyncIngestClientErrorType.None, "No error occurred.");
}
