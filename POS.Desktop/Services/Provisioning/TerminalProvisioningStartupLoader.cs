using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Shared.Contracts;

namespace POS.Desktop.Services.Provisioning;

/// <summary>
/// Reads the persisted <see cref="POS.Desktop.Data.LocalEntities.TerminalProvisioning"/> row from the local SQLite
/// database at application startup and hydrates the singleton <see cref="IProvisionedTerminalContext"/>
/// with the stored identity.
/// </summary>
/// <remarks>
/// <para>
/// Must be called after database migrations have been applied and before the UI becomes active.
/// </para>
/// <para>
/// Fail-closed: if no row exists, or if the row is partial/invalid, the in-memory context
/// remains unprovisioned. Partial IDs are never exposed through the context.
/// </para>
/// </remarks>
public sealed class TerminalProvisioningStartupLoader
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;
    private readonly ILogger<TerminalProvisioningStartupLoader> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalProvisioningStartupLoader"/>.
    /// </summary>
    /// <param name="scopeFactory">The DI scope factory used to resolve the scoped provisioning store.</param>
    /// <param name="provisionedTerminalContext">The singleton runtime context to update.</param>
    /// <param name="logger">Logger for startup diagnostics.</param>
    public TerminalProvisioningStartupLoader(
        IServiceScopeFactory scopeFactory,
        IProvisionedTerminalContext provisionedTerminalContext,
        ILogger<TerminalProvisioningStartupLoader> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads the persisted provisioning record from SQLite and unconditionally sets the runtime context.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>If the row is fully valid, the context is updated with the stored identity.</item>
    ///   <item>If no row exists, the context is explicitly reset to <see cref="ProvisioningRecord.Unprovisioned"/> (fail-closed).</item>
    ///   <item>If the row is partial or invalid, the context is explicitly reset to <see cref="ProvisioningRecord.Unprovisioned"/> (fail-closed).</item>
    /// </list>
    /// The explicit reset in the no-row and partial/invalid branches prevents stale in-memory state
    /// (e.g. from a prior session or an appsettings seed) from remaining active when the durable
    /// SQLite source does not confirm full provisioning.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for the startup operation.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<ITerminalProvisioningStore>();

            var record = await store.GetProvisioningRecordAsync(cancellationToken).ConfigureAwait(false);

            if (record.IsFullyProvisioned)
            {
                if (_provisionedTerminalContext is ProvisionedTerminalContext concreteContext)
                {
                    concreteContext.UpdateState(record);
                }

                _logger.LogInformation(
                    "Terminal provisioning loaded from persistent store. State: provisioned.");
            }
            else if (record.IsUnprovisioned)
            {
                // No row in the database — explicitly reset to fail-closed to clear any stale
                // in-memory state that may have been seeded before startup (e.g. from appsettings).
                ResetToFailClosed();

                _logger.LogInformation(
                    "No persisted provisioning row found. Terminal set to unprovisioned.");
            }
            else
            {
                // Half-provisioned or otherwise invalid — explicitly reset to fail-closed.
                // Partial IDs must never remain active in the runtime context.
                ResetToFailClosed();

                _logger.LogWarning(
                    "Persisted provisioning row is partial or invalid. Terminal set to unprovisioned (fail-closed).");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Startup must not crash if the provisioning row is absent or the read fails.
            // Reset first so any stale/provisioned context is cleared before we return.
            ResetToFailClosed();
            _logger.LogError(ex, "An error occurred while loading provisioning state at startup. Terminal set to unprovisioned.");
        }
    }

    /// <summary>
    /// Resets the runtime context to <see cref="ProvisioningRecord.Unprovisioned"/> (fail-closed).
    /// Called in every non-provisioned code path, including exception handling, to guarantee
    /// stale in-memory state is never left active when the durable SQLite source cannot confirm
    /// full provisioning.
    /// </summary>
    private void ResetToFailClosed()
    {
        if (_provisionedTerminalContext is ProvisionedTerminalContext concreteContext)
        {
            concreteContext.UpdateState(ProvisioningRecord.Unprovisioned);
        }
    }
}
