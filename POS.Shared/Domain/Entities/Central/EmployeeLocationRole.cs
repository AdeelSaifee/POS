using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class EmployeeLocationRole : TenantScopedEntity
{
    public int EmployeeId { get; set; }

    public int? LocationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? PermissionSetCode { get; set; }

    public DateTimeOffset? StartsOn { get; set; }

    public DateTimeOffset? EndsOn { get; set; }
}
