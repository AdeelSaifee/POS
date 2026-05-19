using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class TerminalSession : TenantScopedEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public int EmployeeId { get; set; }

    public Guid? ShiftId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public TerminalSessionStatus Status { get; set; }

    public DateTimeOffset LoggedInOn { get; set; }

    public DateTimeOffset? LoggedOutOn { get; set; }

    public string? MetadataJson { get; set; }
}
