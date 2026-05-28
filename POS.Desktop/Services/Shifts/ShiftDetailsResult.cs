using System;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Represents the safe details of a shift query.
/// </summary>
public sealed record ShiftDetailsResult(
    bool IsOpen,
    Guid? ShiftId = null,
    DateOnly? BusinessDate = null,
    decimal? OpeningFloat = null,
    string? Status = null);
