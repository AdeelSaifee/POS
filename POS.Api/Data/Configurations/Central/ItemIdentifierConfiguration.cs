using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ItemIdentifierConfiguration : IEntityTypeConfiguration<ItemIdentifier>
{
    public void Configure(EntityTypeBuilder<ItemIdentifier> builder)
    {
        builder.ToTable("ItemIdentifiers");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ItemIdentifiers_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.ItemVariantId);

        builder.Property(x => x.IdentifierType)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.IdentifierValue)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.StartsOn);

        builder.Property(x => x.EndsOn);

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

        builder.HasIndex(x => new { x.TenantId, x.IdentifierType, x.IdentifierValue })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_ItemIdentifiers_Active_Type_Value");

        builder.HasIndex(x => new { x.TenantId, x.ItemId })
            .HasDatabaseName("IX_ItemIdentifiers_Tenant_Item");

        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId })
            .HasDatabaseName("IX_ItemIdentifiers_Tenant_Variant");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
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
    }
}
