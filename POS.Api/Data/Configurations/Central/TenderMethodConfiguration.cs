using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class TenderMethodConfiguration : IEntityTypeConfiguration<TenderMethod>
{
    public void Configure(EntityTypeBuilder<TenderMethod> builder)
    {
        builder.ToTable("TenderMethods");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_TenderMethods_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.TenderType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.RequiresExternalReference)
            .IsRequired();

        builder.Property(x => x.AllowsChange)
            .IsRequired();

        builder.Property(x => x.AllowsRefund)
            .IsRequired();

        builder.Property(x => x.RequiresOnlineAuthorization)
            .IsRequired();

        builder.Property(x => x.SortOrder)
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
            .HasDatabaseName("UX_TenderMethods_Tenant_Code");

        builder.HasIndex(x => new { x.TenantId, x.TenderType })
            .HasDatabaseName("IX_TenderMethods_Tenant_Type");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
