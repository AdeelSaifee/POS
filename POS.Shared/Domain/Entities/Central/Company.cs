using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Company : TenantRootEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public string? LogoUrl { get; set; }

    public string DefaultCurrencyCode { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public CompanyStatus Status { get; set; }
}
