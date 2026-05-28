namespace POS.Desktop.Services.Orders;

/// <summary>
/// Represents a single item line in the draft cart/order.
/// </summary>
public sealed record CartLineDto
{
    /// <summary>
    /// Unique identifier for the cart line, matching JS line ID (e.g. string representation of VariantId).
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The ID of the item.
    /// </summary>
    public int ItemId { get; init; }

    /// <summary>
    /// The ID of the item variant.
    /// </summary>
    public int VariantId { get; init; }

    /// <summary>
    /// The name of the item.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The quantity of the item in the cart.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// The unit price of the item.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// The gross amount (Quantity * UnitPrice) before discounts and tax.
    /// </summary>
    public decimal GrossAmount { get; init; }

    /// <summary>
    /// The discount amount applied to this line.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// The tax amount calculated for this line.
    /// </summary>
    public decimal TaxAmount { get; init; }

    /// <summary>
    /// The net amount (GrossAmount - DiscountAmount + TaxAmount) for this line.
    /// </summary>
    public decimal NetAmount { get; init; }

    /// <summary>
    /// The unit of measure code.
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// The category code for classification.
    /// </summary>
    public string CategoryCode { get; init; } = string.Empty;
}
