using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.UnitsOfMeasure;
using POS.Api.Contracts.UnitsOfMeasure;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/units-of-measure")]
[Authorize(Policy = "UserOrAdmin")]
public sealed class UnitsOfMeasureController : ControllerBase
{
    private readonly IUnitOfMeasureReadService _unitOfMeasureReadService;

    public UnitsOfMeasureController(IUnitOfMeasureReadService unitOfMeasureReadService)
    {
        _unitOfMeasureReadService = unitOfMeasureReadService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UnitOfMeasureListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnitOfMeasureListItemDto>>> Get(CancellationToken cancellationToken)
    {
        var unitsOfMeasure = await _unitOfMeasureReadService.GetUnitsOfMeasureAsync(cancellationToken);

        return Ok(unitsOfMeasure);
    }
}
