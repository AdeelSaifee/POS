using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Defines the contract for persisting and retrieving local terminal provisioning state.
/// </summary>
public interface ITerminalProvisioningStore
{
    /// <summary>
    /// Retrieves the current provisioning record from the persistent store.
    /// </summary>
    Task<ProvisioningRecord> GetProvisioningRecordAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists the terminal provisioning record to the persistent store and updates the in-memory context.
    /// </summary>
    Task<ProvisioningResult> ProvisionTerminalAsync(int tenantId, int locationId, int terminalId, CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of a terminal provisioning operation.
/// </summary>
/// <param name="Success">True if the operation succeeded; otherwise false.</param>
/// <param name="ErrorCode">The structured error code if the operation failed.</param>
/// <param name="ErrorMessage">The operator-safe error message if the operation failed.</param>
public sealed record ProvisioningResult(bool Success, string? ErrorCode = null, string? ErrorMessage = null);
