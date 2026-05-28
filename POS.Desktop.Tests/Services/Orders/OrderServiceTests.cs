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
}
