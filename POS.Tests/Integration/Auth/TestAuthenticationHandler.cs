using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace POS.Tests.Integration.Auth;

internal sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthenticationDefaults.AuthenticateHeader, out var authenticateValues) ||
            !string.Equals(authenticateValues.ToString(), bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();

        foreach (var header in Request.Headers)
        {
            if (!header.Key.StartsWith(TestAuthenticationDefaults.ClaimHeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var claimType = header.Key[TestAuthenticationDefaults.ClaimHeaderPrefix.Length..];
            var claimValue = header.Value.ToString();

            if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
            {
                continue;
            }

            claims.Add(new Claim(claimType, claimValue));
        }

        var identity = new ClaimsIdentity(claims, TestAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthenticationDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
