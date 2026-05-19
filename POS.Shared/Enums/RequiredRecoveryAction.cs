namespace POS.Shared.Enums;

public enum RequiredRecoveryAction
{
    ReconcilePayment = 1,
    ReprintReceipt = 2,
    CompleteTender = 3,
    LogDrawer = 4,
    RetrySync = 5
}
