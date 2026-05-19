using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class ItemPrice : TenantScopedEntity
{
    public int PriceListId { get; set; }

    public int ItemVariantId { get; set; }

    public int UnitOfMeasureId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public decimal MinimumQuantity { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public bool IsTaxIncluded { get; set; }
}
