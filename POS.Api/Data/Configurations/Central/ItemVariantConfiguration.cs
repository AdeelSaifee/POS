using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ItemVariantConfiguration : IEntityTypeConfiguration<ItemVariant>
{
    public void Configure(EntityTypeBuilder<ItemVariant> builder)
    {
        builder.ToTable("ItemVariants");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ItemVariants_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.VariantCode)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.SKU)
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SizeText)
            .HasMaxLength(100);

        builder.Property(x => x.WeightValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.WeightUnitOfMeasureId);

        builder.Property(x => x.UnitOfMeasureId)
            .IsRequired();

        builder.Property(x => x.TaxRuleId);

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.Property(x => x.IsSellable)
            .IsRequired();

        builder.Property(x => x.IsPurchasable)
            .IsRequired();

        builder.Property(x => x.CatalogVersion)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.MetadataJson);

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

        builder.HasIndex(x => new { x.TenantId, x.ItemId, x.VariantCode })
            .IsUnique()
            .HasDatabaseName("UX_ItemVariants_Tenant_Item_VariantCode");

        builder.HasIndex(x => new { x.TenantId, x.SKU })
            .IsUnique()
            .HasFilter("[SKU] IS NOT NULL")
            .HasDatabaseName("UX_ItemVariants_Tenant_SKU");

        builder.HasIndex(x => new { x.TenantId, x.ItemId })
            .HasDatabaseName("IX_ItemVariants_Tenant_Item");

        builder.HasIndex(x => new { x.TenantId, x.WeightUnitOfMeasureId })
            .HasDatabaseName("IX_ItemVariants_Tenant_WeightUnit");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_ItemVariants_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UnitOfMeasureId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.WeightUnitOfMeasureId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaxRule>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TaxRuleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
