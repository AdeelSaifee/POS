using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class ItemVariant : TenantScopedEntity
{
    public int ItemId { get; set; }

    public string VariantCode { get; set; } = string.Empty;

    public string? SKU { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? SizeText { get; set; }

    public decimal? WeightValue { get; set; }

    public int? WeightUnitOfMeasureId { get; set; }

    public int UnitOfMeasureId { get; set; }

    public int? TaxRuleId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsSellable { get; set; }

    public bool IsPurchasable { get; set; }

    public long CatalogVersion { get; set; }

    public ItemStatus Status { get; set; }

    public string? MetadataJson { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
