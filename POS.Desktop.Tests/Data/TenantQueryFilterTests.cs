using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Data;

public sealed class TenantQueryFilterTests
{
    private async Task VerifyFilterAsync<TEntity>(
        Func<int, TEntity> createEntityFunc,
        Func<PosLocalDbContext, DbSet<TEntity>> getDbSetFunc)
        where TEntity : class
    {
        using var dbHarness = new SqliteTestDatabase();

        // 1. Seed rows for Tenant 42 and Tenant 99.
        // We use NoProvisionedTerminalContext so the seeder can write.
        using (var seedDb = dbHarness.CreateDbContext(new NoProvisionedTerminalContext()))
        {
            var entityA = createEntityFunc(42);
            var entityB = createEntityFunc(99);

            seedDb.Set<TEntity>().AddRange(entityA, entityB);
            await seedDb.SaveChangesAsync();
        }

        // 2. Query under provisioned context for Tenant 42.
        using (var readDb42 = dbHarness.CreateProvisionedDbContext(42))
        {
            var results42 = await getDbSetFunc(readDb42).ToListAsync();
            Assert.Single(results42);
            var tenantId = (int)typeof(TEntity).GetProperty("TenantId")!.GetValue(results42[0])!;
            Assert.Equal(42, tenantId);
        }

        // 3. Query under provisioned context for Tenant 99.
        using (var readDb99 = dbHarness.CreateProvisionedDbContext(99))
        {
            var results99 = await getDbSetFunc(readDb99).ToListAsync();
            Assert.Single(results99);
            var tenantId = (int)typeof(TEntity).GetProperty("TenantId")!.GetValue(results99[0])!;
            Assert.Equal(99, tenantId);
        }

        // 4. Query under unprovisioned context.
        using (var readDbUnprovisioned = dbHarness.CreateUnprovisionedDbContext())
        {
            var resultsUnprovisioned = await getDbSetFunc(readDbUnprovisioned).ToListAsync();
            Assert.Empty(resultsUnprovisioned);
        }
    }

    [Fact]
    public Task Verify_SyncOutbox_TenantFilter() => VerifyFilterAsync(
        tenantId => new SyncOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            BusinessDate = DateOnly.FromDateTime(DateTime.Today),
            TerminalSequence = 1,
            EventType = "Sale",
            EventId = Guid.NewGuid(),
            PayloadJson = "{}",
            PayloadHash = "hash",
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            Status = SyncOutboxStatus.Pending,
            AttemptCount = 0,
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.SyncOutbox
    );

    [Fact]
    public Task Verify_PrintQueue_TenantFilter() => VerifyFilterAsync(
        tenantId => new PrintQueue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            PrintJobType = "Receipt",
            PayloadJson = "{}",
            Status = PrintQueueStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.PrintQueue
    );

    [Fact]
    public Task Verify_LocalRecoveryJournal_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalRecoveryJournal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            RecoveryType = RecoveryType.AppCrash,
            Status = RecoveryJournalStatus.Open,
            StatePayloadJson = "{}",
            RequiredAction = RequiredRecoveryAction.RetrySync,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.LocalRecoveryJournal
    );

    [Fact]
    public Task Verify_PaymentReconciliationQueue_TenantFilter() => VerifyFilterAsync(
        tenantId => new PaymentReconciliationQueue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            TenderMethodId = 1,
            Status = PaymentReconciliationStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.PaymentReconciliationQueue
    );

    [Fact]
    public Task Verify_SyncCursor_TenantFilter() => VerifyFilterAsync(
        tenantId => new SyncCursor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            StreamName = "Catalog",
            Status = SyncCursorStatus.Active,
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.SyncCursors
    );

    [Fact]
    public Task Verify_LocalRetentionState_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalRetentionState
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 101,
            TerminalId = 999,
            Category = "Operational",
            RetentionDays = 30,
            Status = LocalRetentionStatus.Active,
            IsActive = true,
            CreatedBy = "Tester",
            CreatedOn = DateTimeOffset.UtcNow
        },
        db => db.LocalRetentionState
    );

    [Fact]
    public Task Verify_LocalCategory_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalCategory
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            Code = "CAT-" + tenantId,
            Name = "Category-" + tenantId,
            SortOrder = 1
        },
        db => db.LocalCategories
    );

    [Fact]
    public Task Verify_LocalItem_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalItem
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            ItemCode = "ITEM-" + tenantId,
            Name = "Item-" + tenantId,
            ItemType = ItemType.Stock,
            Status = ItemStatus.Active,
            IsTrackedInventory = true,
            DefaultUnitOfMeasureId = 1,
            CatalogVersion = 1
        },
        db => db.LocalItems
    );

    [Fact]
    public Task Verify_LocalItemVariant_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalItemVariant
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            ItemId = tenantId * 10 + 1,
            VariantCode = "VAR-" + tenantId,
            Name = "Variant-" + tenantId,
            UnitOfMeasureId = 1,
            IsDefault = true,
            IsSellable = true,
            Status = ItemStatus.Active,
            CatalogVersion = 1
        },
        db => db.LocalItemVariants
    );

    [Fact]
    public Task Verify_LocalItemIdentifier_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalItemIdentifier
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            ItemId = tenantId * 10 + 1,
            ItemVariantId = tenantId * 10 + 1,
            IdentifierType = "Barcode",
            IdentifierValue = "BARCODE-" + tenantId,
            IsPrimary = true
        },
        db => db.LocalItemIdentifiers
    );

    [Fact]
    public Task Verify_LocalItemPrice_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalItemPrice
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            PriceListId = 1,
            ItemVariantId = tenantId * 10 + 1,
            UnitOfMeasureId = 1,
            UnitPrice = 10.00m,
            IsTaxIncluded = true,
            EffectiveFrom = DateTimeOffset.UtcNow
        },
        db => db.LocalItemPrices
    );

    [Fact]
    public Task Verify_LocalUnitOfMeasure_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalUnitOfMeasure
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            Code = "UOM-" + tenantId,
            Name = "UOM-" + tenantId,
            MeasurementType = MeasurementType.Count,
            DecimalPlaces = 0,
            AllowsFractionalQuantity = false
        },
        db => db.LocalUnitsOfMeasure
    );

    [Fact]
    public Task Verify_LocalTaxRule_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalTaxRule
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            Code = "TAX-" + tenantId,
            Name = "TAX-" + tenantId,
            Rate = 0.10m,
            CalculationMode = TaxCalculationMode.Inclusive,
            RuleVersion = 1
        },
        db => db.LocalTaxRules
    );

    [Fact]
    public Task Verify_LocalTenderMethod_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalTenderMethod
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            Code = "TENDER-" + tenantId,
            Name = "TENDER-" + tenantId,
            TenderType = "Cash",
            AllowsChange = true,
            RequiresExternalReference = false,
            SortOrder = 1
        },
        db => db.LocalTenderMethods
    );

    [Fact]
    public Task Verify_LocalReasonCode_TenantFilter() => VerifyFilterAsync(
        tenantId => new LocalReasonCode
        {
            Id = tenantId * 10 + 1,
            TenantId = tenantId,
            Code = "REASON-" + tenantId,
            Name = "REASON-" + tenantId,
            ReasonCategory = "Discount",
            RequiresManagerApproval = false,
            SortOrder = 1
        },
        db => db.LocalReasonCodes
    );
}
