using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Location : TenantScopedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public LocationType LocationType { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? CountryCode { get; set; }

    public string? Phone { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public int? DefaultPriceListId { get; set; }

    public int? DefaultReceiptTemplateId { get; set; }

    public TimeSpan? BusinessDayStartTime { get; set; }

    public bool AllowsNegativeStock { get; set; }
}
