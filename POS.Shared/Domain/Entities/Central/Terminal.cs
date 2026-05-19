using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Terminal : TenantScopedEntity
{
    public int LocationId { get; set; }

    public string TerminalCode { get; set; } = string.Empty;

    public Guid DeviceId { get; set; }

    public string DeviceSecretHash { get; set; } = string.Empty;

    public TerminalProvisioningStatus ProvisioningStatus { get; set; }

    public DateTimeOffset? LastSeenOn { get; set; }

    public long? LastCatalogVersion { get; set; }

    public int? LastPriceListId { get; set; }

    public long? LastRuleVersion { get; set; }

    public int? LastReceiptTemplateId { get; set; }
}
