namespace POS.Api.Contracts.Auth;

public sealed record AuthenticatedUserTokenRequestModel(
    string Username,
    string Password);
