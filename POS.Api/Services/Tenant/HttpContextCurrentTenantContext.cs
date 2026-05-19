using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using POS.Api.Auth;
using POS.Shared.Contracts;

namespace POS.Api.Services.Tenant;

public sealed class HttpContextCurrentTenantContext : ICurrentTenantContext
{
    private const int InvalidTenantId = 0;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int CurrentTenantId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (!TryGetValidatedAuthenticatedPrincipal(principal, out var authenticatedPrincipal))
            {
                return InvalidTenantId;
            }

            return TryGetPositiveTenantId(authenticatedPrincipal, out var tenantId)
                ? tenantId
                : InvalidTenantId;
        }
    }

    public bool HasTenant
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return TryGetValidatedAuthenticatedPrincipal(principal, out var authenticatedPrincipal) &&
                   TryGetPositiveTenantId(authenticatedPrincipal, out _);
        }
    }

    public bool IsSystemScope
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return TryGetValidatedAuthenticatedPrincipal(principal, out var authenticatedPrincipal) &&
                   authenticatedPrincipal.HasClaim(ApiClaimTypes.SystemScope, "true");
        }
    }

    private static bool TryGetValidatedAuthenticatedPrincipal(
        ClaimsPrincipal? principal,
        out ClaimsPrincipal authenticatedPrincipal)
    {
        if (principal?.Identity?.IsAuthenticated == true)
        {
            authenticatedPrincipal = principal;
            return true;
        }

        authenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        return false;
    }

    private static bool TryGetPositiveTenantId(ClaimsPrincipal principal, out int tenantId)
    {
        var tenantClaimValue = principal.FindFirstValue(ApiClaimTypes.TenantId);
        if (!int.TryParse(tenantClaimValue, out tenantId))
        {
            tenantId = InvalidTenantId;
            return false;
        }

        if (tenantId <= InvalidTenantId)
        {
            tenantId = InvalidTenantId;
            return false;
        }

        return true;
    }
}
