namespace POS.Api.Contracts.Auth;

public sealed record TokenIssueResultDto(
    bool Succeeded,
    AccessTokenResponseDto? AccessToken,
    string? FailureReason);
