using System;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents a locally persisted cash drawer movement record.
/// </summary>
public class LocalCashDrawerMovement
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid ShiftId { get; set; }

    public int EmployeeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public int ReasonCodeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public CashDrawerMovementType MovementType { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public string? Comment { get; set; }

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; }
}
