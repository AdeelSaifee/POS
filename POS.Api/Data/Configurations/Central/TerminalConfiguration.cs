using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("Terminals");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Terminals_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DeviceId)
            .IsRequired();

        builder.Property(x => x.DeviceSecretHash)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.ProvisioningStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.LastSeenOn);

        builder.Property(x => x.LastCatalogVersion);

        builder.Property(x => x.LastPriceListId);

        builder.Property(x => x.LastRuleVersion);

        builder.Property(x => x.LastReceiptTemplateId);

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

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.TerminalCode })
            .IsUnique()
            .HasDatabaseName("UX_Terminals_Tenant_Location_Code");

        builder.HasIndex(x => new { x.TenantId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName("UX_Terminals_Tenant_DeviceId");

        builder.HasIndex(x => new { x.TenantId, x.ProvisioningStatus })
            .HasDatabaseName("IX_Terminals_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LocationId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LastPriceListId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LastReceiptTemplateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
