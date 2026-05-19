using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_InventoryMovements_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId);

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.ItemVariantId)
            .IsRequired();

        builder.Property(x => x.SourceOrderId);

        builder.Property(x => x.SourceOrderLineId);

        builder.Property(x => x.ReasonCodeId);

        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence);

        builder.Property(x => x.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.QuantityDelta)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitOfMeasureId)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 4);

        builder.Property(x => x.StockBefore)
            .HasPrecision(18, 4);

        builder.Property(x => x.StockAfter)
            .HasPrecision(18, 4);

        builder.Property(x => x.ExceptionStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ExceptionDetailsJson);

        builder.Property(x => x.OccurredOn)
            .IsRequired();

        builder.Property(x => x.AppliedOn);

        builder.Property(x => x.SyncedOn);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedOn)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedOn);

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_InventoryMovements_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_InventoryMovements_Tenant_Item_Location_Date");

        builder.HasIndex(x => new { x.TenantId, x.SourceOrderLineId })
            .HasDatabaseName("IX_InventoryMovements_Tenant_SourceOrderLine");

        builder.HasIndex(x => new { x.TenantId, x.ExceptionStatus })
            .HasDatabaseName("IX_InventoryMovements_Tenant_ExceptionStatus");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_InventoryMovements_Tenant_CorrelationId");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LocationId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Terminal>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TerminalId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ItemVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UnitOfMeasureId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SourceOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SourceOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReasonCodeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AuthorizedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
