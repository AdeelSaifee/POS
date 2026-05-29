using System;
using System.Threading.Tasks;
using POS.Desktop.Services.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Unit tests verifying the safe token provider and in-memory refresh stubs.
/// </summary>
public sealed class DeviceTokenProviderTests
{
    [Fact]
    public async Task FixedDeviceTokenProvider_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = "test-jwt-token";
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var provider = new FixedDeviceTokenProvider(token, expiry);

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(token, result.Token);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(expiry, result.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FixedDeviceTokenProvider_EmptyOrBlankToken_ReturnsFailure(string? blankToken)
    {
        // Arrange
        var provider = new FixedDeviceTokenProvider(blankToken);

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("missing or blank", result.ErrorMessage);
    }

    [Fact]
    public async Task FixedDeviceTokenProvider_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var token = "expired-token";
        var expiredTime = DateTimeOffset.UtcNow.AddSeconds(-1);
        var provider = new FixedDeviceTokenProvider(token, expiredTime);

        // Act
        var result = await provider.GetTokenAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("expired", result.ErrorMessage);
    }

    [Fact]
    public async Task ForceRefreshAsync_NoRefreshDelegate_ReturnsRefreshNotConfigured()
    {
        // Arrange
        var provider = new FixedDeviceTokenProvider("token");

        // Act
        var result = await provider.ForceRefreshAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Device token refresh source is not configured.", result.ErrorMessage);
    }

    [Fact]
    public async Task ForceRefreshAsync_WithRefreshDelegate_InvokesDelegate()
    {
        // Arrange
        var refreshedToken = "refreshed-token";
        var provider = new FixedDeviceTokenProvider("token", DateTimeOffset.UtcNow, (ct) =>
            Task.FromResult(new DeviceTokenResult(true, refreshedToken)));

        // Act
        var result = await provider.ForceRefreshAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(refreshedToken, result.Token);
        Assert.Null(result.ErrorMessage);
    }
}
