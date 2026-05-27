namespace POS.Desktop.Services.Catalog;

/// <summary>
/// UI-friendly representation of a sellable catalog item with its default variant,
/// primary identifier, price, unit of measure, and tax rule flattened into one record.
/// </summary>
public sealed record CatalogItemDto
{
    // ---- Item ----
    public int ItemId { get; init; }
    public string ItemCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    // ---- Category (nullable - items may have no category) ----
    public int? CategoryId { get; init; }
    public string? CategoryCode { get; init; }
    public string? CategoryName { get; init; }

    // ---- Default variant ----
    public int VariantId { get; init; }
    public string VariantCode { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public bool IsSellable { get; init; }

    // ---- Primary identifier (nullable - variant may have no identifier yet) ----
    public string? IdentifierValue { get; init; }

    // ---- Price (from default price list) ----
    public decimal UnitPrice { get; init; }
    public bool IsTaxIncluded { get; init; }

    // ---- Tax rule (nullable - variant may have no tax rule) ----
    public int? TaxRuleId { get; init; }
    public string? TaxCode { get; init; }
    public decimal? TaxRate { get; init; }

    // ---- Unit of measure ----
    public int UnitOfMeasureId { get; init; }
    public string UnitCode { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
}
