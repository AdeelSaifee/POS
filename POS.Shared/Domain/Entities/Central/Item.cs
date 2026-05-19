using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Item : TenantScopedEntity
{
    public int? CategoryId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? BrandName { get; set; }

    public string? ManufacturerName { get; set; }

    public ItemType ItemType { get; set; }

    public bool IsTrackedInventory { get; set; }

    public int DefaultUnitOfMeasureId { get; set; }

    public int? DefaultTaxRuleId { get; set; }

    public long CatalogVersion { get; set; }

    public ItemStatus Status { get; set; }

    public string? MetadataJson { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
