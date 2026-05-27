using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class LocalEmployeeConfiguration : IEntityTypeConfiguration<LocalEmployee>
{
    public void Configure(EntityTypeBuilder<LocalEmployee> builder)
    {
        builder.ToTable("LocalEmployees", t =>
        {
            t.HasCheckConstraint("CK_LocalEmployee_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalEmployee_EmployeeNumber_NotEmpty", "EmployeeNumber <> ''");
            t.HasCheckConstraint("CK_LocalEmployee_DisplayName_NotEmpty", "DisplayName <> ''");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

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

        builder.HasIndex(x => new { x.TenantId, x.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("UX_LocalEmployees_Tenant_EmployeeNumber");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_LocalEmployees_Tenant_Status");
    }
}

public class LocalEmployeeLocationRoleConfiguration : IEntityTypeConfiguration<LocalEmployeeLocationRole>
{
    public void Configure(EntityTypeBuilder<LocalEmployeeLocationRole> builder)
    {
        builder.ToTable("LocalEmployeeLocationRoles", t =>
        {
            t.HasCheckConstraint("CK_LocalEmployeeLocationRole_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalEmployeeLocationRole_EmployeeId", "EmployeeId > 0");
            t.HasCheckConstraint("CK_LocalEmployeeLocationRole_Role_NotEmpty", "Role <> ''");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

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

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.LocationId, x.Role })
            .IsUnique()
            .HasFilter("LocationId IS NOT NULL")
            .HasDatabaseName("UX_LocalEmployeeLocationRoles_Scoped");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Role })
            .IsUnique()
            .HasFilter("LocationId IS NULL")
            .HasDatabaseName("UX_LocalEmployeeLocationRoles_TenantWide");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.Role })
            .HasDatabaseName("IX_LocalEmployeeLocationRoles_Tenant_Location_Role");
    }
}
