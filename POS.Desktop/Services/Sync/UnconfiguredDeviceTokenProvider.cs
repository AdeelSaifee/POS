using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A default implementation of <see cref="IDeviceTokenProvider"/> indicating that the token source has not been configured.
/// </summary>
public sealed class UnconfiguredDeviceTokenProvider : IDeviceTokenProvider
{
    /// <inheritdoc />
    public Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DeviceTokenResult(false, null, "Device token source is not configured."));
    }

    /// <inheritdoc />
    public Task<DeviceTokenResult> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DeviceTokenResult(false, null, "Device token refresh source is not configured."));
    }
}
