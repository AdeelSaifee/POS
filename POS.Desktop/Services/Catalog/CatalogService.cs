using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Desktop.Data;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Catalog;

/// <summary>
/// Reads local catalog data from SQLite via PosLocalDbContext.
/// Relies on the existing global query filters for tenant scoping -
/// no IgnoreQueryFilters() calls, no SaveChangesAsync, no mutations.
/// </summary>
public sealed class CatalogService : ICatalogService
{
    // Bootstrap price list ID - matches the seed data PriceListId.
    private const int DefaultPriceListId = 1;

    // Hard cap to keep bridge payloads reasonable.
    private const int MaxQueryLimit = 200;

    private readonly PosLocalDbContext _db;

    public CatalogService(PosLocalDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // ------------------------------------------------------------------
    // ICatalogService
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<CatalogCategoryDto>> ListCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.LocalCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CatalogCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                SortOrder = c.SortOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItemDto>> ListItemsAsync(
        CatalogItemQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var limit = Math.Clamp(query.Limit, 1, MaxQueryLimit);

        var q = BuildItemQuery();

        if (query.CategoryId.HasValue)
            q = q.Where(x => x.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var pattern = $"%{query.SearchText.Trim()}%";
            q = q.Where(x =>
                EF.Functions.Like(x.ItemName, pattern) ||
                EF.Functions.Like(x.ItemCode, pattern));
        }

        return await q
            .OrderBy(x => x.ItemName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItemDto>> SearchItemsAsync(
        string searchText,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, MaxQueryLimit);

        // Blank search is safe - returns all items up to the limit.
        if (string.IsNullOrWhiteSpace(searchText))
            return await BuildItemQuery()
                .OrderBy(x => x.ItemName)
                .Take(clampedLimit)
                .ToListAsync(cancellationToken);

        var pattern = $"%{searchText.Trim()}%";
        return await BuildItemQuery()
            .Where(x =>
                EF.Functions.Like(x.ItemName, pattern) ||
                EF.Functions.Like(x.ItemCode, pattern) ||
                (x.Sku != null && EF.Functions.Like(x.Sku, pattern)) ||
                (x.IdentifierValue != null && EF.Functions.Like(x.IdentifierValue, pattern)))
            .OrderBy(x => x.ItemName)
            .Take(clampedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogItemDto?> FindByIdentifierAsync(
        string identifierValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifierValue))
            return null;

        return await BuildItemQuery()
            .Where(x => x.IdentifierValue == identifierValue.Trim())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogItemDto?> FindByVariantIdAsync(
        int variantId,
        CancellationToken cancellationToken = default)
    {
        if (variantId <= 0)
            return null;

        return await BuildItemQuery()
            .Where(x => x.VariantId == variantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ------------------------------------------------------------------
    // Query builder
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns the base IQueryable that joins LocalItems with their default variant,
    /// price (default price list), primary identifier, category, unit of measure,
    /// and tax rule. Global query filters on each DbSet automatically scope reads
    /// to the currently provisioned tenant - no IgnoreQueryFilters needed.
    /// Callers compose additional Where/Take/OrderBy clauses before materialising.
    /// </summary>
    private IQueryable<CatalogItemDto> BuildItemQuery()
    {
        return
            from item in _db.LocalItems.AsNoTracking()

            // Inner join - only active default variants that are sellable are included.
            join variant in _db.LocalItemVariants.AsNoTracking()
                    .Where(v => v.IsDefault && v.IsSellable && v.Status == ItemStatus.Active)
                on item.Id equals variant.ItemId

            // Inner join - items without a price in the default price list are excluded.
            join price in _db.LocalItemPrices.AsNoTracking().Where(p => p.PriceListId == DefaultPriceListId)
                on variant.Id equals price.ItemVariantId

            // Inner join - every variant must have a unit of measure.
            join uom in _db.LocalUnitsOfMeasure.AsNoTracking()
                on variant.UnitOfMeasureId equals uom.Id

            // Left join - variant may have no identifier yet (bootstrapping phase).
            join identifier in _db.LocalItemIdentifiers.AsNoTracking().Where(i => i.IsPrimary)
                on (int?)variant.Id equals identifier.ItemVariantId into identGroup
            from identifier in identGroup.DefaultIfEmpty()

            // Left join - item may have no category.
            join category in _db.LocalCategories.AsNoTracking()
                on item.CategoryId equals (int?)category.Id into catGroup
            from category in catGroup.DefaultIfEmpty()

            // Left join - variant may have no tax rule (e.g., zero-rated).
            join taxRule in _db.LocalTaxRules.AsNoTracking()
                on variant.TaxRuleId equals (int?)taxRule.Id into taxGroup
            from taxRule in taxGroup.DefaultIfEmpty()

            where item.Status == ItemStatus.Active

            select new CatalogItemDto
            {
                ItemId       = item.Id,
                ItemCode     = item.ItemCode,
                ItemName     = item.Name,
                Status       = item.Status == ItemStatus.Active       ? "Active"
                             : item.Status == ItemStatus.Draft        ? "Draft"
                             : item.Status == ItemStatus.Discontinued ? "Discontinued"
                             : item.Status == ItemStatus.Blocked      ? "Blocked"
                             : "Unknown",

                CategoryId   = item.CategoryId,
                CategoryCode = category == null ? null : category.Code,
                CategoryName = category == null ? null : category.Name,

                VariantId    = variant.Id,
                VariantCode  = variant.VariantCode,
                Sku          = variant.SKU,
                IsSellable   = variant.IsSellable,

                IdentifierValue = identifier == null ? null : identifier.IdentifierValue,

                UnitPrice    = price.UnitPrice,
                IsTaxIncluded = price.IsTaxIncluded,

                TaxRuleId    = variant.TaxRuleId,
                TaxCode      = taxRule == null ? null : taxRule.Code,
                TaxRate      = taxRule == null ? (decimal?)null : taxRule.Rate,

                UnitOfMeasureId = uom.Id,
                UnitCode     = uom.Code,
                UnitName     = uom.Name
            };
    }
}
