using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class TenderMethod : TenantScopedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TenderType { get; set; } = string.Empty;

    public bool RequiresExternalReference { get; set; }

    public bool AllowsChange { get; set; }

    public bool AllowsRefund { get; set; }

    public bool RequiresOnlineAuthorization { get; set; }

    public int SortOrder { get; set; }
}
