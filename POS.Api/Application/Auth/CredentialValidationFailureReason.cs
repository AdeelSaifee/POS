namespace POS.Api.Application.Auth;

internal enum CredentialValidationFailureReason
{
    None = 0,
    InvalidCredentials = 1,
    CredentialExpired = 2,
    AccountLocked = 3,
    NotProvisioned = 4,
    UnsupportedCredentialType = 5
}
