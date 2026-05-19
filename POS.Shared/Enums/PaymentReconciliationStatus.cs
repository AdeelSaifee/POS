namespace POS.Shared.Enums;

public enum PaymentReconciliationStatus
{
    Pending = 1,
    Checking = 2,
    ResolvedCaptured = 3,
    ResolvedFailed = 4,
    Escalated = 5
}
