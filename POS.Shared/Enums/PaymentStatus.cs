namespace POS.Shared.Enums;

public enum PaymentStatus
{
    Initiated = 1,
    Authorized = 2,
    Captured = 3,
    Settled = 4,
    Failed = 5,
    Cancelled = 6,
    ReconciliationRequired = 7,
    Refunded = 8,
    Reversed = 9,
    Unpaid = 10,
    Partial = 11,
    Paid = 12
}
