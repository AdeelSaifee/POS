using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class OrderLine : AppendOnlyEntity
{
    public Guid OrderId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public int? ItemId { get; set; }

    public int? ItemVariantId { get; set; }

    public Guid? OriginalOrderLineId { get; set; }

    public int? ReasonCodeId { get; set; }

    public int? AuthorizedByEmployeeId { get; set; }

    public int LineNumber { get; set; }

    public string LineType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? SKU { get; set; }

    public string? Barcode { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal NetAmount { get; set; }

    public int? TaxRuleId { get; set; }

    public decimal? TaxRate { get; set; }

    public int? PriceListId { get; set; }

    public long CatalogVersion { get; set; }

    public string? MetadataJson { get; set; }
}
