namespace POS.Shared.Enums;

public enum InventoryExceptionStatus
{
    None = 1,
    NegativeStock = 2,
    ReconciliationRequired = 3,
    BlockedItem = 4,
    UnitMismatch = 5
}
