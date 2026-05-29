using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Contracts.Sync;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Implements the device-authenticated sync ingest client.
/// </summary>
public sealed class SyncIngestClient : ISyncIngestClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IDeviceTokenProvider _tokenProvider;
    private readonly SyncClientOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncIngestClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client wrapper.</param>
    /// <param name="tokenProvider">The token provider helper.</param>
    /// <param name="options">The sync client options.</param>
    public SyncIngestClient(
        HttpClient httpClient,
        IDeviceTokenProvider tokenProvider,
        SyncClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<SyncIngestClientResult> IngestAsync(
        SyncIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate request is not null
        if (request is null)
        {
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Validation,
                "SyncIngestRequest payload cannot be null.",
                "NULL_REQUEST_PAYLOAD"));
        }

        // 2. Validate SyncClientOptions
        if (!_options.Validate(out var optionsError))
        {
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Configuration,
                optionsError ?? "Invalid sync client configuration options.",
                "INVALID_CONFIGURATION"));
        }

        // 3. Acquire device JWT token
        DeviceTokenResult tokenResult;
        try
        {
            tokenResult = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Unexpected,
                "An unexpected exception occurred during token retrieval.",
                "TOKEN_ACQUISITION_EXCEPTION"));
        }

        if (tokenResult is null || !tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Token))
        {
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Unauthorized,
                tokenResult?.ErrorMessage ?? "Device authentication token acquisition failed.",
                "TOKEN_ACQUISITION_FAILED"));
        }

        // 4. Build absolute request URI
        Uri requestUri;
        try
        {
            var baseUri = new Uri(_options.ApiBaseUrl!);
            requestUri = new Uri(baseUri, _options.IngestPath);
        }
        catch (Exception)
        {
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Configuration,
                "Unable to build a valid destination URI from ApiBaseUrl and IngestPath options.",
                "INVALID_URI_BUILD"));
        }

        // 5. Build and send HTTP Post
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            httpRequest.Content = JsonContent.Create(request, typeof(SyncIngestRequest), mediaType: null, SerializerOptions);

            using var responseMessage = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var responseDto = await responseMessage.Content.ReadFromJsonAsync<SyncIngestResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
                    if (responseDto is null)
                    {
                        return SyncIngestClientResult.Failed(new SyncIngestClientError(
                            SyncIngestClientErrorType.Unexpected,
                            "Received an empty response payload from the central API.",
                            "EMPTY_RESPONSE_BODY"));
                    }

                    return SyncIngestClientResult.Succeeded(responseDto);
                }
                catch (JsonException)
                {
                    return SyncIngestClientResult.Failed(new SyncIngestClientError(
                        SyncIngestClientErrorType.Unexpected,
                        "Successfully completed the network call but failed to deserialize the central acknowledgment.",
                        "JSON_DESERIALIZATION_FAILURE"));
                }
            }

            // Map HTTP Status Codes
            return responseMessage.StatusCode switch
            {
                HttpStatusCode.BadRequest => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.Validation,
                    "The server rejected the sync batch as malformed or mismatched with device identity.",
                    "BAD_REQUEST")),

                HttpStatusCode.Unauthorized => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.Unauthorized,
                    "The central server rejected the device credentials (invalid or expired token).",
                    "UNAUTHORIZED")),

                HttpStatusCode.Forbidden => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.Forbidden,
                    "The device has insufficient permissions or missing tenant/location/terminal claims.",
                    "FORBIDDEN")),

                HttpStatusCode.Conflict => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.Conflict,
                    "The sync batch conflicted with an existing key, terminal sequence, or batch duplicates.",
                    "CONFLICT")),

                HttpStatusCode.InternalServerError => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.ServerError,
                    "The central server encountered an internal exception processing the sync batch.",
                    "INTERNAL_SERVER_ERROR")),

                HttpStatusCode.NotImplemented => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.ServerError,
                    "Central persistence ledger is not implemented yet.",
                    "NOT_IMPLEMENTED")),

                _ => SyncIngestClientResult.Failed(new SyncIngestClientError(
                    SyncIngestClientErrorType.Unexpected,
                    $"The central server returned an unmapped status code: {(int)responseMessage.StatusCode}",
                    $"HTTP_ERROR_{(int)responseMessage.StatusCode}"))
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Flow was canceled via CancellationToken
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Unexpected,
                "The synchronization ingest operation was cancelled by the caller.",
                "OPERATION_CANCELLED"));
        }
        catch (OperationCanceledException)
        {
            // Timeout (since cancellationToken wasn't canceled, it was the HttpClient timeout)
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Timeout,
                $"The request timed out.",
                "REQUEST_TIMEOUT"));
        }
        catch (HttpRequestException)
        {
            // Server offline, no network, or DNS resolution failure
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Offline,
                "Host unreachable or network connection unavailable.",
                "NETWORK_OFFLINE"));
        }
        catch (Exception)
        {
            // Unexpected generic exceptions
            return SyncIngestClientResult.Failed(new SyncIngestClientError(
                SyncIngestClientErrorType.Unexpected,
                "An unexpected connection error occurred while contacting the central API.",
                "UNEXPECTED_EXCEPTION"));
        }
    }
}
