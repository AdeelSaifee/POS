using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using POS.Api.Application.Auth;

namespace POS.Tests.UnitTests.Auth;

public sealed class JwtAccessTokenIssuerTests
{
    [Fact]
    public void IssueAccessToken_ValidUserDescriptor_EmitsTenantAndUserClientType()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "user", tenantId: 101, userId: 5001, employeeId: null);

        var issuedToken = issuer.IssueAccessToken(descriptor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issuedToken.AccessToken);

        Assert.Equal("101", GetClaim(jwt, "tenant_id"));
        Assert.Equal("user", GetClaim(jwt, "client_type"));
        Assert.Equal("5001", GetClaim(jwt, "user_id"));
        Assert.Null(jwt.Claims.SingleOrDefault(x => x.Type == "employee_id"));
    }

    [Fact]
    public void IssueAccessToken_ValidAdminDescriptor_EmitsAdminClientType()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "admin", tenantId: 202, userId: null, employeeId: 7001);

        var issuedToken = issuer.IssueAccessToken(descriptor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issuedToken.AccessToken);

        Assert.Equal("admin", GetClaim(jwt, "client_type"));
        Assert.Equal("202", GetClaim(jwt, "tenant_id"));
        Assert.Equal("7001", GetClaim(jwt, "employee_id"));
    }

    [Fact]
    public void IssueAccessToken_ValidDeviceDescriptor_EmitsDeviceAndTerminalClaims()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(
            clientType: "device",
            tenantId: 101,
            terminalId: 901,
            locationId: 801,
            deviceId: "device-abc");

        var issuedToken = issuer.IssueAccessToken(descriptor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issuedToken.AccessToken);

        Assert.Equal("device", GetClaim(jwt, "client_type"));
        Assert.Equal("901", GetClaim(jwt, "terminal_id"));
        Assert.Equal("801", GetClaim(jwt, "location_id"));
        Assert.Equal("device-abc", GetClaim(jwt, "device_id"));
        Assert.Null(jwt.Claims.SingleOrDefault(x => x.Type == "user_id"));
        Assert.Null(jwt.Claims.SingleOrDefault(x => x.Type == "employee_id"));
    }

    [Fact]
    public void IssueAccessToken_InvalidTenantId_Throws()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "user", tenantId: 0, userId: 5001);

        Assert.Throws<ArgumentOutOfRangeException>(() => issuer.IssueAccessToken(descriptor));
    }

    [Fact]
    public void IssueAccessToken_DeviceWithoutTerminalOrLocation_Throws()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "device", tenantId: 101);

        Assert.Throws<ArgumentException>(() => issuer.IssueAccessToken(descriptor));
    }

    [Fact]
    public void IssueAccessToken_SystemScope_EmitsClaimOnlyWhenTrue()
    {
        var issuer = CreateIssuer();
        var withSystemScope = CreateDescriptor(clientType: "admin", tenantId: 101, employeeId: 7001, systemScope: true);
        var withoutSystemScope = CreateDescriptor(clientType: "admin", tenantId: 101, employeeId: 7001, systemScope: false);

        var jwtWithSystemScope = new JwtSecurityTokenHandler().ReadJwtToken(issuer.IssueAccessToken(withSystemScope).AccessToken);
        var jwtWithoutSystemScope = new JwtSecurityTokenHandler().ReadJwtToken(issuer.IssueAccessToken(withoutSystemScope).AccessToken);

        Assert.Equal("true", GetClaim(jwtWithSystemScope, "system_scope"));
        Assert.Null(jwtWithoutSystemScope.Claims.SingleOrDefault(x => x.Type == "system_scope"));
    }

    [Fact]
    public void IssueAccessToken_DoesNotIncludePlaintextSecretsPasswordsOrRefreshTokens()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "user", tenantId: 101, userId: 5001);

        var issuedToken = issuer.IssueAccessToken(descriptor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issuedToken.AccessToken);
        var claimTypes = jwt.Claims.Select(x => x.Type).ToArray();
        var tokenText = issuedToken.AccessToken;

        Assert.DoesNotContain("password", claimTypes, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", claimTypes, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("device_secret", claimTypes, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", tokenText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh", tokenText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", tokenText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IssueAccessToken_DoesNotRequireDatabase()
    {
        var issuer = CreateIssuer();
        var descriptor = CreateDescriptor(clientType: "user", tenantId: 101, userId: 5001);

        var issuedToken = issuer.IssueAccessToken(descriptor);

        Assert.False(string.IsNullOrWhiteSpace(issuedToken.AccessToken));
        Assert.True(issuedToken.ExpiresInSeconds > 0);
    }

    private static JwtAccessTokenIssuer CreateIssuer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "unit-tests",
                ["Authentication:Jwt:Audience"] = "unit-tests",
                ["Authentication:Jwt:SigningKey"] = "unit-tests-signing-key-should-be-long-enough"
            })
            .Build();

        return new JwtAccessTokenIssuer(configuration);
    }

    private static TokenPrincipalDescriptor CreateDescriptor(
        string clientType,
        int tenantId,
        int? userId = null,
        int? employeeId = null,
        int? terminalId = null,
        int? locationId = null,
        string? deviceId = null,
        bool systemScope = false)
    {
        return new TokenPrincipalDescriptor(
            tenantId,
            clientType,
            userId,
            employeeId,
            terminalId,
            locationId,
            deviceId,
            Roles: new[] { "Reader" },
            Permissions: new[] { "modules.read" },
            SystemScope: systemScope,
            AccessTokenLifetime: TimeSpan.FromMinutes(30));
    }

    private static string? GetClaim(JwtSecurityToken jwt, string claimType) =>
        jwt.Claims.SingleOrDefault(x => x.Type == claimType)?.Value;
}
