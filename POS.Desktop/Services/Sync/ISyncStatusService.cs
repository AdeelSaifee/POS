using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for querying the terminal's synchronization status and counts cheaply and safely.
/// </summary>
public interface ISyncStatusService
{
    /// <summary>
    /// Gets the current synchronization status of the terminal.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing sync metrics and statuses.</returns>
    Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
