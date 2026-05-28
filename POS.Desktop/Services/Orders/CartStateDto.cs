using System;
using System.Collections.Generic;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Represents the authoritative in-memory state of the draft cart/order.
/// </summary>
public sealed record CartStateDto
{
    /// <summary>
    /// The sum of all line item gross amounts.
    /// </summary>
    public decimal SubtotalAmount { get; init; }

    /// <summary>
    /// The total discount amount applied to the cart.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// The total tax amount calculated for the cart.
    /// </summary>
    public decimal TaxAmount { get; init; }

    /// <summary>
    /// The final grand total (Subtotal - Discount + Tax) to be paid.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// The list of items currently in the cart.
    /// </summary>
    public IReadOnlyList<CartLineDto> Lines { get; init; } = Array.Empty<CartLineDto>();

    /// <summary>
    /// The type of discount applied (e.g. "pct" or "amount").
    /// </summary>
    public string DiscountType { get; init; } = string.Empty;

    /// <summary>
    /// The raw discount value/rate applied.
    /// </summary>
    public decimal DiscountValue { get; init; }
}
