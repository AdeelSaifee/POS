using System.Collections.Generic;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Typed options for shift-open policy limits and pre-shift checklist items.
/// Bound from the "ShiftOpen" configuration section.
/// </summary>
public sealed class ShiftOpenPolicyOptions
{
    public const int MaxChecklistItems = 10;
    public const decimal DefaultCashDrawerLimit = 25000m;
    public const decimal DefaultAutoSafeDropThreshold = 20000m;

    public static IReadOnlyList<string> DefaultChecklist() =>
    [
        "Physical cash counted & verified",
        "Barcode scanner powered on",
        "Receipt printer has paper loaded",
        "Card terminal connected & tested",
        "Product catalog synced & up to date"
    ];

    public decimal CashDrawerLimit { get; set; } = DefaultCashDrawerLimit;
    public decimal AutoSafeDropThreshold { get; set; } = DefaultAutoSafeDropThreshold;
    public List<string> Checklist { get; set; } = [..DefaultChecklist()];
}
