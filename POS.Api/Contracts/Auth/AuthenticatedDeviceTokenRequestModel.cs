namespace POS.Api.Contracts.Auth;

public sealed record AuthenticatedDeviceTokenRequestModel(
    string DeviceCredentialIdentifier,
    string DeviceCredentialSecret);
