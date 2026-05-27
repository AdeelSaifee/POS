using System;

namespace POS.Desktop.Data.LocalEntities;

public class LocalItemPrice : LocalCatalogEntity
{
    public int PriceListId { get; set; }

    public int ItemVariantId { get; set; }

    public int UnitOfMeasureId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public bool IsTaxIncluded { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }
}
