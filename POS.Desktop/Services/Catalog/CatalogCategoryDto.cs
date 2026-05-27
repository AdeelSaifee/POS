namespace POS.Desktop.Services.Catalog;

/// <summary>
/// UI-friendly representation of a local catalog category.
/// </summary>
public sealed record CatalogCategoryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
