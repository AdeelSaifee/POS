using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Shift : AppendOnlyEntity
{
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
}
