using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// A safe, in-memory IDeviceTokenProvider designed for testing and development environments.
/// This provider does not store credentials on disk, generate JWTs, or access private server keys.
/// </summary>
public sealed class FixedDeviceTokenProvider : IDeviceTokenProvider
{
    private readonly string? _token;
    private readonly DateTimeOffset? _expiresAtUtc;
    private readonly Func<CancellationToken, Task<DeviceTokenResult>>? _refreshDelegate;

    /// <summary>
    /// Initializes a new instance of <see cref="FixedDeviceTokenProvider"/> with a static token and optional expiry.
    /// </summary>
    /// <param name="token">The raw JWT token string.</param>
    /// <param name="expiresAtUtc">Optional token expiration time in UTC.</param>
    public FixedDeviceTokenProvider(string? token, DateTimeOffset? expiresAtUtc = null)
    {
        _token = token;
        _expiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FixedDeviceTokenProvider"/> with a static token and an injected refresh delegate (for tests).
    /// </summary>
    /// <param name="token">The raw JWT token string.</param>
    /// <param name="expiresAtUtc">Optional token expiration time in UTC.</param>
    /// <param name="refreshDelegate">A delegate invoked to perform token refresh in testing environments.</param>
    public FixedDeviceTokenProvider(string? token, DateTimeOffset? expiresAtUtc, Func<CancellationToken, Task<DeviceTokenResult>> refreshDelegate)
    {
        _token = token;
        _expiresAtUtc = expiresAtUtc;
        _refreshDelegate = refreshDelegate;
    }

    /// <inheritdoc />
    public Task<DeviceTokenResult> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            return Task.FromResult(new DeviceTokenResult(false, null, "Device token is missing or blank."));
        }

        if (_expiresAtUtc.HasValue && _expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            return Task.FromResult(new DeviceTokenResult(false, null, "Device token has expired."));
        }

        return Task.FromResult(new DeviceTokenResult(true, _token, null, _expiresAtUtc));
    }

    /// <inheritdoc />
    public async Task<DeviceTokenResult> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_refreshDelegate != null)
        {
            return await _refreshDelegate(cancellationToken).ConfigureAwait(false);
        }

        return new DeviceTokenResult(false, null, "Device token refresh source is not configured.");
    }
}
