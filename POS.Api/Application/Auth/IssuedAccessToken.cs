namespace POS.Api.Application.Auth;

internal sealed record IssuedAccessToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    int ExpiresInSeconds);
