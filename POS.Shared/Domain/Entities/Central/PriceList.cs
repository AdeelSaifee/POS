using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class PriceList : TenantScopedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PriceListType PriceListType { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public long PriceListVersion { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int Priority { get; set; }

    public bool IsDefault { get; set; }
}
