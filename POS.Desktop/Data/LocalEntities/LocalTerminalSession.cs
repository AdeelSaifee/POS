using System;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents a locally persisted terminal login session, aligned with central TerminalSession.
/// </summary>
public class LocalTerminalSession
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public Guid? ShiftId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public TerminalSessionStatus Status { get; set; }

    public DateTimeOffset LoggedInOn { get; set; }

    public DateTimeOffset? LoggedOutOn { get; set; }

    public string? MetadataJson { get; set; }
}
