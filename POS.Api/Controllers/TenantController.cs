using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Tenant;
using POS.Api.Contracts.Tenant;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize(Policy = "UserOrAdmin")]
public sealed class TenantController : ControllerBase
{
    private readonly ITenantProfileReadService _tenantProfileReadService;

    public TenantController(ITenantProfileReadService tenantProfileReadService)
    {
        _tenantProfileReadService = tenantProfileReadService;
    }

    [HttpGet("profile")]
    [ProducesResponseType<TenantProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var tenantProfile = await _tenantProfileReadService.GetCurrentTenantProfileAsync(cancellationToken);

        if (tenantProfile is null)
        {
            return NotFound();
        }

        return Ok(tenantProfile);
    }
}
