using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Employees_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.UserName)
            .HasMaxLength(150);

        builder.Property(x => x.Email)
            .HasMaxLength(254);

        builder.Property(x => x.Phone)
            .HasMaxLength(40);

        builder.Property(x => x.PinHash)
            .HasMaxLength(300);

        builder.Property(x => x.PinSalt)
            .HasMaxLength(200);

        builder.Property(x => x.PinHashAlgorithm)
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.MustChangePin)
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

        builder.HasIndex(x => new { x.TenantId, x.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("UX_Employees_Tenant_EmployeeNumber");

        builder.HasIndex(x => new { x.TenantId, x.UserName })
            .IsUnique()
            .HasFilter("[UserName] IS NOT NULL")
            .HasDatabaseName("UX_Employees_Tenant_UserName");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Employees_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
