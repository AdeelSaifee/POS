using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Seeding;

public sealed class LocalCatalogSeeder : ILocalCatalogSeeder
{
    private readonly PosLocalDbContext _dbContext;

    public LocalCatalogSeeder(PosLocalDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SeedAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), tenantId, "Tenant ID must be a positive integer.");

        // Seeding order respects conceptual dependencies (UoM and TaxRule before Item/Variant/Price).
        await UpsertByIdAsync(_dbContext.LocalUnitsOfMeasure,  LocalCatalogSeedData.UnitsOfMeasure(tenantId),  cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalTaxRules,        LocalCatalogSeedData.TaxRules(tenantId),        cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalCategories,      LocalCatalogSeedData.Categories(tenantId),      cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalItems,           LocalCatalogSeedData.Items(tenantId),           cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalItemVariants,    LocalCatalogSeedData.ItemVariants(tenantId),    cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalItemIdentifiers, LocalCatalogSeedData.ItemIdentifiers(tenantId), cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalItemPrices,      LocalCatalogSeedData.ItemPrices(tenantId),      cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalTenderMethods,   LocalCatalogSeedData.TenderMethods(tenantId),   cancellationToken);
        await UpsertByIdAsync(_dbContext.LocalReasonCodes,     LocalCatalogSeedData.ReasonCodes(tenantId),     cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Insert-or-retarget policy:
    // Because local catalog IDs use a single-column PK (ValueGeneratedNever), the same bootstrap
    // Id cannot exist for two tenants in the same SQLite database. When the terminal is
    // re-provisioned to a different tenant, existing rows are retargeted (TenantId + all
    // seed-controlled fields updated) rather than duplicated. This keeps the bootstrap catalog
    // visible to exactly the currently provisioned tenant.
    // - Same tenant, same data  -> no DB update (EF change tracker detects no diff).
    // - Same tenant, re-run     -> no DB update (idempotent).
    // - Different tenant        -> UPDATE existing rows to new TenantId and fresh seed values.
    // - Row absent              -> INSERT.
    private async Task UpsertByIdAsync<T>(
        DbSet<T> dbSet,
        IReadOnlyList<T> seedRows,
        CancellationToken cancellationToken)
        where T : LocalCatalogEntity
    {
        foreach (var seedRow in seedRows)
        {
            var existing = await dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == seedRow.Id, cancellationToken);

            if (existing is null)
                dbSet.Add(seedRow);
            else
                _dbContext.Entry(existing).CurrentValues.SetValues(seedRow);
        }
    }
}
