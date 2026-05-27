using System.Collections.Generic;

namespace POS.Desktop.Services.Catalog;

/// <summary>Bridge response payload for catalog.listCategories.</summary>
public sealed record CatalogListCategoriesResponse
{
    public IReadOnlyList<CatalogCategoryDto> Categories { get; init; } = [];
}
