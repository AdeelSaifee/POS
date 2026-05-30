using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Shared.Contracts;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Background hosted service that handles draining and syncing the local transaction outbox.
/// </summary>
public sealed class SyncProcessor : BackgroundService
{
    private readonly ILogger<SyncProcessor> _logger;
    private readonly IProvisionedTerminalContext _provisioningContext;
    private readonly SyncProcessorOptions _options;
    private readonly ISyncRetryPolicy _retryPolicy;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<string> _successfullyPostedChunkKeysThisSession = new(StringComparer.Ordinal);
    private readonly object _guardLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SyncProcessor"/>.
    /// </summary>
    /// <param name="logger">The logger helper.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    /// <param name="retryPolicy">The retry policy helper.</param>
    /// <param name="scopeFactory">The service scope factory to manage scoped services safely.</param>
    public SyncProcessor(
        ILogger<SyncProcessor> logger,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options,
        ISyncRetryPolicy retryPolicy,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to allow hosted service initialization to complete without blocking
        // the generic host bootstrapper/UI thread.
        await Task.Yield();

        _logger.LogInformation("Sync outbox drain processor background service started.");

        // Validate options at startup
        if (!_options.Validate(out var validationError))
        {
            _logger.LogError("SyncProcessorOptions validation failed: {Error}. Sync processor stopping.", validationError);
            return;
        }

        int consecutiveFailureCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            bool success = false;
            try
            {
                if (!_provisioningContext.IsProvisioned)
                {
                    _logger.LogDebug("Terminal is not provisioned. Sync processor is idle.");
                    success = true; // Safe idle status does not count as failure
                }
                else
                {
                    _logger.LogDebug("Sync processor checking outbox. Terminal is provisioned.");
                    success = await RunOnceAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Safe cooperative cancellation check. Break cleanly.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred inside the sync processor execution loop.");
                success = false;
            }

            if (success)
            {
                consecutiveFailureCount = 0;
            }
            else
            {
                consecutiveFailureCount++;
            }

            try
            {
                TimeSpan delay;
                if (consecutiveFailureCount == 0)
                {
                    delay = TimeSpan.FromSeconds(_options.PollIntervalSeconds);
                }
                else
                {
                    delay = _retryPolicy.CalculateBackoff(consecutiveFailureCount);
                    _logger.LogWarning(
                        "Sync processor backoff applied. Failure count: {Count}. Delaying for {DelaySeconds}s.",
                        consecutiveFailureCount,
                        delay.TotalSeconds);
                }

                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful cancellation caught during Task.Delay. Break cleanly.
                break;
            }
        }

        _logger.LogInformation("Sync outbox drain processor background service stopped cleanly.");
    }

    /// <summary>
    /// Performs a single processor sweep using a scoped batch reader, a request builder, a sync client, and an ack applier.
    /// </summary>
    /// <returns>True if the sweep was successful (or no-op); false if a sync or write failure occurred.</returns>
    private async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var reader = scope.ServiceProvider.GetRequiredService<ISyncOutboxBatchReader>();
            var builder = scope.ServiceProvider.GetRequiredService<ISyncIngestRequestBuilder>();
            var client = scope.ServiceProvider.GetRequiredService<ISyncIngestClient>();
            var ackApplier = scope.ServiceProvider.GetRequiredService<ISyncAckApplier>();

            var batch = await reader.ReadPendingBatchAsync(cancellationToken).ConfigureAwait(false);

            if (!batch.HasItems)
            {
                _logger.LogDebug("SyncProcessor assembled an empty outbox batch.");
                return true; // No pending items is a successful sweep
            }

            _logger.LogInformation("SyncProcessor assembled outbox batch containing {Count} pending events.", batch.Count);

            var request = builder.Build(batch);

            lock (_guardLock)
            {
                if (_successfullyPostedChunkKeysThisSession.Contains(request.ChunkIdempotencyKey))
                {
                    _logger.LogDebug(
                        "SyncProcessor: Batch {Sequence} with key {Key} was already successfully posted in this process session. Skipping re-transmission.",
                        request.ChunkSequence,
                        request.ChunkIdempotencyKey);
                    return true; // Already successfully posted during session
                }
            }

            var result = await client.IngestAsync(request, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                if (result.Response == null)
                {
                    _logger.LogError(
                        "SyncProcessor successfully posted outbox batch {Sequence} but received a null response payload from Central. Skipping local database mutations.",
                        request.ChunkSequence);

                    var error = new SyncIngestClientError(SyncIngestClientErrorType.Unexpected, "Received a null response payload from Central.", "NULL_RESPONSE");
                    await ackApplier.ApplyFailureAsync(batch, request, error, cancellationToken).ConfigureAwait(false);
                    return false;
                }

                _logger.LogInformation(
                    "SyncProcessor successfully posted outbox batch {Sequence} containing {Count} events. IdempotencyKey: {Key}. Applying local DB updates.",
                    request.ChunkSequence,
                    request.Events.Count,
                    request.ChunkIdempotencyKey);

                var applyResult = await ackApplier.ApplySuccessAsync(batch, request, result.Response, cancellationToken).ConfigureAwait(false);

                if (applyResult.Success)
                {
                    _logger.LogInformation(
                        "SyncProcessor successfully persisted outbox batch {Sequence} in local DB. Mark count: {Count}.",
                        request.ChunkSequence,
                        applyResult.AckedRowCount);

                    lock (_guardLock)
                    {
                        _successfullyPostedChunkKeysThisSession.Add(request.ChunkIdempotencyKey);
                    }
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "SyncProcessor failed to apply outbox database updates for batch {Sequence}. ErrorCode: {Code}, Message: {Message}",
                        request.ChunkSequence,
                        applyResult.ErrorCode,
                        applyResult.ErrorMessage);

                    var error = new SyncIngestClientError(SyncIngestClientErrorType.Unexpected, applyResult.ErrorMessage ?? "Local DB ack apply failed.", applyResult.ErrorCode ?? "DB_WRITE_ERROR");
                    await ackApplier.ApplyFailureAsync(batch, request, error, cancellationToken).ConfigureAwait(false);
                    return false;
                }
            }
            else
            {
                var error = result.Error;
                _logger.LogError(
                    "SyncProcessor failed to post outbox batch {Sequence}. ErrorType: {ErrorType}, Code: {Code}, Message: {Message}",
                    request.ChunkSequence,
                    error?.ErrorType,
                    error?.Code,
                    error?.Message);

                await ackApplier.ApplyFailureAsync(batch, request, error, cancellationToken).ConfigureAwait(false);
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Propagate cancellation correctly
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute outbox batch read cycle in RunOnceAsync.");
            return false;
        }
    }
}
