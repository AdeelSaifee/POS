namespace POS.Api.Application.Sync;

/// <summary>
/// Represents the claims-derived authenticated identity of the POS device invoking the sync.
/// </summary>
public sealed record SyncIngestIdentity(
    /// <summary>
    /// The Tenant ID derived from the device token claims.
    /// </summary>
    int TenantId,

    /// <summary>
    /// The Location ID derived from the device token claims.
    /// </summary>
    int LocationId,

    /// <summary>
    /// The Terminal ID derived from the device token claims.
    /// </summary>
    int TerminalId,

    /// <summary>
    /// The unique Device ID (optional) derived from the device token claims.
    /// </summary>
    string? DeviceId
);
