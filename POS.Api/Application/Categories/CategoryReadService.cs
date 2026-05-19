using Microsoft.EntityFrameworkCore;
using POS.Api.Contracts.Categories;
using POS.Api.Data;

namespace POS.Api.Application.Categories;

public sealed class CategoryReadService : ICategoryReadService
{
    private readonly PosCentralDbContext _dbContext;

    public CategoryReadService(PosCentralDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryListItemDto(
                x.Id,
                x.ParentCategoryId,
                x.Code,
                x.Name,
                x.ImageUrl,
                x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
