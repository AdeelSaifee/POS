using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Health;
using POS.Api.Contracts;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthStatusService _healthStatusService;

    public HealthController(IHealthStatusService healthStatusService)
    {
        _healthStatusService = healthStatusService;
    }

    [HttpGet]
    [ProducesResponseType<HealthStatusDto>(StatusCodes.Status200OK)]
    public ActionResult<HealthStatusDto> Get()
    {
        return Ok(_healthStatusService.GetStatus());
    }
}
