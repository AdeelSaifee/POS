using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Employee : TenantScopedEntity
{
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
}
