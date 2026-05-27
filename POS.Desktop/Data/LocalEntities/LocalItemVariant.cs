using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalItemVariant : LocalCatalogEntity
{
    public int ItemId { get; set; }

    public string VariantCode { get; set; } = string.Empty;

    public string? SKU { get; set; }

    public string Name { get; set; } = string.Empty;

    public int UnitOfMeasureId { get; set; }

    public int? TaxRuleId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsSellable { get; set; }

    public ItemStatus Status { get; set; }

    public long CatalogVersion { get; set; }
}
