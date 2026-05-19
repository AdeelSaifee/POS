using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ReceiptTemplateConfiguration : IEntityTypeConfiguration<ReceiptTemplate>
{
    public void Configure(EntityTypeBuilder<ReceiptTemplate> builder)
    {
        builder.ToTable("ReceiptTemplates");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ReceiptTemplates_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TemplateCode)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.TemplateVersion)
            .IsRequired();

        builder.Property(x => x.ContentFormat)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.TemplateContent)
            .IsRequired();

        builder.Property(x => x.ContentHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EffectiveFrom)
            .IsRequired();

        builder.Property(x => x.EffectiveTo);

        builder.Property(x => x.IsDefault)
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

        builder.HasIndex(x => new { x.TenantId, x.TemplateCode, x.TemplateVersion })
            .IsUnique()
            .HasDatabaseName("UX_ReceiptTemplates_Tenant_Code_Version");

        builder.HasIndex(x => new { x.TenantId, x.TemplateCode, x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_ReceiptTemplates_Tenant_Effective");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
