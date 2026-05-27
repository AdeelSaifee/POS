namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Represents the local persistent identity of a provisioned terminal.
/// This record is immutable and contains only non-sensitive identifier metadata
/// required for scoping queries in the local database.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="LocationId">The unique identifier of the location.</param>
/// <param name="TerminalId">The unique identifier of the terminal.</param>
public sealed record ProvisioningRecord(
    int? TenantId,
    int? LocationId,
    int? TerminalId)
{
    /// <summary>
    /// Gets a value indicating whether the provisioning record is valid and complete.
    /// A valid record must have positive, non-null values for TenantId, LocationId, and TerminalId.
    /// </summary>
    public bool IsValid =>
        TenantId.HasValue && TenantId.Value > 0 &&
        LocationId.HasValue && LocationId.Value > 0 &&
        TerminalId.HasValue && TerminalId.Value > 0;

    /// <summary>
    /// Gets a value indicating whether the terminal is in an unprovisioned state (all values are null).
    /// </summary>
    public bool IsUnprovisioned =>
        TenantId == null && LocationId == null && TerminalId == null;

    /// <summary>
    /// Gets a value indicating whether the terminal is in a fully provisioned state.
    /// </summary>
    public bool IsFullyProvisioned => IsValid;

    /// <summary>
    /// Gets a value indicating whether the terminal is in a half-provisioned or invalid state.
    /// </summary>
    public bool IsHalfProvisioned => !IsUnprovisioned && !IsFullyProvisioned;

    /// <summary>
    /// Gets a default unprovisioned instance of <see cref="ProvisioningRecord"/> with all null values.
    /// </summary>
    public static ProvisioningRecord Unprovisioned { get; } = new(null, null, null);
}
