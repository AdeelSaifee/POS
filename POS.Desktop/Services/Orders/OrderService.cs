using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Desktop.Services.Catalog;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Scoped service implementing business logic for managing the in-memory draft cart.
/// </summary>
public sealed class OrderService : IOrderService
{
    private const int MaxQuantityLimit = 9999;
    private readonly IDraftCartStore _store;
    private readonly ICatalogService _catalogService;

    public OrderService(IDraftCartStore store, ICatalogService catalogService)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
    }

    /// <inheritdoc />
    public Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.GetState());
    }

    /// <inheritdoc />
    public async Task<CartStateDto> AddItemAsync(int variantId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        if (variantId <= 0)
        {
            throw new OrderValidationException("Variant ID must be greater than zero.", "INVALID_VARIANT_ID");
        }

        if (quantity <= 0)
        {
            throw new OrderValidationException("Quantity must be greater than zero.", "INVALID_QUANTITY");
        }

        var item = await _catalogService.FindByVariantIdAsync(variantId, cancellationToken);
        if (item == null || !item.IsSellable || item.Status != "Active")
        {
            throw new OrderValidationException("Item not found or is not sellable.", "ITEM_NOT_SELLABLE");
        }

        var newState = _store.Update(currentState =>
        {
            var lines = currentState.Lines.ToList();
            var existingLine = lines.FirstOrDefault(l => l.VariantId == variantId);

            if (existingLine != null)
            {
                int newQuantity = existingLine.Quantity + quantity;
                if (newQuantity > MaxQuantityLimit)
                {
                    throw new OrderValidationException($"Quantity exceeds the maximum limit of {MaxQuantityLimit} per item.", "EXCESSIVE_QUANTITY");
                }

                var updatedLine = existingLine with
                {
                    Quantity = newQuantity,
                    GrossAmount = newQuantity * existingLine.UnitPrice,
                    NetAmount = newQuantity * existingLine.UnitPrice
                };

                var index = lines.IndexOf(existingLine);
                lines[index] = updatedLine;
            }
            else
            {
                if (quantity > MaxQuantityLimit)
                {
                    throw new OrderValidationException($"Quantity exceeds the maximum limit of {MaxQuantityLimit} per item.", "EXCESSIVE_QUANTITY");
                }

                var newLine = new CartLineDto
                {
                    Id = variantId.ToString(),
                    ItemId = item.ItemId,
                    VariantId = variantId,
                    Name = item.ItemName,
                    Quantity = quantity,
                    UnitPrice = item.UnitPrice,
                    GrossAmount = quantity * item.UnitPrice,
                    DiscountAmount = 0m,
                    TaxAmount = 0m,
                    NetAmount = quantity * item.UnitPrice,
                    Unit = item.UnitCode,
                    CategoryCode = item.CategoryCode ?? string.Empty,
                    TaxRuleId = item.TaxRuleId,
                    TaxCode = item.TaxCode,
                    TaxRate = item.TaxRate,
                    IsTaxIncluded = item.IsTaxIncluded
                };
                lines.Add(newLine);
            }

            return RecalculateTotals(lines, currentState.DiscountType, currentState.DiscountValue);
        });

        return newState;
    }

    /// <inheritdoc />
    public Task<CartStateDto> UpdateLineQuantityAsync(int variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (variantId <= 0)
        {
            throw new OrderValidationException("Variant ID must be greater than zero.", "INVALID_VARIANT_ID");
        }

        if (quantity <= 0)
        {
            throw new OrderValidationException("Quantity must be greater than zero.", "INVALID_QUANTITY");
        }

        if (quantity > MaxQuantityLimit)
        {
            throw new OrderValidationException($"Quantity exceeds the maximum limit of {MaxQuantityLimit} per item.", "EXCESSIVE_QUANTITY");
        }

        var newState = _store.Update(currentState =>
        {
            var lines = currentState.Lines.ToList();
            var existingLine = lines.FirstOrDefault(l => l.VariantId == variantId);

            if (existingLine == null)
            {
                throw new OrderValidationException("Item is not in the cart.", "ITEM_NOT_IN_CART");
            }

            var updatedLine = existingLine with
            {
                Quantity = quantity,
                GrossAmount = quantity * existingLine.UnitPrice,
                NetAmount = quantity * existingLine.UnitPrice
            };

            var index = lines.IndexOf(existingLine);
            lines[index] = updatedLine;

            return RecalculateTotals(lines, currentState.DiscountType, currentState.DiscountValue);
        });

        return Task.FromResult(newState);
    }

    /// <inheritdoc />
    public Task<CartStateDto> RemoveItemAsync(int variantId, CancellationToken cancellationToken = default)
    {
        if (variantId <= 0)
        {
            throw new OrderValidationException("Variant ID must be greater than zero.", "INVALID_VARIANT_ID");
        }

        var newState = _store.Update(currentState =>
        {
            var lines = currentState.Lines.ToList();
            var existingLine = lines.FirstOrDefault(l => l.VariantId == variantId);

            if (existingLine == null)
            {
                throw new OrderValidationException("Item is not in the cart.", "ITEM_NOT_IN_CART");
            }

            lines.Remove(existingLine);

            // If cart is now empty, reset discount
            var discountType = lines.Count == 0 ? string.Empty : currentState.DiscountType;
            var discountValue = lines.Count == 0 ? 0m : currentState.DiscountValue;

            return RecalculateTotals(lines, discountType, discountValue);
        });

        return Task.FromResult(newState);
    }

    /// <inheritdoc />
    public Task<CartStateDto> ClearCartAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.FromResult(new CartStateDto());
    }

    /// <inheritdoc />
    public Task<CartStateDto> ApplyDiscountAsync(string discountType, decimal discountValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discountType))
        {
            throw new OrderValidationException("Discount type cannot be null or empty.", "INVALID_DISCOUNT_TYPE");
        }

        if (discountType != "amount" && discountType != "pct")
        {
            throw new OrderValidationException("Invalid discount type. Supported types: 'amount' and 'pct'.", "INVALID_DISCOUNT_TYPE");
        }

        if (discountValue < 0)
        {
            if (discountType == "pct")
            {
                throw new OrderValidationException("Percentage discount must be between 0 and 100.", "INVALID_DISCOUNT_PERCENT");
            }
            throw new OrderValidationException("Discount amount must be greater than zero.", "INVALID_DISCOUNT_AMOUNT");
        }

        var newState = _store.Update(currentState =>
        {
            if (currentState.Lines.Count == 0)
            {
                throw new OrderValidationException("Cannot apply discount to an empty cart.", "EMPTY_CART_DISCOUNT");
            }

            decimal subtotal = currentState.Lines.Sum(l => MoneyRounder.Round(l.Quantity * l.UnitPrice));

            if (discountType == "amount")
            {
                if (discountValue <= 0)
                {
                    throw new OrderValidationException("Discount amount must be greater than zero.", "INVALID_DISCOUNT_AMOUNT");
                }
                if (discountValue > subtotal)
                {
                    throw new OrderValidationException("Discount amount cannot exceed the cart subtotal.", "INVALID_DISCOUNT_AMOUNT");
                }
            }
            else if (discountType == "pct")
            {
                if (discountValue <= 0 || discountValue > 100)
                {
                    throw new OrderValidationException("Percentage discount must be between 0 and 100.", "INVALID_DISCOUNT_PERCENT");
                }
            }

            return RecalculateTotals(currentState.Lines, discountType, discountValue);
        });

        return Task.FromResult(newState);
    }

    /// <inheritdoc />
    public Task<CartStateDto> RemoveDiscountAsync(CancellationToken cancellationToken = default)
    {
        var newState = _store.Update(currentState =>
        {
            return RecalculateTotals(currentState.Lines, string.Empty, 0m);
        });

        return Task.FromResult(newState);
    }

    private static CartStateDto RecalculateTotals(
        IReadOnlyList<CartLineDto> lines,
        string discountType,
        decimal discountValue)
    {
        if (lines.Count == 0)
        {
            return new CartStateDto
            {
                SubtotalAmount = 0m,
                DiscountAmount = 0m,
                TaxAmount = 0m,
                TotalAmount = 0m,
                Lines = Array.Empty<CartLineDto>(),
                DiscountType = string.Empty,
                DiscountValue = 0m
            };
        }

        // 1. Calculate Gross Amounts and Subtotal
        decimal subtotalAmount = 0m;
        var linesWithGross = new List<(CartLineDto Line, decimal Gross)>();
        foreach (var line in lines)
        {
            decimal gross = MoneyRounder.Round(line.Quantity * line.UnitPrice);
            subtotalAmount += gross;
            linesWithGross.Add((line, gross));
        }

        // 2. Calculate Cart-Level Discount
        decimal cartDiscount = 0m;
        if (discountType == "pct")
        {
            cartDiscount = MoneyRounder.Round(subtotalAmount * discountValue / 100m);
        }
        else if (discountType == "amount")
        {
            cartDiscount = MoneyRounder.Round(discountValue);
        }

        // Clamp discount to subtotal
        cartDiscount = Math.Min(cartDiscount, subtotalAmount);

        // 3. Proportional Discount Distribution & Line calculations
        var updatedLines = new List<CartLineDto>();
        decimal allocatedDiscountSum = 0m;

        for (int i = 0; i < linesWithGross.Count; i++)
        {
            var (line, grossAmount) = linesWithGross[i];

            decimal lineDiscount = 0m;
            if (cartDiscount > 0m && subtotalAmount > 0m)
            {
                if (i == linesWithGross.Count - 1)
                {
                    // Last line absorbs the remainder
                    lineDiscount = MoneyRounder.Round(cartDiscount - allocatedDiscountSum);
                }
                else
                {
                    decimal share = (grossAmount / subtotalAmount) * cartDiscount;
                    lineDiscount = MoneyRounder.Round(share);
                    allocatedDiscountSum += lineDiscount;
                }
            }

            decimal taxableBase = MoneyRounder.Round(grossAmount - lineDiscount);

            decimal taxRate = line.TaxRate ?? 0m;
            decimal taxAmount = 0m;
            decimal netAmount = 0m;

            if (taxRate <= 0m)
            {
                taxAmount = 0m;
                netAmount = taxableBase;
            }
            else if (line.IsTaxIncluded)
            {
                // Tax-inclusive
                decimal calculatedTax = taxableBase - (taxableBase / (1m + taxRate / 100m));
                taxAmount = MoneyRounder.Round(calculatedTax);
                netAmount = taxableBase; // Net amount equals taxable base (inclusive of tax)
            }
            else
            {
                // Tax-exclusive
                decimal calculatedTax = taxableBase * (taxRate / 100m);
                taxAmount = MoneyRounder.Round(calculatedTax);
                netAmount = MoneyRounder.Round(taxableBase + taxAmount);
            }

            updatedLines.Add(line with
            {
                GrossAmount = grossAmount,
                DiscountAmount = lineDiscount,
                TaxAmount = taxAmount,
                NetAmount = netAmount
            });
        }

        // 4. Calculate final cart-level totals
        decimal totalTaxAmount = 0m;
        decimal totalNetAmount = 0m;
        foreach (var uLine in updatedLines)
        {
            totalTaxAmount += uLine.TaxAmount;
            totalNetAmount += uLine.NetAmount;
        }

        return new CartStateDto
        {
            SubtotalAmount = subtotalAmount,
            DiscountAmount = cartDiscount,
            TaxAmount = MoneyRounder.Round(totalTaxAmount),
            TotalAmount = MoneyRounder.Round(totalNetAmount),
            Lines = updatedLines,
            DiscountType = discountType,
            DiscountValue = discountValue
        };
    }
}
