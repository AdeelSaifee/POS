using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ItemStockConfiguration : IEntityTypeConfiguration<ItemStock>
{
    public void Configure(EntityTypeBuilder<ItemStock> builder)
    {
        builder.ToTable("ItemStocks");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ItemStocks_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.ItemVariantId)
            .IsRequired();

        builder.Property(x => x.QuantityOnHand)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.QuantityReserved)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.ReorderPoint)
            .HasPrecision(18, 4);

        builder.Property(x => x.LastMovementId);

        builder.Property(x => x.LastMovementOn);

        builder.Property(x => x.StockStatus)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

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

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.ItemVariantId })
            .IsUnique()
            .HasDatabaseName("UX_ItemStocks_Tenant_Location_Variant");

        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId })
            .HasDatabaseName("IX_ItemStocks_Tenant_Variant");

        builder.HasIndex(x => new { x.TenantId, x.StockStatus })
            .HasDatabaseName("IX_ItemStocks_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LocationId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ItemVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
