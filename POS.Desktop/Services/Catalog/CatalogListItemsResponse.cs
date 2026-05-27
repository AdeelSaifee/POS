using System.Collections.Generic;

namespace POS.Desktop.Services.Catalog;

/// <summary>Bridge response payload for catalog.listItems and catalog.searchItems.</summary>
public sealed record CatalogListItemsResponse
{
    public IReadOnlyList<CatalogItemDto> Items { get; init; } = [];
}
