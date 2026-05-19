using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class InventoryMovement : AppendOnlyEntity
{
    public int LocationId { get; set; }

    public int? TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public int ItemId { get; set; }

    public int ItemVariantId { get; set; }

    public Guid? SourceOrderId { get; set; }

    public Guid? SourceOrderLineId { get; set; }

    public int? ReasonCodeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long? TerminalSequence { get; set; }

    public InventoryMovementType MovementType { get; set; }

    public decimal QuantityDelta { get; set; }

    public int UnitOfMeasureId { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? StockBefore { get; set; }

    public decimal? StockAfter { get; set; }

    public InventoryExceptionStatus ExceptionStatus { get; set; }

    public string? ExceptionDetailsJson { get; set; }

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? AppliedOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }
}
