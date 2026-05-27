using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Data.Seeding;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Data.Seeding;

public sealed class LocalCatalogSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public LocalCatalogSeederTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Dispose();

    private PosLocalDbContext CreateDbContext()
    {
        var db = new PosLocalDbContext(_options, new NoProvisionedTerminalContext());
        db.Database.EnsureCreated();
        return db;
    }

    // Test 1: First seed inserts the expected number of rows per entity type.
    [Fact]
    public async Task SeedAsync_InsertsExpectedRowCounts_ForProvisionedTenant()
    {
        using var db = CreateDbContext();
        var seeder = new LocalCatalogSeeder(db);

        await seeder.SeedAsync(42);

        Assert.Equal(2, await db.LocalUnitsOfMeasure.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.LocalTaxRules.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalCategories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItems.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemVariants.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemIdentifiers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemPrices.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.LocalTenderMethods.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalReasonCodes.IgnoreQueryFilters().CountAsync());
    }

    // Test 2: Running the seed twice for the same tenant must not create duplicate rows.
    [Fact]
    public async Task SeedAsync_IsIdempotent_RowCountsStableOnRerun()
    {
        using var db = CreateDbContext();
        var seeder = new LocalCatalogSeeder(db);

        await seeder.SeedAsync(42);
        await seeder.SeedAsync(42);

        Assert.Equal(2, await db.LocalUnitsOfMeasure.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.LocalTaxRules.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalCategories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItems.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemVariants.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemIdentifiers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemPrices.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.LocalTenderMethods.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalReasonCodes.IgnoreQueryFilters().CountAsync());
    }

    // Test 3: An invalid tenant ID must throw before any DB access.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SeedAsync_WithInvalidTenantId_ThrowsAndInsertsNothing(int invalidTenantId)
    {
        using var db = CreateDbContext();
        var seeder = new LocalCatalogSeeder(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            seeder.SeedAsync(invalidTenantId));

        Assert.Equal(0, await db.LocalUnitsOfMeasure.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.LocalCategories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.LocalItems.IgnoreQueryFilters().CountAsync());
    }

    // Test 4: Core re-provision correctness test.
    // This is the same-DB re-provision scenario that catches the ID-based-skip bug:
    // if the seeder skips rows that already exist by Id, tenant 99 would see zero catalog rows
    // after re-provisioning because the global query filter scopes by TenantId.
    // The correct behavior is to retarget all seed rows to the new tenant.
    [Fact]
    public async Task SeedAsync_ReProvisionInSameDatabase_RetargetsSeedRowsToNewTenant()
    {
        using var db = CreateDbContext();
        var seeder = new LocalCatalogSeeder(db);

        // First provisioning — tenant 42
        await seeder.SeedAsync(42);

        var uomsAfterFirst = await db.LocalUnitsOfMeasure.IgnoreQueryFilters().ToListAsync();
        Assert.All(uomsAfterFirst, x => Assert.Equal(42, x.TenantId));

        // Re-provision to tenant 99 — same database, same bootstrap IDs
        await seeder.SeedAsync(99);

        // Row counts must stay stable — no duplication
        Assert.Equal(2, await db.LocalUnitsOfMeasure.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.LocalTaxRules.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalCategories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItems.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemVariants.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemIdentifiers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalItemPrices.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.LocalTenderMethods.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.LocalReasonCodes.IgnoreQueryFilters().CountAsync());

        // All rows must now carry TenantId 99
        var uomsAfterReprovision = await db.LocalUnitsOfMeasure.IgnoreQueryFilters().ToListAsync();
        Assert.All(uomsAfterReprovision, x => Assert.Equal(99, x.TenantId));

        var itemsAfterReprovision = await db.LocalItems.IgnoreQueryFilters().ToListAsync();
        Assert.All(itemsAfterReprovision, x => Assert.Equal(99, x.TenantId));

        var tenderMethodsAfterReprovision = await db.LocalTenderMethods.IgnoreQueryFilters().ToListAsync();
        Assert.All(tenderMethodsAfterReprovision, x => Assert.Equal(99, x.TenantId));

        // Tenant 42 must have zero visible rows — proves retargeting, not orphaning
        Assert.Equal(0, await db.LocalUnitsOfMeasure.IgnoreQueryFilters()
            .Where(x => x.TenantId == 42).CountAsync());
        Assert.Equal(0, await db.LocalItems.IgnoreQueryFilters()
            .Where(x => x.TenantId == 42).CountAsync());

        // Tenant 99 must have all expected rows
        Assert.Equal(2, await db.LocalUnitsOfMeasure.IgnoreQueryFilters()
            .Where(x => x.TenantId == 99).CountAsync());
        Assert.Equal(3, await db.LocalItems.IgnoreQueryFilters()
            .Where(x => x.TenantId == 99).CountAsync());
    }

    // Test 5: Catalog entity types must not declare sensitive property names.
    [Fact]
    public void CatalogEntities_DoNotHaveSensitiveProperties()
    {
        var sensitiveNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PIN", "Password", "Token", "CardNumber", "Secret", "ConnectionString"
        };

        var entityTypes = new[]
        {
            typeof(LocalCategory), typeof(LocalItem), typeof(LocalItemVariant),
            typeof(LocalItemIdentifier), typeof(LocalItemPrice), typeof(LocalUnitOfMeasure),
            typeof(LocalTaxRule), typeof(LocalTenderMethod), typeof(LocalReasonCode)
        };

        foreach (var type in entityTypes)
        {
            foreach (var prop in type.GetProperties())
            {
                Assert.False(sensitiveNames.Contains(prop.Name),
                    $"{type.Name}.{prop.Name} looks like a sensitive field and must not exist on a catalog entity.");
            }
        }
    }

    // Test 6: Successful provisioning must call the seeder with the correct tenant ID.
    [Fact]
    public async Task ProvisionTerminalAsync_CallsSeeder_WithCorrectTenantIdAfterSuccess()
    {
        using var db = CreateDbContext();
        var terminalContext = new ProvisionedTerminalContext();
        var spy = new SpyCatalogSeeder();

        var store = new EfTerminalProvisioningStore(
            db,
            terminalContext,
            spy,
            NullLogger<EfTerminalProvisioningStore>.Instance);

        var result = await store.ProvisionTerminalAsync(42, 101, 999, false, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, spy.CallCount);
        Assert.Equal(42, spy.LastTenantId);
    }

    // Test 7: Invalid provisioning input must not reach the seeder.
    [Theory]
    [InlineData(0,  101, 999)]
    [InlineData(42,   0, 999)]
    [InlineData(42, 101,   0)]
    public async Task ProvisionTerminalAsync_WithInvalidPayload_DoesNotCallSeeder(
        int tenantId, int locationId, int terminalId)
    {
        using var db = CreateDbContext();
        var terminalContext = new ProvisionedTerminalContext();
        var spy = new SpyCatalogSeeder();

        var store = new EfTerminalProvisioningStore(
            db,
            terminalContext,
            spy,
            NullLogger<EfTerminalProvisioningStore>.Instance);

        var result = await store.ProvisionTerminalAsync(tenantId, locationId, terminalId, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, spy.CallCount);
    }

    // Test 8: A seeder failure must return a safe structured error without leaking internals.
    [Fact]
    public async Task ProvisionTerminalAsync_WhenSeederThrows_ReturnsSafeStructuredError()
    {
        using var db = CreateDbContext();
        var terminalContext = new ProvisionedTerminalContext();
        var failing = new FailingCatalogSeeder();

        var store = new EfTerminalProvisioningStore(
            db,
            terminalContext,
            failing,
            NullLogger<EfTerminalProvisioningStore>.Instance);

        var result = await store.ProvisionTerminalAsync(42, 101, 999, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("SEED_FAILED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain("Exception", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        // Provisioning row must be committed even when seed fails — retry is safe.
        var row = await db.TerminalProvisioning.FirstOrDefaultAsync(x => x.Id == 1);
        Assert.NotNull(row);
        Assert.Equal(42, row.TenantId);
    }

    // -------------------------------------------------------------------------
    // Task 4.3.8 — No-op re-run verification
    // -------------------------------------------------------------------------

    // Test 9: Uses separate DbContext instances per seed run so EF's change tracker
    // does not mask any actual DB-level duplication. A third independent context is
    // used for the final count assertion.
    [Fact]
    public async Task SeedAsync_IsIdempotent_FreshContextSeesNoGrowth()
    {
        using (var db = CreateDbContext())
            await new LocalCatalogSeeder(db).SeedAsync(42);

        using (var db = CreateDbContext())
            await new LocalCatalogSeeder(db).SeedAsync(42);

        using var verifyDb = CreateDbContext();
        Assert.Equal(2, await verifyDb.LocalUnitsOfMeasure.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await verifyDb.LocalTaxRules.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalCategories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalItems.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalItemVariants.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalItemIdentifiers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalItemPrices.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await verifyDb.LocalTenderMethods.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await verifyDb.LocalReasonCodes.IgnoreQueryFilters().CountAsync());
    }

    // -------------------------------------------------------------------------
    // Task 4.3.10 — Seeded catalog visibility through global query filters
    // -------------------------------------------------------------------------

    // Test 10: Provisioned context with TenantId=42 must be able to read all seeded
    // catalog rows via normal DbSet queries (i.e. WITH the global query filter active).
    // Proves that seeding produces tenant-visible data for the provisioned terminal.
    [Fact]
    public async Task ProvisionedContext_CanReadSeededCatalogThroughQueryFilter()
    {
        // Seed using the normal (NoProvisionedTerminalContext) seeder path —
        // inserts bypass the read filter, so seed data lands with TenantId=42.
        using (var db = CreateDbContext())
            await new LocalCatalogSeeder(db).SeedAsync(42);

        // Re-open the same SQLite connection with a provisioned context (TenantId=42).
        // The global query filter x => x.TenantId == CurrentTenantId now passes for 42.
        var record = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);
        var provisionedContext = new ProvisionedTerminalContext(record);
        using var readDb = new PosLocalDbContext(_options, provisionedContext);

        var categories = await readDb.LocalCategories.ToListAsync();
        var items = await readDb.LocalItems.ToListAsync();
        var uoms = await readDb.LocalUnitsOfMeasure.ToListAsync();
        var taxRules = await readDb.LocalTaxRules.ToListAsync();
        var variants = await readDb.LocalItemVariants.ToListAsync();
        var prices = await readDb.LocalItemPrices.ToListAsync();
        var tenderMethods = await readDb.LocalTenderMethods.ToListAsync();
        var reasonCodes = await readDb.LocalReasonCodes.ToListAsync();

        Assert.Equal(3, categories.Count);
        Assert.Equal(3, items.Count);
        Assert.Equal(2, uoms.Count);
        Assert.Single(taxRules);
        Assert.Equal(3, variants.Count);
        Assert.Equal(3, prices.Count);
        Assert.Equal(2, tenderMethods.Count);
        Assert.Equal(3, reasonCodes.Count);

        Assert.All(categories, c => Assert.Equal(42, c.TenantId));
        Assert.All(items, i => Assert.Equal(42, i.TenantId));
        Assert.All(uoms, u => Assert.Equal(42, u.TenantId));
    }

    // Test 11: Unprovisioned context (CurrentTenantId=0) must return zero rows even
    // after the catalog has been seeded for tenant 42. Proves fail-closed behavior
    // of the global query filter.
    [Fact]
    public async Task UnprovisionedContext_CannotReadSeededCatalog()
    {
        using (var db = CreateDbContext())
            await new LocalCatalogSeeder(db).SeedAsync(42);

        // New context with an unprovisioned terminal — CurrentTenantId=0.
        var unprovisionedContext = new ProvisionedTerminalContext();
        using var readDb = new PosLocalDbContext(_options, unprovisionedContext);

        Assert.Empty(await readDb.LocalCategories.ToListAsync());
        Assert.Empty(await readDb.LocalItems.ToListAsync());
        Assert.Empty(await readDb.LocalUnitsOfMeasure.ToListAsync());
        Assert.Empty(await readDb.LocalTaxRules.ToListAsync());
        Assert.Empty(await readDb.LocalItemVariants.ToListAsync());
        Assert.Empty(await readDb.LocalItemPrices.ToListAsync());
        Assert.Empty(await readDb.LocalTenderMethods.ToListAsync());
        Assert.Empty(await readDb.LocalReasonCodes.ToListAsync());
    }

    // Spy and fake seeder helpers

    private sealed class SpyCatalogSeeder : ILocalCatalogSeeder
    {
        public int CallCount { get; private set; }
        public int? LastTenantId { get; private set; }

        public Task SeedAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastTenantId = tenantId;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingCatalogSeeder : ILocalCatalogSeeder
    {
        public Task SeedAsync(int tenantId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated seed failure.");
    }
}
