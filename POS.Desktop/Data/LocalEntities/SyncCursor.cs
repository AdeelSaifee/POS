using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class SyncCursor : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public string StreamName { get; set; } = string.Empty;

    public string? LastPullToken { get; set; }

    public DateTimeOffset? LastSuccessfulPullOn { get; set; }

    public long? LastPushedChunkSequence { get; set; }

    public long? LastAckedChunkSequence { get; set; }

    public DateTimeOffset? ServerBackoffUntil { get; set; }

    public SyncCursorStatus Status { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }
}
