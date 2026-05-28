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

    /// <summary>
    /// Returns the configured shift-open policy (cash limits and pre-shift checklist).
    /// Does not require an active session or open shift.
    /// Always returns safe defaults if configuration is missing or partial.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A sanitised policy result ready for UI display.</returns>
    Task<ShiftOpenPolicyResult> GetOpenPolicyAsync(CancellationToken cancellationToken = default);
}
