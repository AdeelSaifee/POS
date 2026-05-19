using POS.Api.Contracts.Categories;

namespace POS.Api.Application.Categories;

public interface ICategoryReadService : IApplicationService
{
    Task<IReadOnlyList<CategoryListItemDto>> GetCategoriesAsync(CancellationToken cancellationToken);
}
