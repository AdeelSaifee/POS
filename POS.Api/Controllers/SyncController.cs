using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Auth;
using POS.Api.Application.Sync;
using POS.Shared.Contracts.Sync;

namespace POS.Api.Controllers;

/// <summary>
/// Controller handling sync ingest endpoints for authenticated devices.
/// </summary>
[ApiController]
[Route("api/sync")]
[Authorize(Policy = "PosDevice")]
public sealed class SyncController : ControllerBase
{
    private readonly ISyncIngestService _syncIngestService;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncController"/>.
    /// </summary>
    public SyncController(ISyncIngestService syncIngestService)
    {
        _syncIngestService = syncIngestService;
    }

    /// <summary>
    /// Ingests a batch of POS terminal outbox events.
    /// </summary>
    /// <param name="request">The chunk batch push request payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A structured synchronization response acknowledgment.</returns>
    [HttpPost("ingest")]
    [ProducesResponseType<SyncIngestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<SyncIngestResponse>> Ingest(
        [FromBody] SyncIngestRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Extract claims-derived sync identity
        var tenantIdClaim = User.FindFirstValue(ApiClaimTypes.TenantId);
        var locationIdClaim = User.FindFirstValue(ApiClaimTypes.LocationId);
        var terminalIdClaim = User.FindFirstValue(ApiClaimTypes.TerminalId);
        var deviceId = User.FindFirstValue(ApiClaimTypes.DeviceId);

        if (string.IsNullOrEmpty(tenantIdClaim) ||
            string.IsNullOrEmpty(locationIdClaim) ||
            string.IsNullOrEmpty(terminalIdClaim) ||
            !int.TryParse(tenantIdClaim, out var tenantId) ||
            !int.TryParse(locationIdClaim, out var locationId) ||
            !int.TryParse(terminalIdClaim, out var terminalId) ||
            tenantId <= 0 ||
            locationId <= 0 ||
            terminalId <= 0)
        {
            return Forbid();
        }

        var identity = new SyncIngestIdentity(tenantId, locationId, terminalId, deviceId);

        // 2. Perform consistency check against request body identity
        if (identity.TenantId != request.TenantId)
        {
            return Problem(
                detail: $"TenantId in request body ({request.TenantId}) does not match authenticated device claim ({identity.TenantId}).",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Device Identity Mismatch");
        }

        if (identity.LocationId != request.LocationId)
        {
            return Problem(
                detail: $"LocationId in request body ({request.LocationId}) does not match authenticated device claim ({identity.LocationId}).",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Device Identity Mismatch");
        }

        if (identity.TerminalId != request.TerminalId)
        {
            return Problem(
                detail: $"TerminalId in request body ({request.TerminalId}) does not match authenticated device claim ({identity.TerminalId}).",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Device Identity Mismatch");
        }

        // 3. Call the sync ingest service and handle deferred processing safely
        try
        {
            var response = await _syncIngestService.IngestAsync(identity, request, cancellationToken);
            return Ok(response);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(
                StatusCodes.Status501NotImplemented,
                new { message = "Sync ingest persistence is not implemented yet. This is deferred to Milestone 6.1 persistence tasks.", details = ex.Message });
        }
    }
}
