using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Items_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CategoryId);

        builder.Property(x => x.ItemCode)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.BrandName)
            .HasMaxLength(150);

        builder.Property(x => x.ManufacturerName)
            .HasMaxLength(150);

        builder.Property(x => x.ItemType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.IsTrackedInventory)
            .IsRequired();

        builder.Property(x => x.DefaultUnitOfMeasureId)
            .IsRequired();

        builder.Property(x => x.DefaultTaxRuleId);

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

        builder.HasIndex(x => new { x.TenantId, x.ItemCode })
            .IsUnique()
            .HasDatabaseName("UX_Items_Tenant_ItemCode");

        builder.HasIndex(x => new { x.TenantId, x.CategoryId })
            .HasDatabaseName("IX_Items_Tenant_Category");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_Items_Tenant_Name");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Items_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CategoryId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DefaultUnitOfMeasureId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaxRule>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DefaultTaxRuleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
