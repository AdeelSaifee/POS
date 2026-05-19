using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class ItemIdentifier : TenantScopedEntity
{
    public int ItemId { get; set; }

    public int? ItemVariantId { get; set; }

    public string IdentifierType { get; set; } = string.Empty;

    public string IdentifierValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTimeOffset? StartsOn { get; set; }

    public DateTimeOffset? EndsOn { get; set; }
}
