using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class ManagerAction : AppendOnlyEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? OrderLineId { get; set; }

    public int PerformedByEmployeeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public int? ReasonCodeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredOn { get; set; }
}
