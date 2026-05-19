using System.Net.Http.Headers;
using POS.Api.Auth;

namespace POS.Tests.Integration.Auth;

internal sealed class TestRequestAuthentication
{
    private readonly Dictionary<string, string> _claims = new(StringComparer.Ordinal);

    public bool IsAuthenticated { get; private set; }

    public IReadOnlyDictionary<string, string> Claims => _claims;

    public static TestRequestAuthentication Unauthenticated() => new();

    public static TestRequestAuthentication Authenticated() => new()
    {
        IsAuthenticated = true
    };

    public static TestRequestAuthentication UserOrAdmin(
        string clientType = "user",
        string? tenantId = null)
    {
        var authentication = Authenticated()
            .WithClaim(ApiClaimTypes.ClientType, clientType);

        if (tenantId is not null)
        {
            authentication.WithClaim(ApiClaimTypes.TenantId, tenantId);
        }

        return authentication;
    }

    public TestRequestAuthentication WithClaim(string claimType, string claimValue)
    {
        IsAuthenticated = true;
        _claims[claimType] = claimValue;
        return this;
    }

    public void Apply(HttpRequestMessage request)
    {
        request.Headers.Remove(TestAuthenticationDefaults.AuthenticateHeader);

        foreach (var claimType in _claims.Keys)
        {
            request.Headers.Remove(TestAuthenticationDefaults.ClaimHeaderPrefix + claimType);
        }

        if (!IsAuthenticated)
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(TestAuthenticationDefaults.AuthenticateHeader, bool.TrueString);

        foreach (var claim in _claims)
        {
            request.Headers.TryAddWithoutValidation(
                TestAuthenticationDefaults.ClaimHeaderPrefix + claim.Key,
                claim.Value);
        }
    }
}
