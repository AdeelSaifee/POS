using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Implements network connectivity detection using cheap, cached OS-level APIs.
/// </summary>
public sealed class OsNetworkConnectivityService : ISyncConnectivityService
{
    private readonly ILogger<OsNetworkConnectivityService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OsNetworkConnectivityService"/>.
    /// </summary>
    /// <param name="logger">The logger helper.</param>
    public OsNetworkConnectivityService(ILogger<OsNetworkConnectivityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Cheap, non-network-blocking OS status query. No DNS lookups, no pinging, no sockets.
            bool isAvailable = NetworkInterface.GetIsNetworkAvailable();
            return Task.FromResult(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An exception occurred while querying NetworkInterface.GetIsNetworkAvailable. Defaulting to true.");
            return Task.FromResult(true); // Safe fallback to prevent blocking sync pipeline
        }
    }
}
