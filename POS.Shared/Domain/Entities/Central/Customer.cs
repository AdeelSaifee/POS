using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class Customer : GuidTenantScopedEntity
{
    public string? CustomerNumber { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? NormalizedPhone { get; set; }

    public string? Email { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public string CustomerType { get; set; } = string.Empty;

    public string PrivacyStatus { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? CorrelationId { get; set; }
}
