using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for verifying network connectivity in a cheap, non-blocking manner.
/// </summary>
public interface ISyncConnectivityService
{
    /// <summary>
    /// Checks whether the terminal has network connectivity.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if network connectivity is available; otherwise, false.</returns>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}
