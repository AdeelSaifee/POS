using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Represents the result of a shift open operation.
/// </summary>
public sealed record ShiftOpenResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    LocalShift? Shift = null);
