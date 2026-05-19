namespace POS.Api.Application.Auth;

internal sealed record CredentialValidationResult(
    bool Succeeded,
    TokenPrincipalDescriptor? Principal,
    CredentialValidationFailureReason FailureReason)
{
    public static CredentialValidationResult Success(TokenPrincipalDescriptor principal) =>
        new(true, principal, CredentialValidationFailureReason.None);

    public static CredentialValidationResult Fail(CredentialValidationFailureReason failureReason) =>
        new(false, null, failureReason);
}
