using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Catalog;

/// <summary>
/// Read-only catalog service that serves local SQLite catalog data scoped to the
/// currently provisioned tenant. Returns empty collections when the terminal is
/// unprovisioned (fail-closed via the global query filter).
/// </summary>
public interface ICatalogService
{
    /// <summary>Returns all active categories ordered by SortOrder.</summary>
    Task<IReadOnlyList<CatalogCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns items matching the given query. Supports optional category filter,
    /// optional text filter, and a row limit. Safe to call with an empty query.
    /// </summary>
    Task<IReadOnlyList<CatalogItemDto>> ListItemsAsync(CatalogItemQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches items by name, code, SKU, or identifier value. Case-insensitive.
    /// Returns a safe empty list when searchText is blank.
    /// </summary>
    Task<IReadOnlyList<CatalogItemDto>> SearchItemsAsync(string searchText, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single item by its barcode or other identifier value.
    /// Returns null when no match is found or the identifier is blank.
    /// </summary>
    Task<CatalogItemDto?> FindByIdentifierAsync(string identifierValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single item by its variant ID.
    /// Returns null when no match is found or the variant ID is invalid.
    /// </summary>
    Task<CatalogItemDto?> FindByVariantIdAsync(int variantId, CancellationToken cancellationToken = default);
}
