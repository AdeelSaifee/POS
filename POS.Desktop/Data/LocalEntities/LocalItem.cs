using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalItem : LocalCatalogEntity
{
    public int? CategoryId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ItemType ItemType { get; set; }

    public bool IsTrackedInventory { get; set; }

    public int DefaultUnitOfMeasureId { get; set; }

    public int? DefaultTaxRuleId { get; set; }

    public ItemStatus Status { get; set; }

    public long CatalogVersion { get; set; }
}
