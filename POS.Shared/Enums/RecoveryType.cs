namespace POS.Shared.Enums;

public enum RecoveryType
{
    PaymentInitiated = 1,
    PaidNotPrinted = 2,
    PartialTender = 3,
    DrawerOpened = 4,
    SyncInFlight = 5,
    AppCrash = 6
}
