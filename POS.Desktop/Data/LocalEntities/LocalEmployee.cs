using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents the local read-model for an Employee.
/// </summary>
public class LocalEmployee
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? PinHash { get; set; }

    public string? PinSalt { get; set; }

    public string? PinHashAlgorithm { get; set; }

    public EmployeeStatus Status { get; set; }

    public bool MustChangePin { get; set; }

    public bool IsActive { get; set; }
}
