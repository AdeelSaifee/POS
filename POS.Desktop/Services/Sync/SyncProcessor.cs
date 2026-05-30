using System;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Initializes a new instance of <see cref="SyncProcessor"/>.
    /// </summary>
    /// <param name="logger">The logger helper.</param>
    /// <param name="provisioningContext">The provisioning state helper.</param>
    /// <param name="options">The sync processor options.</param>
    public SyncProcessor(
        ILogger<SyncProcessor> logger,
        IProvisionedTerminalContext provisioningContext,
        SyncProcessorOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provisioningContext = provisioningContext ?? throw new ArgumentNullException(nameof(provisioningContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
    /// Placeholder method representing a single processor sweep.
    /// For Group 1, this does not query database, call HTTP client, or mutate state.
    /// </summary>
    private Task RunOnceAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("RunOnceAsync placeholder boundary executed.");
        return Task.CompletedTask;
    }
}
