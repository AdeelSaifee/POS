using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Catalog;
using POS.Desktop.Services.Orders;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Orders;

public sealed class OrderServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private readonly int _tenantId = 1;
    private readonly int _locationId = 101;
    private readonly int _terminalId = 999;

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    private PosLocalDbContext CreateDbContext()
    {
        return _dbHarness.CreateProvisionedDbContext(_tenantId, _locationId, _terminalId);
    }

    private async Task SeedItemAsync(
        PosLocalDbContext db,
        int itemId,
        int variantId,
        string name,
        decimal price,
        bool isSellable = true,
        ItemStatus status = ItemStatus.Active,
        bool isDefault = true)
    {
        var uom = await db.LocalUnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == 1);
        if (uom == null)
        {
            uom = new LocalUnitOfMeasure
            {
                Id = 1,
                TenantId = _tenantId,
                Code = "PCS",
                Name = "Pieces"
            };
            db.LocalUnitsOfMeasure.Add(uom);
        }

        var item = new LocalItem
        {
            Id = itemId,
            TenantId = _tenantId,
            ItemCode = $"ITEM{itemId}",
            Name = name,
            Status = status
        };
        db.LocalItems.Add(item);

        var variant = new LocalItemVariant
        {
            Id = variantId,
            TenantId = _tenantId,
            ItemId = itemId,
            VariantCode = $"VAR{variantId}",
            IsDefault = isDefault,
            IsSellable = isSellable,
            Status = status,
            UnitOfMeasureId = 1
        };
        db.LocalItemVariants.Add(variant);

        var priceRow = new LocalItemPrice
        {
            Id = variantId,
            TenantId = _tenantId,
            ItemVariantId = variantId,
            PriceListId = 1,
            UnitPrice = price,
            IsTaxIncluded = true
        };
        db.LocalItemPrices.Add(priceRow);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCartStateAsync_ReturnsEmptyCartState_WhenNoActionsPerformed()
    {
        // Arrange
        using var db = CreateDbContext();
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.GetCartStateAsync();

        // Assert
        Assert.NotNull(state);
        Assert.Empty(state.Lines);
        Assert.Equal(0m, state.SubtotalAmount);
        Assert.Equal(0m, state.DiscountAmount);
        Assert.Equal(0m, state.TaxAmount);
        Assert.Equal(0m, state.TotalAmount);
        Assert.Equal(string.Empty, state.DiscountType);
        Assert.Equal(0m, state.DiscountValue);
    }

    [Fact]
    public async Task AddItemAsync_CreatesLine_WhenItemDoesNotExist()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.AddItemAsync(100, 2);

        // Assert
        Assert.Single(state.Lines);
        var line = state.Lines.First();
        Assert.Equal("100", line.Id);
        Assert.Equal(10, line.ItemId);
        Assert.Equal(100, line.VariantId);
        Assert.Equal("Item A", line.Name);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(150m, line.UnitPrice);
        Assert.Equal(300m, line.GrossAmount);
        Assert.Equal(300m, line.NetAmount);
        Assert.Equal(300m, state.SubtotalAmount);
        Assert.Equal(300m, state.TotalAmount);
    }

    [Fact]
    public async Task AddItemAsync_IncrementsQuantity_WhenItemExists()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        await orderService.AddItemAsync(100, 2);
        var state = await orderService.AddItemAsync(100, 3);

        // Assert
        Assert.Single(state.Lines);
        var line = state.Lines.First();
        Assert.Equal(5, line.Quantity);
        Assert.Equal(750m, line.GrossAmount);
        Assert.Equal(750m, state.SubtotalAmount);
        Assert.Equal(750m, state.TotalAmount);
    }

    [Fact]
    public async Task UpdateLineQuantityAsync_ChangesQuantityAndTotals()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 2);

        // Act
        var state = await orderService.UpdateLineQuantityAsync(100, 5);

        // Assert
        var line = state.Lines.First();
        Assert.Equal(5, line.Quantity);
        Assert.Equal(750m, line.GrossAmount);
        Assert.Equal(750m, state.SubtotalAmount);
        Assert.Equal(750m, state.TotalAmount);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesLineAndResetsTotals()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        await SeedItemAsync(db, 11, 101, "Item B", 200m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 2);
        await orderService.AddItemAsync(101, 1);

        // Act
        var state = await orderService.RemoveItemAsync(100);

        // Assert
        Assert.Single(state.Lines);
        Assert.Equal(101, state.Lines.First().VariantId);
        Assert.Equal(200m, state.SubtotalAmount);
    }

    [Fact]
    public async Task ClearCartAsync_EmptiesLinesAndResetsDiscount()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 2);
        await orderService.ApplyDiscountAsync("pct", 10m);

        // Act
        var state = await orderService.ClearCartAsync();

        // Assert
        Assert.Empty(state.Lines);
        Assert.Equal(0m, state.SubtotalAmount);
        Assert.Equal(string.Empty, state.DiscountType);
        Assert.Equal(0m, state.DiscountValue);
    }

    [Fact]
    public async Task ApplyDiscountAsync_FixedAmount_AppliesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 2); // Subtotal: 300

        // Act
        var state = await orderService.ApplyDiscountAsync("amount", 50m);

        // Assert
        Assert.Equal(50m, state.DiscountAmount);
        Assert.Equal(250m, state.TotalAmount);
        Assert.Equal("amount", state.DiscountType);
        Assert.Equal(50m, state.DiscountValue);
    }

    [Fact]
    public async Task ApplyDiscountAsync_Percentage_AppliesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 3); // Subtotal: 450

        // Act
        var state = await orderService.ApplyDiscountAsync("pct", 15m); // 15% of 450 = 67.50

        // Assert
        Assert.Equal(67.50m, state.DiscountAmount);
        Assert.Equal(382.50m, state.TotalAmount);
        Assert.Equal("pct", state.DiscountType);
        Assert.Equal(15m, state.DiscountValue);
    }

    [Fact]
    public async Task RemoveDiscountAsync_ResetsDiscountAndRecalculates()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 2);
        await orderService.ApplyDiscountAsync("amount", 50m);

        // Act
        var state = await orderService.RemoveDiscountAsync();

        // Assert
        Assert.Equal(0m, state.DiscountAmount);
        Assert.Equal(300m, state.TotalAmount);
        Assert.Equal(string.Empty, state.DiscountType);
        Assert.Equal(0m, state.DiscountValue);
    }

    [Fact]
    public async Task AddItemAsync_RejectsInvalidVariantId()
    {
        // Arrange
        using var db = CreateDbContext();
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(0, 1));
        Assert.Equal("INVALID_VARIANT_ID", ex1.ErrorCode);
        var ex2 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(-5, 1));
        Assert.Equal("INVALID_VARIANT_ID", ex2.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_RejectsUnknownVariant()
    {
        // Arrange
        using var db = CreateDbContext();
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(999, 1));
        Assert.Equal("ITEM_NOT_SELLABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_RejectsNotSellableItem()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m, isSellable: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, 1));
        Assert.Equal("ITEM_NOT_SELLABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_RejectsInactiveItem()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m, status: ItemStatus.Discontinued);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, 1));
        Assert.Equal("ITEM_NOT_SELLABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_RejectsQuantityLessThanOrEqualToZero()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, 0));
        Assert.Equal("INVALID_QUANTITY", ex1.ErrorCode);
        var ex2 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, -1));
        Assert.Equal("INVALID_QUANTITY", ex2.ErrorCode);
    }

    [Fact]
    public async Task AddItemAsync_RejectsExcessiveQuantity()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, 10000));
        Assert.Equal("EXCESSIVE_QUANTITY", ex1.ErrorCode);

        await orderService.AddItemAsync(100, 9990);
        var ex2 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.AddItemAsync(100, 11));
        Assert.Equal("EXCESSIVE_QUANTITY", ex2.ErrorCode);
    }

    [Fact]
    public async Task ApplyDiscountAsync_RejectsEmptyCart()
    {
        // Arrange
        using var db = CreateDbContext();
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("amount", 50m));
        Assert.Equal("EMPTY_CART_DISCOUNT", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyDiscountAsync_RejectsAmountGreaterThanSubtotal()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 1); // Subtotal: 150

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("amount", 151m));
        Assert.Equal("INVALID_DISCOUNT_AMOUNT", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyDiscountAsync_RejectsInvalidPercentage()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 1);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("pct", 0m));
        Assert.Equal("INVALID_DISCOUNT_PERCENT", ex1.ErrorCode);

        var ex2 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("pct", 100.1m));
        Assert.Equal("INVALID_DISCOUNT_PERCENT", ex2.ErrorCode);

        var ex3 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("pct", -5m));
        Assert.Equal("INVALID_DISCOUNT_PERCENT", ex3.ErrorCode);
    }

    [Fact]
    public async Task ApplyDiscountAsync_RejectsInvalidDiscountType()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemAsync(db, 10, 100, "Item A", 150m);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);
        await orderService.AddItemAsync(100, 1);

        // Act & Assert
        var ex1 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("", 10m));
        Assert.Equal("INVALID_DISCOUNT_TYPE", ex1.ErrorCode);

        var ex2 = await Assert.ThrowsAsync<OrderValidationException>(() => orderService.ApplyDiscountAsync("invalid_type", 10m));
        Assert.Equal("INVALID_DISCOUNT_TYPE", ex2.ErrorCode);
    }

    private async Task SeedItemWithTaxAsync(
        PosLocalDbContext db,
        int itemId,
        int variantId,
        string name,
        decimal price,
        int? taxRuleId,
        string? taxCode,
        decimal? taxRate,
        bool isTaxIncluded,
        bool isSellable = true)
    {
        var uom = await db.LocalUnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == 1);
        if (uom == null)
        {
            uom = new LocalUnitOfMeasure
            {
                Id = 1,
                TenantId = _tenantId,
                Code = "PCS",
                Name = "Pieces"
            };
            db.LocalUnitsOfMeasure.Add(uom);
        }

        if (taxRuleId.HasValue)
        {
            var taxRule = await db.LocalTaxRules.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == taxRuleId.Value);
            if (taxRule == null)
            {
                taxRule = new LocalTaxRule
                {
                    Id = taxRuleId.Value,
                    TenantId = _tenantId,
                    Code = taxCode ?? $"TAX{taxRuleId}",
                    Name = $"Tax Rule {taxRuleId}",
                    Rate = taxRate ?? 0m,
                    CalculationMode = isTaxIncluded ? TaxCalculationMode.Inclusive : TaxCalculationMode.Exclusive
                };
                db.LocalTaxRules.Add(taxRule);
            }
        }

        var item = new LocalItem
        {
            Id = itemId,
            TenantId = _tenantId,
            ItemCode = $"ITEM{itemId}",
            Name = name,
            Status = ItemStatus.Active
        };
        db.LocalItems.Add(item);

        var variant = new LocalItemVariant
        {
            Id = variantId,
            TenantId = _tenantId,
            ItemId = itemId,
            VariantCode = $"VAR{variantId}",
            IsDefault = true,
            IsSellable = isSellable,
            Status = ItemStatus.Active,
            UnitOfMeasureId = 1,
            TaxRuleId = taxRuleId
        };
        db.LocalItemVariants.Add(variant);

        var priceRow = new LocalItemPrice
        {
            Id = variantId,
            TenantId = _tenantId,
            ItemVariantId = variantId,
            PriceListId = 1,
            UnitPrice = price,
            IsTaxIncluded = isTaxIncluded
        };
        db.LocalItemPrices.Add(priceRow);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateTotals_NoTaxItem_CalculatesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item No Tax", 150m, taxRuleId: null, taxCode: null, taxRate: null, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.AddItemAsync(100, 2);

        // Assert
        Assert.Single(state.Lines);
        var line = state.Lines.First();
        Assert.Equal(300m, line.GrossAmount);
        Assert.Equal(0m, line.DiscountAmount);
        Assert.Equal(0m, line.TaxAmount);
        Assert.Equal(300m, line.NetAmount);

        Assert.Equal(300m, state.SubtotalAmount);
        Assert.Equal(0m, state.DiscountAmount);
        Assert.Equal(0m, state.TaxAmount);
        Assert.Equal(300m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_FivePercentTaxExclusive_CalculatesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 5% Excl", 100m, taxRuleId: 1, taxCode: "VAT5", taxRate: 5m, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.AddItemAsync(100, 2);

        // Assert
        var line = state.Lines.First();
        Assert.Equal(200m, line.GrossAmount);
        Assert.Equal(0m, line.DiscountAmount);
        Assert.Equal(10m, line.TaxAmount);
        Assert.Equal(210m, line.NetAmount);

        Assert.Equal(200m, state.SubtotalAmount);
        Assert.Equal(0m, state.DiscountAmount);
        Assert.Equal(10m, state.TaxAmount);
        Assert.Equal(210m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_EighteenPercentTaxExclusive_CalculatesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 18% Excl", 150m, taxRuleId: 2, taxCode: "VAT18", taxRate: 18m, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.AddItemAsync(100, 1);

        // Assert
        var line = state.Lines.First();
        Assert.Equal(150m, line.GrossAmount);
        Assert.Equal(27m, line.TaxAmount);
        Assert.Equal(177m, line.NetAmount);

        Assert.Equal(150m, state.SubtotalAmount);
        Assert.Equal(27m, state.TaxAmount);
        Assert.Equal(177m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_TaxIncluded_CalculatesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 10% Incl", 110m, taxRuleId: 3, taxCode: "VAT10I", taxRate: 10m, isTaxIncluded: true);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        var state = await orderService.AddItemAsync(100, 1);

        // Assert
        var line = state.Lines.First();
        Assert.Equal(110m, line.GrossAmount);
        Assert.Equal(10m, line.TaxAmount);
        Assert.Equal(110m, line.NetAmount);

        Assert.Equal(110m, state.SubtotalAmount);
        Assert.Equal(10m, state.TaxAmount);
        Assert.Equal(110m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_MixedTaxRates_CalculatesCorrectly()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 5% Excl", 100m, taxRuleId: 1, taxCode: "VAT5", taxRate: 5m, isTaxIncluded: false);
        await SeedItemWithTaxAsync(db, 11, 101, "Item 10% Incl", 110m, taxRuleId: 3, taxCode: "VAT10I", taxRate: 10m, isTaxIncluded: true);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        // Act
        await orderService.AddItemAsync(100, 1);
        var state = await orderService.AddItemAsync(101, 1);

        // Assert
        Assert.Equal(210m, state.SubtotalAmount);

        var line1 = state.Lines.First(l => l.VariantId == 100);
        Assert.Equal(100m, line1.GrossAmount);
        Assert.Equal(5m, line1.TaxAmount);
        Assert.Equal(105m, line1.NetAmount);

        var line2 = state.Lines.First(l => l.VariantId == 101);
        Assert.Equal(110m, line2.GrossAmount);
        Assert.Equal(10m, line2.TaxAmount);
        Assert.Equal(110m, line2.NetAmount);

        Assert.Equal(15m, state.TaxAmount);
        Assert.Equal(215m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_FixedAmountDiscount_DistributesProportiallyBeforeTax()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 1", 100m, taxRuleId: 1, taxCode: "VAT10", taxRate: 10m, isTaxIncluded: false);
        await SeedItemWithTaxAsync(db, 11, 101, "Item 2", 200m, taxRuleId: 2, taxCode: "VAT20", taxRate: 20m, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        await orderService.AddItemAsync(100, 1);
        await orderService.AddItemAsync(101, 1);

        // Act
        var state = await orderService.ApplyDiscountAsync("amount", 30m);

        // Assert
        var line1 = state.Lines.First(l => l.VariantId == 100);
        Assert.Equal(10m, line1.DiscountAmount);
        Assert.Equal(9m, line1.TaxAmount);
        Assert.Equal(99m, line1.NetAmount);

        var line2 = state.Lines.First(l => l.VariantId == 101);
        Assert.Equal(20m, line2.DiscountAmount);
        Assert.Equal(36m, line2.TaxAmount);
        Assert.Equal(216m, line2.NetAmount);

        Assert.Equal(300m, state.SubtotalAmount);
        Assert.Equal(30m, state.DiscountAmount);
        Assert.Equal(45m, state.TaxAmount);
        Assert.Equal(315m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_PercentageDiscount_DistributesProportiallyBeforeTax()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 1", 100m, taxRuleId: 1, taxCode: "VAT10", taxRate: 10m, isTaxIncluded: false);
        await SeedItemWithTaxAsync(db, 11, 101, "Item 2", 200m, taxRuleId: 2, taxCode: "VAT20", taxRate: 20m, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        await orderService.AddItemAsync(100, 1);
        await orderService.AddItemAsync(101, 1);

        // Act
        var state = await orderService.ApplyDiscountAsync("pct", 10m);

        // Assert
        var line1 = state.Lines.First(l => l.VariantId == 100);
        Assert.Equal(10m, line1.DiscountAmount);
        Assert.Equal(9m, line1.TaxAmount);
        Assert.Equal(99m, line1.NetAmount);

        var line2 = state.Lines.First(l => l.VariantId == 101);
        Assert.Equal(20m, line2.DiscountAmount);
        Assert.Equal(36m, line2.TaxAmount);
        Assert.Equal(216m, line2.NetAmount);

        Assert.Equal(300m, state.SubtotalAmount);
        Assert.Equal(30m, state.DiscountAmount);
        Assert.Equal(45m, state.TaxAmount);
        Assert.Equal(315m, state.TotalAmount);
    }

    [Fact]
    public async Task RecalculateTotals_DiscountDistributionRoundingRemainder_IsAbsorbedByLastLine()
    {
        // Arrange
        using var db = CreateDbContext();
        await SeedItemWithTaxAsync(db, 10, 100, "Item 1", 100m, taxRuleId: null, taxCode: null, taxRate: null, isTaxIncluded: false);
        await SeedItemWithTaxAsync(db, 11, 101, "Item 2", 100m, taxRuleId: null, taxCode: null, taxRate: null, isTaxIncluded: false);
        await SeedItemWithTaxAsync(db, 12, 102, "Item 3", 100m, taxRuleId: null, taxCode: null, taxRate: null, isTaxIncluded: false);
        var store = new DraftCartStore();
        var catalogService = new CatalogService(db);
        var orderService = new OrderService(store, catalogService);

        await orderService.AddItemAsync(100, 1);
        await orderService.AddItemAsync(101, 1);
        await orderService.AddItemAsync(102, 1);

        // Act
        var state = await orderService.ApplyDiscountAsync("amount", 10m);

        // Assert
        var line1 = state.Lines.First(l => l.VariantId == 100);
        Assert.Equal(3.33m, line1.DiscountAmount);

        var line2 = state.Lines.First(l => l.VariantId == 101);
        Assert.Equal(3.33m, line2.DiscountAmount);

        var line3 = state.Lines.First(l => l.VariantId == 102);
        Assert.Equal(3.34m, line3.DiscountAmount);

        Assert.Equal(10m, state.DiscountAmount);
        Assert.Equal(290m, state.TotalAmount);
    }
}
