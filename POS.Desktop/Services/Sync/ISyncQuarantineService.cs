using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Sync;

/// <summary>
/// Defines the contract for querying quarantined (DeadLetter) outbox items safely.
/// </summary>
public interface ISyncQuarantineService
{
    /// <summary>
    /// Retrieves a read-only list of quarantined outbox items with safe metadata.
    /// Excludes business payload data to prevent sensitive information leakage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of quarantined item metadata DTOs.</returns>
    Task<IReadOnlyList<QuarantinedItemDto>> GetQuarantinedItemsAsync(CancellationToken cancellationToken = default);
}
