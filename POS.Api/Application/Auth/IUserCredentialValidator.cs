namespace POS.Api.Application.Auth;

internal interface IUserCredentialValidator : IAuthApplicationService
{
    Task<CredentialValidationResult> ValidateAsync(
        string username,
        string password,
        CancellationToken cancellationToken);
}
