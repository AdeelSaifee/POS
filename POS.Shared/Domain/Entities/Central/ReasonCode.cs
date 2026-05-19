using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class ReasonCode : TenantScopedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ReasonCategory { get; set; } = string.Empty;

    public bool RequiresManagerApproval { get; set; }

    public bool RequiresComment { get; set; }

    public int SortOrder { get; set; }
}
