namespace POS.Desktop.Services.Catalog;

/// <summary>
/// Parameters for a catalog item list or filter query.
/// All fields are optional - omitted fields apply no filter.
/// </summary>
public sealed record CatalogItemQuery
{
    /// <summary>Optional category filter. When set, only items in this category are returned.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Optional text filter applied to item name and code. Case-insensitive.</summary>
    public string? SearchText { get; init; }

    /// <summary>Maximum number of rows to return. Clamped to 1-200. Defaults to 50.</summary>
    public int Limit { get; init; } = 50;
}
