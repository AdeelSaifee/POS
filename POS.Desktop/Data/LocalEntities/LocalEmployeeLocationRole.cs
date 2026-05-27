using System;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents the local read-model mapping an Employee to their locations, roles, and permissions.
/// </summary>
public class LocalEmployeeLocationRole
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int EmployeeId { get; set; }

    public int? LocationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? PermissionSetCode { get; set; }

    public DateTime? StartsOn { get; set; }

    public DateTime? EndsOn { get; set; }

    public bool IsActive { get; set; }
}
