using System;
using System.Threading;
using System.Threading.Tasks;
using POS.Shared.Enums;

namespace POS.Desktop.Services.CashControl;

/// <summary>
/// Payload for recording a cash control movement.
/// </summary>
public sealed record CashControlMovementRequest(
    CashDrawerMovementType MovementType,
    decimal Amount,
    int ReasonCodeId,
    string? Comment,
    string IdempotencyKey);

/// <summary>
/// Result of a cash control operation.
/// </summary>
public sealed record CashControlMovementResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Guid? MovementId = null,
    CashDrawerMovementType? MovementType = null,
    decimal? Amount = null,
    int? ReasonCodeId = null,
    Guid? ShiftId = null,
    DateOnly? BusinessDate = null,
    long? TerminalSequence = null,
    DateTimeOffset? OccurredOn = null);

/// <summary>
/// Defines the contract for local cash drawer movement operations.
/// </summary>
public interface ICashControlService
{
    /// <summary>
    /// Records a generic cash control movement (e.g. drop) for the current active terminal session and shift.
    /// </summary>
    Task<CashControlMovementResult> RecordMovementAsync(
        CashControlMovementRequest request,
        CancellationToken cancellationToken = default);
}
