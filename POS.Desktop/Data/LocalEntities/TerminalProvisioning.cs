using System;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents the local persistent store for terminal provisioning state.
/// This table must contain at most one row to enforce the single-terminal identity.
/// </summary>
public sealed class TerminalProvisioning
{
    /// <summary>
    /// Unique identifier. For a single-row configuration, this is always 1.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unique identifier of the tenant. Null if unprovisioned.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// The unique identifier of the location. Null if unprovisioned.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// The unique identifier of the terminal. Null if unprovisioned.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// The timestamp when the terminal was provisioned or updated in UTC. Null if unprovisioned.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
