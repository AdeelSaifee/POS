using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalRecoveryJournal : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? PaymentId { get; set; }

    public RecoveryType RecoveryType { get; set; }

    public RecoveryJournalStatus Status { get; set; }

    public string StatePayloadJson { get; set; } = string.Empty;

    public RequiredRecoveryAction RequiredAction { get; set; }

    public int? ResolvedByEmployeeId { get; set; }

    public DateTimeOffset? ResolvedOn { get; set; }

    public string? ResolutionComment { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset? RetainUntil { get; set; }
}
