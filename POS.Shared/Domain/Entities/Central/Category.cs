using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class Category : TenantScopedEntity
{
    public int? ParentCategoryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }
}
