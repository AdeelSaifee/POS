using System;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Focused unit tests verifying the correctness of sync client options, error models, and result wrappers.
/// </summary>
public sealed class SyncClientOptionsTests
{
    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://localhost:5001")]
    [InlineData("https://sync.imagynpos.com")]
    public void SyncClientOptions_ValidAbsoluteApiBaseUrl_PassesValidation(string apiBaseUrl)
    {
        // Arrange
        var options = new SyncClientOptions
        {
            ApiBaseUrl = apiBaseUrl,
            IngestPath = "/api/sync/ingest",
            TimeoutSeconds = 15,
            ClockSkewSeconds = 300
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-url")]
    [InlineData("ftp://localhost:5000")]
    [InlineData("localhost:5000")]
    public void SyncClientOptions_InvalidApiBaseUrl_FailsValidation(string? apiBaseUrl)
    {
        // Arrange
        var options = new SyncClientOptions
        {
            ApiBaseUrl = apiBaseUrl,
            IngestPath = "/api/sync/ingest",
            TimeoutSeconds = 15,
            ClockSkewSeconds = 300
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("ApiBaseUrl", errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("api/sync/ingest")]
    public void SyncClientOptions_InvalidIngestPath_FailsValidation(string? ingestPath)
    {
        // Arrange
        var options = new SyncClientOptions
        {
            ApiBaseUrl = "https://localhost:5001",
            IngestPath = ingestPath!,
            TimeoutSeconds = 15,
            ClockSkewSeconds = 300
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("IngestPath", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(301)]
    public void SyncClientOptions_InvalidTimeoutSeconds_FailsValidation(int timeoutSeconds)
    {
        // Arrange
        var options = new SyncClientOptions
        {
            ApiBaseUrl = "https://localhost:5001",
            IngestPath = "/api/sync/ingest",
            TimeoutSeconds = timeoutSeconds,
            ClockSkewSeconds = 300
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("TimeoutSeconds", errorMessage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1801)]
    public void SyncClientOptions_InvalidClockSkewSeconds_FailsValidation(int clockSkewSeconds)
    {
        // Arrange
        var options = new SyncClientOptions
        {
            ApiBaseUrl = "https://localhost:5001",
            IngestPath = "/api/sync/ingest",
            TimeoutSeconds = 15,
            ClockSkewSeconds = clockSkewSeconds
        };

        // Act
        var isValid = options.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("ClockSkewSeconds", errorMessage);
    }

    [Fact]
    public void SyncIngestClientResult_Succeeded_StoresResponseAndNoSpecificError()
    {
        // Arrange
        var mockResponse = new SyncIngestResponse(
            Guid.NewGuid(),
            1,
            "test-idem-key",
            "Received",
            0,
            Array.Empty<SyncIngestEventAck>(),
            null,
            null
        );

        // Act
        var result = SyncIngestClientResult.Succeeded(mockResponse);

        // Assert
        Assert.True(result.Success);
        Assert.Same(mockResponse, result.Response);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SyncIngestClientResult_Failed_StoresErrorAndNoResponse()
    {
        // Arrange
        var mockError = new SyncIngestClientError(SyncIngestClientErrorType.Offline, "Device offline.");

        // Act
        var result = SyncIngestClientResult.Failed(mockError);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.Same(mockError, result.Error);
    }

    [Fact]
    public void SyncIngestClientError_StoresSafeDetailsAndNoSensitiveExceptions()
    {
        // Arrange
        var error = new SyncIngestClientError(
            SyncIngestClientErrorType.Offline,
            "No network connectivity",
            "NETWORK_UNREACHABLE"
        );

        // Assert
        Assert.Equal(SyncIngestClientErrorType.Offline, error.ErrorType);
        Assert.Equal("No network connectivity", error.Message);
        Assert.Equal("NETWORK_UNREACHABLE", error.Code);
        Assert.Equal(SyncIngestClientErrorType.None, SyncIngestClientError.None.ErrorType);
    }
}
