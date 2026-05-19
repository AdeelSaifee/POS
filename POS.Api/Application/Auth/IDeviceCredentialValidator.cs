namespace POS.Api.Application.Auth;

internal interface IDeviceCredentialValidator : IAuthApplicationService
{
    Task<CredentialValidationResult> ValidateAsync(
        string credentialIdentifier,
        string credentialSecret,
        CancellationToken cancellationToken);
}
