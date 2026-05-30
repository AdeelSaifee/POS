using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using POS.Desktop.Configuration;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Verifies the correct DI registration and options binding of Sync services inside DesktopHostBuilder.
/// </summary>
public sealed class SyncDiResolutionTests
{
    [Fact]
    public void DesktopHostBuilder_RegistersSyncOptionsAndClientSuccessfully()
    {
        // Arrange
        using var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Act
        var options = sp.GetRequiredService<SyncClientOptions>();
        var optionsWrapper = sp.GetRequiredService<IOptions<SyncClientOptions>>();
        var processorOptions = sp.GetRequiredService<SyncProcessorOptions>();
        var processorOptionsWrapper = sp.GetRequiredService<IOptions<SyncProcessorOptions>>();
        var tokenProvider = sp.GetRequiredService<IDeviceTokenProvider>();
        var syncClient = sp.GetRequiredService<ISyncIngestClient>();
        var batchReader = sp.GetRequiredService<ISyncOutboxBatchReader>();
        var requestBuilder = sp.GetRequiredService<ISyncIngestRequestBuilder>();
        var ackApplier = sp.GetRequiredService<ISyncAckApplier>();
        var retryPolicy = sp.GetRequiredService<ISyncRetryPolicy>();
        var hostedServices = sp.GetServices<IHostedService>();

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(optionsWrapper);
        Assert.NotNull(processorOptions);
        Assert.NotNull(processorOptionsWrapper);
        Assert.NotNull(tokenProvider);
        Assert.NotNull(syncClient);
        Assert.NotNull(batchReader);
        Assert.NotNull(requestBuilder);
        Assert.NotNull(ackApplier);
        Assert.NotNull(retryPolicy);
        Assert.IsType<SyncIngestRequestBuilder>(requestBuilder);
        Assert.IsType<EfSyncOutboxBatchReader>(batchReader);
        Assert.IsType<EfSyncAckApplier>(ackApplier);
        Assert.IsType<SyncRetryPolicy>(retryPolicy);
        Assert.Contains(hostedServices, s => s is SyncProcessor);

        // Verify default options bindings are resolved from appsettings.json
        Assert.Equal("https://localhost:5001", options.ApiBaseUrl);
        Assert.Equal("/api/sync/ingest", options.IngestPath);
        Assert.Equal(15, options.TimeoutSeconds);
        Assert.Equal(300, options.ClockSkewSeconds);
        Assert.Equal(50, processorOptions.BatchSize);
        Assert.Equal(10, processorOptions.PollIntervalSeconds);
        Assert.Equal(2, processorOptions.InitialBackoffSeconds);
        Assert.Equal(300, processorOptions.MaxBackoffSeconds);
        Assert.Equal(2.0, processorOptions.BackoffMultiplier);

        // Verify token provider is UnconfiguredDeviceTokenProvider by default
        Assert.IsType<UnconfiguredDeviceTokenProvider>(tokenProvider);
    }

    [Fact]
    public async Task DesktopHostBuilder_UnconfiguredTokenProvider_ReturnsGracefulFailureResult()
    {
        // Arrange
        using var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var tokenProvider = sp.GetRequiredService<IDeviceTokenProvider>();

        // Act
        var tokenResult = await tokenProvider.GetTokenAsync();
        var refreshResult = await tokenProvider.ForceRefreshAsync();

        // Assert
        Assert.False(tokenResult.Success);
        Assert.Null(tokenResult.Token);
        Assert.Equal("Device token source is not configured.", tokenResult.ErrorMessage);

        Assert.False(refreshResult.Success);
        Assert.Null(refreshResult.Token);
        Assert.Equal("Device token refresh source is not configured.", refreshResult.ErrorMessage);
    }

    [Fact]
    public async Task DesktopHostBuilder_ClientResolution_ReturnsFailureWhenAuthSourceUnconfigured()
    {
        // Arrange
        using var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var syncClient = sp.GetRequiredService<ISyncIngestClient>();
        var sampleRequest = new SyncIngestRequest(
            TenantId: 1,
            LocationId: 1,
            TerminalId: 1,
            ChunkSequence: 1,
            ChunkIdempotencyKey: "key-123",
            RequestHash: "hash-xyz",
            CorrelationId: "corr-123",
            Events: Array.Empty<SyncIngestEvent>()
        );

        // Act
        var result = await syncClient.IngestAsync(sampleRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Unauthorized, result.Error.ErrorType);
        Assert.Equal("TOKEN_ACQUISITION_FAILED", result.Error.Code);
        Assert.Equal("Device token source is not configured.", result.Error.Message);
    }

    [Theory]
    [InlineData("invalid-url-here", -5, 15)]
    [InlineData("", 500, 15)]
    [InlineData("http://localhost:5000", 120, 120)]
    public void AddHttpClient_ConfiguredWithVariousOptions_ResolvesSafelyAndSetsExpectedValues(
        string? apiBaseUrl,
        int timeoutSeconds,
        int expectedTimeoutSeconds)
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new SyncClientOptions
        {
            ApiBaseUrl = apiBaseUrl,
            TimeoutSeconds = timeoutSeconds
        };

        services.AddSingleton(options);
        services.AddSingleton<IDeviceTokenProvider, UnconfiguredDeviceTokenProvider>();

        services.AddHttpClient<ISyncIngestClient, SyncIngestClient>((serviceProvider, client) =>
        {
            var opt = serviceProvider.GetRequiredService<SyncClientOptions>();

            if (Uri.TryCreate(opt.ApiBaseUrl, UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
            {
                client.BaseAddress = baseUri;
            }

            var timeoutSecs = opt.TimeoutSeconds is > 0 and <= 300
                ? opt.TimeoutSeconds
                : 15;

            client.Timeout = TimeSpan.FromSeconds(timeoutSecs);
        });

        using var sp = services.BuildServiceProvider();

        // Act
        var client = sp.GetRequiredService<ISyncIngestClient>();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(typeof(ISyncIngestClient).Name);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(expectedTimeoutSeconds), httpClient.Timeout);

        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var expectedUri) &&
            (expectedUri.Scheme == Uri.UriSchemeHttp || expectedUri.Scheme == Uri.UriSchemeHttps))
        {
            Assert.Equal(expectedUri, httpClient.BaseAddress);
        }
        else
        {
            Assert.Null(httpClient.BaseAddress);
        }
    }
}
