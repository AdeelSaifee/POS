using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using POS.Api.Auth;
using POS.Api.Services.Tenant;

namespace POS.Tests.UnitTests.Auth;

public class HttpContextCurrentTenantContextTests
{
    [Fact]
    public void AuthenticatedUser_WithValidTenantClaim_ResolvesTenant()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "42")));

        Assert.True(context.HasTenant);
        Assert.Equal(42, context.CurrentTenantId);
        Assert.False(context.IsSystemScope);
    }

    [Fact]
    public void AuthenticatedUser_WithoutTenantClaim_FailsClosed()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal());

        Assert.False(context.HasTenant);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.False(context.IsSystemScope);
    }

    [Fact]
    public void AuthenticatedUser_WithMalformedTenantClaim_FailsClosed()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "abc")));

        Assert.False(context.HasTenant);
        Assert.Equal(0, context.CurrentTenantId);
    }

    [Fact]
    public void AuthenticatedUser_WithZeroTenantClaim_FailsClosed()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "0")));

        Assert.False(context.HasTenant);
        Assert.Equal(0, context.CurrentTenantId);
    }

    [Fact]
    public void UnauthenticatedIdentity_FailsClosed()
    {
        var context = CreateSut(CreateUnauthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "42"),
            new Claim(ApiClaimTypes.SystemScope, "true")));

        Assert.False(context.HasTenant);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.False(context.IsSystemScope);
    }

    [Fact]
    public void AuthenticatedUser_WithSystemScopeTrueClaim_SetsIsSystemScopeTrue()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "42"),
            new Claim(ApiClaimTypes.SystemScope, "true")));

        Assert.True(context.HasTenant);
        Assert.Equal(42, context.CurrentTenantId);
        Assert.True(context.IsSystemScope);
    }

    [Fact]
    public void AuthenticatedUser_WithoutSystemScopeClaim_KeepsIsSystemScopeFalse()
    {
        var context = CreateSut(CreateAuthenticatedPrincipal(
            new Claim(ApiClaimTypes.TenantId, "42")));

        Assert.False(context.IsSystemScope);
    }

    private static HttpContextCurrentTenantContext CreateSut(ClaimsPrincipal principal)
    {
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        return new HttpContextCurrentTenantContext(accessor);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    private static ClaimsPrincipal CreateUnauthenticatedPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
}
