using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ItemPriceConfiguration : IEntityTypeConfiguration<ItemPrice>
{
    public void Configure(EntityTypeBuilder<ItemPrice> builder)
    {
        builder.ToTable("ItemPrices");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ItemPrices_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.PriceListId)
            .IsRequired();

        builder.Property(x => x.ItemVariantId)
            .IsRequired();

        builder.Property(x => x.UnitOfMeasureId)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CompareAtPrice)
            .HasPrecision(18, 4);

        builder.Property(x => x.MinimumQuantity)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.EffectiveFrom)
            .IsRequired();

        builder.Property(x => x.EffectiveTo);

        builder.Property(x => x.IsTaxIncluded)
            .IsRequired();

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

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.ItemVariantId, x.MinimumQuantity, x.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("UX_ItemPrices_Tenant_List_Variant_Qty_From");

        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId })
            .HasDatabaseName("IX_ItemPrices_Tenant_Variant");

        builder.HasIndex(x => new { x.TenantId, x.PriceListId, x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_ItemPrices_Tenant_List_Effective");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PriceListId })
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
    }
}
