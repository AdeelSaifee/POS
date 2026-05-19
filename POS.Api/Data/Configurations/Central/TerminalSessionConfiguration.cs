using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class TerminalSessionConfiguration : IEntityTypeConfiguration<TerminalSession>
{
    public void Configure(EntityTypeBuilder<TerminalSession> builder)
    {
        builder.ToTable("TerminalSessions");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_TerminalSessions_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

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

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_TerminalSessions_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId, x.BusinessDate })
            .HasDatabaseName("IX_TerminalSessions_Tenant_Employee_Date");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_TerminalSessions_Tenant_Location_Date");

        builder.HasIndex(x => new { x.TenantId, x.ShiftId })
            .HasDatabaseName("IX_TerminalSessions_Tenant_Shift");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_TerminalSessions_Tenant_Status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.LocationId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Terminal>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TerminalId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
