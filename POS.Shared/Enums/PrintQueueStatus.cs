namespace POS.Shared.Enums;

public enum PrintQueueStatus
{
    Pending = 1,
    Printing = 2,
    Printed = 3,
    Failed = 4,
    Cancelled = 5,
    ManualHandled = 6
}
