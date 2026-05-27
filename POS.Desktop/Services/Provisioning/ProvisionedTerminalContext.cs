using System;
using POS.Shared.Contracts;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// A real implementation of <see cref="IProvisionedTerminalContext"/> that manages the in-memory
/// state of terminal provisioning, failing closed if the state is invalid, half-provisioned, or unprovisioned.
/// </summary>
public sealed class ProvisionedTerminalContext : IProvisionedTerminalContext
{
    private ProvisioningRecord _record = ProvisioningRecord.Unprovisioned;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ProvisionedTerminalContext"/> in an unprovisioned state.
    /// </summary>
    public ProvisionedTerminalContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ProvisionedTerminalContext"/> with the specified initial provisioning record.
    /// </summary>
    /// <param name="initialRecord">The initial provisioning record.</param>
    public ProvisionedTerminalContext(ProvisioningRecord initialRecord)
    {
        UpdateState(initialRecord);
    }

    /// <summary>
    /// Gets the current Tenant ID. Returns 0 (fail-closed) if the terminal is not fully provisioned.
    /// </summary>
    public int CurrentTenantId
    {
        get
        {
            lock (_lock)
            {
                return _record.IsFullyProvisioned ? _record.TenantId!.Value : 0;
            }
        }
    }

    /// <summary>
    /// Gets the current Location ID. Returns 0 (fail-closed) if the terminal is not fully provisioned.
    /// </summary>
    public int CurrentLocationId
    {
        get
        {
            lock (_lock)
            {
                return _record.IsFullyProvisioned ? _record.LocationId!.Value : 0;
            }
        }
    }

    /// <summary>
    /// Gets the current Terminal ID. Returns 0 (fail-closed) if the terminal is not fully provisioned.
    /// </summary>
    public int CurrentTerminalId
    {
        get
        {
            lock (_lock)
            {
                return _record.IsFullyProvisioned ? _record.TerminalId!.Value : 0;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the terminal is fully provisioned with valid values.
    /// </summary>
    public bool IsProvisioned
    {
        get
        {
            lock (_lock)
            {
                return _record.IsFullyProvisioned;
            }
        }
    }

    /// <summary>
    /// Gets the current read-only snapshot of the provisioning record.
    /// </summary>
    public ProvisioningRecord CurrentRecord
    {
        get
        {
            lock (_lock)
            {
                return _record;
            }
        }
    }

    /// <summary>
    /// Updates the context's internal state with the provided provisioning record.
    /// If the provided record is null, throws <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="record">The new provisioning record.</param>
    public void UpdateState(ProvisioningRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            _record = record;
        }
    }
}
