using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Categories;
using POS.Api.Contracts.Categories;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Policy = "UserOrAdmin")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryReadService _categoryReadService;

    public CategoriesController(ICategoryReadService categoryReadService)
    {
        _categoryReadService = categoryReadService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryListItemDto>>> Get(CancellationToken cancellationToken)
    {
        var categories = await _categoryReadService.GetCategoriesAsync(cancellationToken);

        return Ok(categories);
    }
}
