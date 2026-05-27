using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Data.Seeding;
using POS.Desktop.Services.Catalog;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Catalog;

/// <summary>
/// Integration tests for CatalogService reading from a seeded in-memory SQLite database.
/// Proves tenant scoping via global query filters - no IgnoreQueryFilters() used in service reads.
/// Each test class instance gets its own in-memory connection (xUnit creates one instance per test).
/// </summary>
public sealed class CatalogServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public CatalogServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Dispose();

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private PosLocalDbContext CreateDbContext(IProvisionedTerminalContext terminalContext)
    {
        var db = new PosLocalDbContext(_options, terminalContext);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Seeds catalog for the given tenant. Uses NoProvisionedTerminalContext so the seeder
    /// can write with IgnoreQueryFilters while the read DbContexts use the provisioned filter.</summary>
    private async Task SeedForTenantAsync(int tenantId)
    {
        using var db = CreateDbContext(new NoProvisionedTerminalContext());
        await new LocalCatalogSeeder(db).SeedAsync(tenantId);
    }

    /// <summary>Creates a CatalogService whose DbContext is tenant-scoped to the given tenant.</summary>
    private CatalogService CreateProvisionedService(int tenantId)
    {
        var record = new ProvisioningRecord(tenantId, 101, 999);
        var ctx = new ProvisionedTerminalContext(record);
        // db lifetime is fine here - it is backed by the shared in-memory connection
        // that the test class manages. No risk of premature disposal.
        return new CatalogService(CreateDbContext(ctx));
    }

    /// <summary>Creates a CatalogService whose DbContext is unprovisioned (CurrentTenantId=0).</summary>
    private CatalogService CreateUnprovisionedService()
        => new CatalogService(CreateDbContext(new ProvisionedTerminalContext()));

    // ---------------------------------------------------------------
    // ListCategories
    // ---------------------------------------------------------------

    [Fact]
    public async Task ListCategories_ProvisionedTerminal_ReturnsAllSeededCategories()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var categories = await service.ListCategoriesAsync();

        Assert.Equal(3, categories.Count);
        Assert.All(categories, c => Assert.False(string.IsNullOrEmpty(c.Code)));
        Assert.All(categories, c => Assert.False(string.IsNullOrEmpty(c.Name)));
    }

    [Fact]
    public async Task ListCategories_ReturnedInSortOrder()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var categories = await service.ListCategoriesAsync();

        var orders = categories.Select(c => c.SortOrder).ToList();
        Assert.Equal(orders.OrderBy(x => x).ToList(), orders);
    }

    [Fact]
    public async Task ListCategories_UnprovisionedTerminal_ReturnsEmpty()
    {
        await SeedForTenantAsync(42);
        var service = CreateUnprovisionedService();

        var categories = await service.ListCategoriesAsync();

        Assert.Empty(categories);
    }

    // ---------------------------------------------------------------
    // ListItems
    // ---------------------------------------------------------------

    [Fact]
    public async Task ListItems_ProvisionedTerminal_ReturnsAllSeededItems()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var items = await service.ListItemsAsync(new CatalogItemQuery());

        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.ItemCode)));
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.ItemName)));
        Assert.All(items, i => Assert.True(i.UnitPrice > 0));
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.UnitCode)));
    }

    [Fact]
    public async Task ListItems_JoinedFieldsAllPopulated_ForSeededItems()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var items = await service.ListItemsAsync(new CatalogItemQuery());

        Assert.All(items, i =>
        {
            Assert.True(i.VariantId > 0);
            Assert.False(string.IsNullOrEmpty(i.VariantCode));
            Assert.False(string.IsNullOrEmpty(i.CategoryCode));
            Assert.False(string.IsNullOrEmpty(i.CategoryName));
            Assert.False(string.IsNullOrEmpty(i.TaxCode));
            Assert.NotNull(i.TaxRate);
            Assert.NotNull(i.IdentifierValue);
            Assert.Equal("Active", i.Status);
            Assert.True(i.IsSellable);
        });
    }

    [Fact]
    public async Task ListItems_CategoryFilter_ReturnsOnlyItemsInThatCategory()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        // CategoryId=2 is BEVERAGES; Mineral Water (ITEM-001) has CategoryId=2 in seed.
        var items = await service.ListItemsAsync(new CatalogItemQuery { CategoryId = 2 });

        Assert.Single(items);
        Assert.Equal("ITEM-001", items[0].ItemCode);
    }

    [Fact]
    public async Task ListItems_LimitIsRespected()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var items = await service.ListItemsAsync(new CatalogItemQuery { Limit = 1 });

        Assert.Single(items);
    }

    [Fact]
    public async Task ListItems_UnprovisionedTerminal_ReturnsEmpty()
    {
        await SeedForTenantAsync(42);
        var service = CreateUnprovisionedService();

        var items = await service.ListItemsAsync(new CatalogItemQuery());

        Assert.Empty(items);
    }

    // ---------------------------------------------------------------
    // SearchItems
    // ---------------------------------------------------------------

    [Fact]
    public async Task SearchItems_ByItemName_ReturnsMatchingItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync("Water");

        Assert.Single(results);
        Assert.Equal("ITEM-001", results[0].ItemCode);
    }

    [Fact]
    public async Task SearchItems_ByItemCode_ReturnsMatchingItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync("ITEM-002");

        Assert.Single(results);
        Assert.Contains("Milk", results[0].ItemName);
    }

    [Fact]
    public async Task SearchItems_BySku_ReturnsMatchingItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync("SKU-003");

        Assert.Single(results);
        Assert.Contains("Crackers", results[0].ItemName);
    }

    [Fact]
    public async Task SearchItems_ByIdentifierValue_ReturnsMatchingItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync("5000001000010");

        Assert.Single(results);
        Assert.Equal("ITEM-001", results[0].ItemCode);
    }

    [Fact]
    public async Task SearchItems_BlankText_ReturnsAllItemsUpToLimit()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync(string.Empty, limit: 50);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task SearchItems_NoMatch_ReturnsEmptyList()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var results = await service.SearchItemsAsync("ZZNOTFOUND");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchItems_UnprovisionedTerminal_ReturnsEmpty()
    {
        await SeedForTenantAsync(42);
        var service = CreateUnprovisionedService();

        var results = await service.SearchItemsAsync("Water");

        Assert.Empty(results);
    }

    // ---------------------------------------------------------------
    // FindByIdentifier
    // ---------------------------------------------------------------

    [Fact]
    public async Task FindByIdentifier_KnownBarcode_ReturnsExpectedItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var item = await service.FindByIdentifierAsync("5000002000010");

        Assert.NotNull(item);
        Assert.Equal("ITEM-002", item!.ItemCode);
        Assert.Contains("Milk", item.ItemName);
        Assert.Equal("5000002000010", item.IdentifierValue);
    }

    [Fact]
    public async Task FindByIdentifier_UnknownBarcode_ReturnsNull()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        var item = await service.FindByIdentifierAsync("9999999999999");

        Assert.Null(item);
    }

    [Fact]
    public async Task FindByIdentifier_BlankValue_ReturnsNull()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        Assert.Null(await service.FindByIdentifierAsync(string.Empty));
        Assert.Null(await service.FindByIdentifierAsync("   "));
    }

    [Fact]
    public async Task FindByIdentifier_UnprovisionedTerminal_ReturnsNull()
    {
        await SeedForTenantAsync(42);
        var service = CreateUnprovisionedService();

        var item = await service.FindByIdentifierAsync("5000001000010");

        Assert.Null(item);
    }

    [Fact]
    public async Task FindByIdentifier_WhitespacePaddedBarcode_ReturnsMatchingItem()
    {
        await SeedForTenantAsync(42);
        var service = CreateProvisionedService(42);

        // Trimming must happen inside FindByIdentifierAsync before the DB lookup.
        var item = await service.FindByIdentifierAsync("  5000001000010  ");

        Assert.NotNull(item);
        Assert.Equal("ITEM-001", item!.ItemCode);
    }

    // ---------------------------------------------------------------
    // Active-only filtering
    // ---------------------------------------------------------------

    [Fact]
    public async Task ListItems_BlockedItem_IsExcluded()
    {
        await SeedForTenantAsync(42);

        // Insert a blocked item with an otherwise-valid variant and price (IDs chosen
        // to avoid collision with the 3 seed items: 101-103, variants 201-203, prices 401-403).
        using (var writeDb = CreateDbContext(new NoProvisionedTerminalContext()))
        {
            writeDb.LocalItems.Add(new LocalItem
            {
                Id = 199, TenantId = 42, ItemCode = "BLOCKED-001", Name = "Blocked Item",
                ItemType = ItemType.Stock,
                Status = ItemStatus.Blocked,
                IsTrackedInventory = false, DefaultUnitOfMeasureId = 1, CatalogVersion = 1
            });
            writeDb.LocalItemVariants.Add(new LocalItemVariant
            {
                Id = 299, TenantId = 42, ItemId = 199, VariantCode = "BLOCKED-001-DEFAULT",
                Name = "Blocked Item", UnitOfMeasureId = 1,
                IsDefault = true, IsSellable = true,
                Status = ItemStatus.Active, CatalogVersion = 1
            });
            writeDb.LocalItemPrices.Add(new LocalItemPrice
            {
                Id = 499, TenantId = 42, PriceListId = 1, ItemVariantId = 299,
                UnitOfMeasureId = 1, UnitPrice = 10.00m, IsTaxIncluded = true,
                EffectiveFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
            await writeDb.SaveChangesAsync();
        }

        var service = CreateProvisionedService(42);
        var items = await service.ListItemsAsync(new CatalogItemQuery());

        // Only the 3 seeded active items should appear; the blocked item must be excluded.
        Assert.Equal(3, items.Count);
        Assert.DoesNotContain(items, i => i.ItemCode == "BLOCKED-001");
    }

    [Fact]
    public async Task ListItems_NonSellableVariant_IsExcluded()
    {
        await SeedForTenantAsync(42);

        // Insert an active item whose sole default variant has IsSellable=false.
        using (var writeDb = CreateDbContext(new NoProvisionedTerminalContext()))
        {
            writeDb.LocalItems.Add(new LocalItem
            {
                Id = 198, TenantId = 42, ItemCode = "NOSELL-001", Name = "Non-Sellable Item",
                ItemType = ItemType.Stock,
                Status = ItemStatus.Active,
                IsTrackedInventory = false, DefaultUnitOfMeasureId = 1, CatalogVersion = 1
            });
            writeDb.LocalItemVariants.Add(new LocalItemVariant
            {
                Id = 298, TenantId = 42, ItemId = 198, VariantCode = "NOSELL-001-DEFAULT",
                Name = "Non-Sellable Item", UnitOfMeasureId = 1,
                IsDefault = true, IsSellable = false,
                Status = ItemStatus.Active, CatalogVersion = 1
            });
            writeDb.LocalItemPrices.Add(new LocalItemPrice
            {
                Id = 498, TenantId = 42, PriceListId = 1, ItemVariantId = 298,
                UnitOfMeasureId = 1, UnitPrice = 5.00m, IsTaxIncluded = true,
                EffectiveFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
            await writeDb.SaveChangesAsync();
        }

        var service = CreateProvisionedService(42);
        var items = await service.ListItemsAsync(new CatalogItemQuery());

        // Only the 3 seeded sellable items should appear; the non-sellable variant item must be excluded.
        Assert.Equal(3, items.Count);
        Assert.DoesNotContain(items, i => i.ItemCode == "NOSELL-001");
    }
}
