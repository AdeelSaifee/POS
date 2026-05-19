using POS.Shared.Domain.Base;

namespace POS.Shared.Domain.Entities.Central;

public class ItemStock : TenantScopedEntity
{
    public int LocationId { get; set; }

    public int ItemVariantId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal? ReorderPoint { get; set; }

    public Guid? LastMovementId { get; set; }

    public DateTimeOffset? LastMovementOn { get; set; }

    public string StockStatus { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];
}
