using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class LocalTerminalSessionConfiguration : IEntityTypeConfiguration<LocalTerminalSession>
{
    public void Configure(EntityTypeBuilder<LocalTerminalSession> builder)
    {
        builder.ToTable("LocalTerminalSessions", t =>
        {
            t.HasCheckConstraint("CK_LocalTerminalSession_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalTerminalSession_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalTerminalSession_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalTerminalSession_EmployeeId", "EmployeeId > 0");
            t.HasCheckConstraint("CK_LocalTerminalSession_EmployeeNumber_NotEmpty", "EmployeeNumber <> ''");
            t.HasCheckConstraint("CK_LocalTerminalSession_DisplayName_NotEmpty", "DisplayName <> ''");
            t.HasCheckConstraint("CK_LocalTerminalSession_Role_NotEmpty", "Role <> ''");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.BusinessDate)
            .IsRequired();

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.LoggedInOn)
            .IsRequired();

        builder.Property(x => x.LoggedOutOn);

        builder.Property(x => x.MetadataJson);

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_LocalTerminalSessions_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .HasDatabaseName("IX_LocalTerminalSessions_Tenant_Employee");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_LocalTerminalSessions_Tenant_Status");
    }
}
