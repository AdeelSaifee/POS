namespace POS.Api.Configuration;

public static class JwtAuthenticationConfigurationGuard
{
    public static JwtAuthenticationConfiguration GetRequiredConfiguration(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Authentication:Jwt");

        var issuer = jwtSection["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "Missing required configuration key 'Authentication:Jwt:Issuer'.");
        }

        var audience = jwtSection["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Missing required configuration key 'Authentication:Jwt:Audience'.");
        }

        var signingKey = jwtSection["SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Missing required configuration key 'Authentication:Jwt:SigningKey'.");
        }

        return new JwtAuthenticationConfiguration(issuer, audience, signingKey);
    }
}

public sealed record JwtAuthenticationConfiguration(
    string Issuer,
    string Audience,
    string SigningKey);
