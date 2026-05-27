namespace POS.Desktop.Data.LocalEntities;

public class LocalCategory : LocalCatalogEntity
{
    public int? ParentCategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
