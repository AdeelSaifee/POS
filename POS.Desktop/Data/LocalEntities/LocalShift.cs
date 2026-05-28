using System;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents the local read-model/table for a Shift on the desktop client.
/// </summary>
public class LocalShift
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public int OpenedByEmployeeId { get; set; }

    public int? ClosedByEmployeeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public ShiftStatus Status { get; set; }

    public decimal OpeningCashAmount { get; set; }

    public decimal? ExpectedCashAmount { get; set; }

    public decimal? CountedCashAmount { get; set; }

    public decimal? VarianceAmount { get; set; }

    public DateTimeOffset OpenedOn { get; set; }

    public DateTimeOffset? ClosedOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
}
