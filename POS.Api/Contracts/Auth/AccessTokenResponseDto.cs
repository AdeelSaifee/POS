namespace POS.Api.Contracts.Auth;

public sealed record AccessTokenResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds);
