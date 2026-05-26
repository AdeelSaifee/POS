using System;

namespace POS.Desktop.Services.Session;

/// <summary>
/// Represents a safe, non-sensitive snapshot of the current operator session.
/// </summary>
/// <param name="OperatorId">The unique identifier of the operator.</param>
/// <param name="DisplayName">The display name of the operator.</param>
/// <param name="Role">The primary role of the operator in this session.</param>
/// <param name="LoginTime">The timestamp when the session was established.</param>
/// <param name="TerminalId">The identifier of the terminal where the session is active.</param>
/// <param name="SessionId">A unique identifier for this specific session instance.</param>
public sealed record OperatorSession(
    string OperatorId,
    string DisplayName,
    string Role,
    DateTimeOffset LoginTime,
    string? TerminalId = null,
    string? SessionId = null);
