using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class CashAccountMovement : AppendOnlyEntity
{
    public int LocationId { get; set; }

    public int? TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long? TerminalSequence { get; set; }

    public CashAccountMovementType MovementType { get; set; }

    public CashAccountMovementStatus Status { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public int? SourceCashAccountId { get; set; }

    public int? DestinationCashAccountId { get; set; }

    public int PerformedByEmployeeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public int? VerifiedByEmployeeId { get; set; }

    public DateTimeOffset? VerifiedOn { get; set; }

    public int? ReasonCodeId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Comment { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredOn { get; set; }
}
