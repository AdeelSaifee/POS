using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalRetentionState : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public string Category { get; set; } = string.Empty;

    public int RetentionDays { get; set; }

    public DateTimeOffset? LastCleanupOn { get; set; }

    public DateOnly? OldestRetainedBusinessDate { get; set; }

    public LocalRetentionStatus Status { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }
}
