using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Locations;
using POS.Api.Contracts.Locations;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize(Policy = "UserOrAdmin")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationReadService _locationReadService;

    public LocationsController(ILocationReadService locationReadService)
    {
        _locationReadService = locationReadService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LocationListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationListItemDto>>> Get(CancellationToken cancellationToken)
    {
        var locations = await _locationReadService.GetLocationsAsync(cancellationToken);

        return Ok(locations);
    }
}
