using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Defines the contract for shift management operations.
/// </summary>
public interface IShiftService
{
    /// <summary>
    /// Opens a shift with the specified opening float amount.
    /// </summary>
    /// <param name="openingFloat">The opening float cash amount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result object containing operation outcome details.</returns>
    Task<ShiftOpenResult> OpenShiftAsync(decimal openingFloat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current active/open shift on the provisioned terminal.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing current open shift details or false if none exists.</returns>
    Task<ShiftDetailsResult> GetCurrentShiftAsync(CancellationToken cancellationToken = default);
}
