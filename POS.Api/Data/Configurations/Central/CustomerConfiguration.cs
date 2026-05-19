using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Customers_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CustomerNumber)
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NormalizedPhone)
            .HasMaxLength(40);

        builder.Property(x => x.Email)
            .HasMaxLength(254);

        builder.Property(x => x.TaxRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.CustomerType)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.PrivacyStatus)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

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

        builder.HasIndex(x => new { x.TenantId, x.CustomerNumber })
            .IsUnique()
            .HasFilter("[CustomerNumber] IS NOT NULL")
            .HasDatabaseName("UX_Customers_Tenant_CustomerNumber");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedPhone })
            .HasFilter("[NormalizedPhone] IS NOT NULL")
            .HasDatabaseName("IX_Customers_Tenant_NormalizedPhone");

        builder.HasIndex(x => new { x.TenantId, x.DisplayName })
            .HasDatabaseName("IX_Customers_Tenant_DisplayName");

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_Customers_Tenant_IdempotencyKey");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
