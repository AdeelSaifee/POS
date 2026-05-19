using POS.Shared.Enums;

namespace POS.Api.Contracts.Tenant;

public sealed record TenantProfileDto(
    int Id,
    string Code,
    string Name,
    string? LogoUrl,
    CompanyStatus Status);
