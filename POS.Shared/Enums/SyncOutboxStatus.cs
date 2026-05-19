namespace POS.Shared.Enums;

public enum SyncOutboxStatus
{
    Pending = 1,
    InFlight = 2,
    Acked = 3,
    Failed = 4,
    DeadLetter = 5
}
