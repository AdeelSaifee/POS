namespace POS.Shared.Enums;

public enum SyncCursorStatus
{
    Active = 1,
    Backoff = 2,
    Failed = 3,
    RebuildRequired = 4
}
