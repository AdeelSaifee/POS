using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Auth;
using POS.Api.Configuration;

namespace POS.Api.Application.Auth;

internal sealed class JwtAccessTokenIssuer : IJwtAccessTokenIssuer
{
    private static readonly HashSet<string> AllowedClientTypes = new(StringComparer.Ordinal)
    {
        "user",
        "admin",
        "device"
    };

    private static readonly TimeSpan MaximumAccessTokenLifetime = TimeSpan.FromHours(8);
    private readonly JwtAuthenticationConfiguration _jwtConfiguration;

    public JwtAccessTokenIssuer(IConfiguration configuration)
    {
        _jwtConfiguration = JwtAuthenticationConfigurationGuard.GetRequiredConfiguration(configuration);
    }

    public IssuedAccessToken IssueAccessToken(TokenPrincipalDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);

        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.Add(descriptor.AccessTokenLifetime);
        var claims = BuildClaims(descriptor);

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtConfiguration.Issuer,
            audience: _jwtConfiguration.Audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new IssuedAccessToken(
            accessToken,
            expiresAtUtc,
            (int)descriptor.AccessTokenLifetime.TotalSeconds);
    }

    private static void ValidateDescriptor(TokenPrincipalDescriptor descriptor)
    {
        if (descriptor.TenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(descriptor), "TenantId must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.ClientType) || !AllowedClientTypes.Contains(descriptor.ClientType))
        {
            throw new ArgumentException("ClientType must be one of: user, admin, device.", nameof(descriptor));
        }

        if (descriptor.AccessTokenLifetime <= TimeSpan.Zero || descriptor.AccessTokenLifetime > MaximumAccessTokenLifetime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                $"AccessTokenLifetime must be greater than zero and no more than {MaximumAccessTokenLifetime.TotalHours:0} hours.");
        }

        if (descriptor.ClientType == "device")
        {
            if (!descriptor.TerminalId.HasValue || descriptor.TerminalId.Value <= 0)
            {
                throw new ArgumentException("Device tokens require a positive TerminalId.", nameof(descriptor));
            }

            if (!descriptor.LocationId.HasValue || descriptor.LocationId.Value <= 0)
            {
                throw new ArgumentException("Device tokens require a positive LocationId.", nameof(descriptor));
            }

            if (descriptor.UserId.HasValue || descriptor.EmployeeId.HasValue)
            {
                throw new ArgumentException("Device tokens must not include user/admin identity claims.", nameof(descriptor));
            }
        }
        else if (!descriptor.UserId.HasValue && !descriptor.EmployeeId.HasValue)
        {
            throw new ArgumentException("User/admin tokens require UserId or EmployeeId.", nameof(descriptor));
        }
    }

    private static IReadOnlyList<Claim> BuildClaims(TokenPrincipalDescriptor descriptor)
    {
        var claims = new List<Claim>
        {
            new(ApiClaimTypes.TenantId, descriptor.TenantId.ToString()),
            new(ApiClaimTypes.ClientType, descriptor.ClientType)
        };

        if (descriptor.UserId.HasValue)
        {
            claims.Add(new Claim(ApiClaimTypes.UserId, descriptor.UserId.Value.ToString()));
        }

        if (descriptor.EmployeeId.HasValue)
        {
            claims.Add(new Claim(ApiClaimTypes.EmployeeId, descriptor.EmployeeId.Value.ToString()));
        }

        if (descriptor.TerminalId.HasValue)
        {
            claims.Add(new Claim(ApiClaimTypes.TerminalId, descriptor.TerminalId.Value.ToString()));
        }

        if (descriptor.LocationId.HasValue)
        {
            claims.Add(new Claim(ApiClaimTypes.LocationId, descriptor.LocationId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.DeviceId))
        {
            claims.Add(new Claim(ApiClaimTypes.DeviceId, descriptor.DeviceId));
        }

        if (descriptor.SystemScope)
        {
            claims.Add(new Claim(ApiClaimTypes.SystemScope, bool.TrueString.ToLowerInvariant()));
        }

        if (descriptor.Roles is not null)
        {
            foreach (var role in descriptor.Roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (descriptor.Permissions is not null)
        {
            foreach (var permission in descriptor.Permissions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        return claims;
    }
}
