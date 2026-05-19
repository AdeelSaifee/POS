namespace POS.Shared.Enums;

public enum OrderStatus
{
    Draft = 1,
    Suspended = 2,
    Completed = 3,
    Voided = 4,
    Refunded = 5,
    PartiallyRefunded = 6,
    Cancelled = 7,
    RecoveryRequired = 8
}
