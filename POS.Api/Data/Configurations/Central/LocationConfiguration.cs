using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Locations_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.LocationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.AddressLine1)
            .HasMaxLength(200);

        builder.Property(x => x.AddressLine2)
            .HasMaxLength(200);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.Region)
            .HasMaxLength(100);

        builder.Property(x => x.PostalCode)
            .HasMaxLength(30);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(2)
            .IsFixedLength();

        builder.Property(x => x.Phone)
            .HasMaxLength(40);

        builder.Property(x => x.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DefaultPriceListId);

        builder.Property(x => x.DefaultReceiptTemplateId);

        builder.Property(x => x.BusinessDayStartTime)
            .HasColumnType("time");

        builder.Property(x => x.AllowsNegativeStock)
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

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_Locations_Tenant_Code");

        builder.HasIndex(x => new { x.TenantId, x.LocationType })
            .HasDatabaseName("IX_Locations_Tenant_Type");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DefaultPriceListId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DefaultReceiptTemplateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
