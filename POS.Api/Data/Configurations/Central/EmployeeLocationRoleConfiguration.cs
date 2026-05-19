using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class EmployeeLocationRoleConfiguration : IEntityTypeConfiguration<EmployeeLocationRole>
{
    public void Configure(EntityTypeBuilder<EmployeeLocationRole> builder)
    {
        builder.ToTable("EmployeeLocationRoles");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_EmployeeLocationRoles_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.LocationId);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.PermissionSetCode)
            .HasMaxLength(100);

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

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.LocationId, x.Role })
            .IsUnique()
            .HasFilter("[LocationId] IS NOT NULL")
            .HasDatabaseName("UX_EmployeeLocationRoles_Scoped");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Role })
            .IsUnique()
            .HasFilter("[LocationId] IS NULL")
            .HasDatabaseName("UX_EmployeeLocationRoles_TenantWide");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.Role })
            .HasDatabaseName("IX_EmployeeLocationRoles_Tenant_Location_Role");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LocationId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
