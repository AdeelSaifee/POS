namespace POS.Api.Application.Auth;

internal sealed record TokenPrincipalDescriptor(
    int TenantId,
    string ClientType,
    int? UserId,
    int? EmployeeId,
    int? TerminalId,
    int? LocationId,
    string? DeviceId,
    IReadOnlyCollection<string>? Roles,
    IReadOnlyCollection<string>? Permissions,
    bool SystemScope,
    TimeSpan AccessTokenLifetime);
