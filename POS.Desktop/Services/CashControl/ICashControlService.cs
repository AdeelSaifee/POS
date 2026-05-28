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
    string IdempotencyKey,
    string? ManagerOperatorId = null,
    string? ManagerPin = null);

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
/// Detailed summary of the cash drawer status, sales, drops, and alert states.
/// </summary>
public sealed record CashDrawerSummaryResult(
    bool IsOpen,
    Guid? ShiftId,
    DateOnly? BusinessDate,
    decimal OpeningFloat,
    decimal CashSales,
    decimal SafeDrops,
    decimal FloatInjections,
    decimal ExpectedDrawerBalance,
    int TransactionCount,
    DateTimeOffset? LastMovementAt,
    string AlertCode,
    string? AlertMessage,
    bool IsSafeDropRecommended,
    bool IsOverLimit,
    decimal CashDrawerLimit,
    decimal SafeDropThreshold);

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

    /// <summary>
    /// Computes and returns the live summary and balance details for the current open cash drawer.
    /// </summary>
    Task<CashDrawerSummaryResult> GetDrawerSummaryAsync(CancellationToken cancellationToken = default);
}
