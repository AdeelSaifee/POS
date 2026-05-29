using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using POS.Desktop.Services.Sync;
using POS.Shared.Contracts.Sync;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Comprehensive unit tests for the SyncIngestClient verifying request formatting,
/// authorization header mapping, and HTTP status code translation without raising raw exceptions.
/// </summary>
public sealed class SyncIngestClientTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    private static SyncIngestRequest CreateSampleRequest()
    {
        return new SyncIngestRequest(
            TenantId: 101,
            LocationId: 1,
            TerminalId: 1,
            ChunkSequence: 1,
            ChunkIdempotencyKey: "idem-key-123",
            RequestHash: "req-hash-abc",
            CorrelationId: "corr-id-789",
            Events: Array.Empty<SyncIngestEvent>()
        );
    }

    private static SyncClientOptions CreateValidOptions()
    {
        return new SyncClientOptions
        {
            ApiBaseUrl = "https://localhost:5001",
            IngestPath = "/api/sync/ingest",
            TimeoutSeconds = 15,
            ClockSkewSeconds = 300
        };
    }

    [Fact]
    public async Task IngestAsync_HappyPath_ReturnsSucceededResult()
    {
        // Arrange
        var expectedResponse = new SyncIngestResponse(
            Guid.NewGuid(),
            1,
            "idem-key-123",
            "Received",
            0,
            Array.Empty<SyncIngestEventAck>(),
            null,
            null
        );

        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("https://localhost:5001/api/sync/ingest", req.RequestUri?.AbsoluteUri);
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("valid-jwt-token", req.Headers.Authorization?.Parameter);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedResponse)
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("valid-jwt-token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Response);
        Assert.Equal(expectedResponse.AckId, result.Response.AckId);
        Assert.Equal("Received", result.Response.Status);
    }

    [Fact]
    public async Task IngestAsync_InvalidConfiguration_ReturnsConfigurationError()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((r, c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var tokenProvider = new FixedDeviceTokenProvider("token");

        // Invalid options: empty base URL
        var invalidOptions = new SyncClientOptions { ApiBaseUrl = "" };
        var client = new SyncIngestClient(httpClient, tokenProvider, invalidOptions);

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Configuration, result.Error.ErrorType);
        Assert.Contains("ApiBaseUrl", result.Error.Message);
    }

    [Fact]
    public async Task IngestAsync_NullRequestPayload_ReturnsValidationError()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((r, c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Validation, result.Error.ErrorType);
        Assert.Contains("payload cannot be null", result.Error.Message);
    }

    [Fact]
    public async Task IngestAsync_TokenProviderAcquisitionFails_ReturnsUnauthorizedError()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((r, c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        // Unauthenticated provider
        var tokenProvider = new FixedDeviceTokenProvider(null);
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Unauthorized, result.Error.ErrorType);
        Assert.Contains("missing or blank", result.Error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, SyncIngestClientErrorType.Validation, "BAD_REQUEST")]
    [InlineData(HttpStatusCode.Unauthorized, SyncIngestClientErrorType.Unauthorized, "UNAUTHORIZED")]
    [InlineData(HttpStatusCode.Forbidden, SyncIngestClientErrorType.Forbidden, "FORBIDDEN")]
    [InlineData(HttpStatusCode.Conflict, SyncIngestClientErrorType.Conflict, "CONFLICT")]
    [InlineData(HttpStatusCode.InternalServerError, SyncIngestClientErrorType.ServerError, "INTERNAL_SERVER_ERROR")]
    [InlineData(HttpStatusCode.NotImplemented, SyncIngestClientErrorType.ServerError, "NOT_IMPLEMENTED")]
    public async Task IngestAsync_HttpFailureStatusCodes_MapsCorrectly(
        HttpStatusCode responseStatusCode,
        SyncIngestClientErrorType expectedErrorType,
        string expectedCode)
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(responseStatusCode));
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(expectedErrorType, result.Error.ErrorType);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_ServerOffline_MapsHttpRequestExceptionToOfflineError()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            throw new HttpRequestException("Server connection refused.");
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Offline, result.Error.ErrorType);
        Assert.Equal("NETWORK_OFFLINE", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_TimeoutPath_MapsToTimeoutError()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            // Throw OperationCanceledException without cancelling the request cancellation token
            // which simulates HttpClient internal timeout behavior
            throw new OperationCanceledException("The HttpClient timeout was reached.");
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Timeout, result.Error.ErrorType);
        Assert.Equal("REQUEST_TIMEOUT", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_UserCancellation_MapsToUnexpectedCancelledError()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            throw new OperationCanceledException(ct);
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await client.IngestAsync(CreateSampleRequest(), cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Unexpected, result.Error.ErrorType);
        Assert.Equal("OPERATION_CANCELLED", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_InvalidJsonResponseOnSuccess_MapsToUnexpectedError()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid-json-payload }")
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Unexpected, result.Error.ErrorType);
        Assert.Equal("JSON_DESERIALIZATION_FAILURE", result.Error.Code);
    }

    [Fact]
    public async Task IngestAsync_UnexpectedException_ReturnsSafeMessage()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            throw new InvalidOperationException("Raw exception message that must not leak.");
        });

        using var httpClient = new HttpClient(fakeHandler);
        var tokenProvider = new FixedDeviceTokenProvider("token");
        var client = new SyncIngestClient(httpClient, tokenProvider, CreateValidOptions());

        // Act
        var result = await client.IngestAsync(CreateSampleRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Response);
        Assert.NotNull(result.Error);
        Assert.Equal(SyncIngestClientErrorType.Unexpected, result.Error.ErrorType);
        Assert.Equal("UNEXPECTED_EXCEPTION", result.Error.Code);
        Assert.Equal("An unexpected connection error occurred while contacting the central API.", result.Error.Message);
        Assert.DoesNotContain("Raw exception message", result.Error.Message);
    }
}
