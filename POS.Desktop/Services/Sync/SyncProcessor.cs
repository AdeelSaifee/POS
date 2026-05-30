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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<string> _successfullyPostedChunkKeysThisSession = new(StringComparer.Ordinal);
    private readonly object _guardLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SyncProcessor"/>.
    /// </summary>
    /// <param name="logger">The logger helper.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    /// <param name="scopeFactory">The service scope factory to manage scoped services safely.</param>
    public SyncProcessor(
        ILogger<SyncProcessor> logger,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_provisioningContext.IsProvisioned)
                {
                    _logger.LogDebug("Terminal is not provisioned. Sync processor is idle.");
                }
                else
                {
                    _logger.LogDebug("Sync processor checking outbox. Terminal is provisioned.");
                    await RunOnceAsync(stoppingToken).ConfigureAwait(false);
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
            }

            try
            {
                // Delay using configured poll interval
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
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
    private async Task RunOnceAsync(CancellationToken cancellationToken)
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
                return;
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
                    return;
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
                    return;
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
                }
                else
                {
                    _logger.LogError(
                        "SyncProcessor failed to apply outbox database updates for batch {Sequence}. ErrorCode: {Code}, Message: {Message}",
                        request.ChunkSequence,
                        applyResult.ErrorCode,
                        applyResult.ErrorMessage);
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
        }
    }
}
