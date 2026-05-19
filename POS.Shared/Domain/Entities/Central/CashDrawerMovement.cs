using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class CashDrawerMovement : AppendOnlyEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid ShiftId { get; set; }

    public int EmployeeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public int? ReasonCodeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public CashDrawerMovementType MovementType { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }
}
