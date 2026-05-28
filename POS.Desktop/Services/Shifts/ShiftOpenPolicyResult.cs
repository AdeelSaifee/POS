using System.Collections.Generic;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Safe, sanitised policy values returned by <see cref="IShiftService.GetOpenPolicyAsync"/>.
/// Always contains valid defaults — callers never need to null-check members.
/// </summary>
public sealed record ShiftOpenPolicyResult(
    decimal CashDrawerLimit,
    decimal AutoSafeDropThreshold,
    IReadOnlyList<string> Checklist);
