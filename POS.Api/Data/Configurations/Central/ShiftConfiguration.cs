using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Shifts_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.OpenedByEmployeeId)
            .IsRequired();

        builder.Property(x => x.ClosedByEmployeeId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.OpeningCashAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.ExpectedCashAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.CountedCashAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.VarianceAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.OpenedOn)
            .IsRequired();

        builder.Property(x => x.ClosedOn);

        builder.Property(x => x.SyncedOn);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
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

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_Shifts_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_Shifts_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_Shifts_Tenant_Location_Date");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Shifts_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_Shifts_Tenant_CorrelationId");

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
            .HasForeignKey(x => new { x.TenantId, x.OpenedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ClosedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
