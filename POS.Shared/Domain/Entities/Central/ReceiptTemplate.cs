using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class ReceiptTemplate : TenantScopedEntity
{
    public string TemplateCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public long TemplateVersion { get; set; }

    public ReceiptContentFormat ContentFormat { get; set; }

    public string TemplateContent { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public bool IsDefault { get; set; }
}
