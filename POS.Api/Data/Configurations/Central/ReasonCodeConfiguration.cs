using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ReasonCodeConfiguration : IEntityTypeConfiguration<ReasonCode>
{
    public void Configure(EntityTypeBuilder<ReasonCode> builder)
    {
        builder.ToTable("ReasonCodes");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ReasonCodes_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ReasonCategory)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(x => x.RequiresManagerApproval)
            .IsRequired();

        builder.Property(x => x.RequiresComment)
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

        builder.HasIndex(x => new { x.TenantId, x.ReasonCategory, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_ReasonCodes_Tenant_Category_Code");

        builder.HasIndex(x => new { x.TenantId, x.ReasonCategory, x.IsActive })
            .HasDatabaseName("IX_ReasonCodes_Tenant_Category_Active");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
