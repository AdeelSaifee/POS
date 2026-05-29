using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Represents the result of a device token acquisition attempt.
/// </summary>
/// <param name="Success">True if a valid token was acquired; otherwise, false.</param>
/// <param name="Token">The acquired JWT bearer token string, or null on failure.</param>
/// <param name="ErrorMessage">An operator-safe error description if acquisition failed.</param>
public sealed record DeviceTokenResult(
    bool Success,
    string? Token = null,
    string? ErrorMessage = null
);

/// <summary>
/// Defines the local contract for managing and acquiring authenticated device access tokens.
/// </summary>
public interface IDeviceTokenProvider
{
    /// <summary>
    /// Acquires a valid device access token asynchronously.
    /// This method is responsible for returning a cached token if valid, or transparently refreshing it.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured result carrying the JWT token or error details.</returns>
    Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default);
}
