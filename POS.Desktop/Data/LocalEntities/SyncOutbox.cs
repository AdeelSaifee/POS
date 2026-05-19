using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class SyncOutbox : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid EventId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public long? ChunkSequence { get; set; }

    public SyncOutboxStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptOn { get; set; }

    public DateTimeOffset? AckedOn { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset? RetainUntil { get; set; }
}
