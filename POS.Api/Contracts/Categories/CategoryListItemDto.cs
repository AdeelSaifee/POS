namespace POS.Api.Contracts.Categories;

public sealed record CategoryListItemDto(
    int Id,
    int? ParentCategoryId,
    string CategoryCode,
    string Name,
    string? ImageUrl,
    int DisplayOrder);
