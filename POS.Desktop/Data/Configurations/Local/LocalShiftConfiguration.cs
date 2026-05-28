using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// Entity Framework Core mapping configuration for the <see cref="LocalShift"/> entity.
/// </summary>
public class LocalShiftConfiguration : IEntityTypeConfiguration<LocalShift>
{
    public void Configure(EntityTypeBuilder<LocalShift> builder)
    {
        builder.ToTable("LocalShifts", t =>
        {
            t.HasCheckConstraint("CK_LocalShift_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalShift_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalShift_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalShift_OpenedByEmployeeId", "OpenedByEmployeeId > 0");
            t.HasCheckConstraint("CK_LocalShift_OpeningCashAmount", "OpeningCashAmount > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

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
            .IsRequired();

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.OpeningCashAmount)
            .IsRequired();

        builder.Property(x => x.ExpectedCashAmount);
        builder.Property(x => x.CountedCashAmount);
        builder.Property(x => x.VarianceAmount);

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
            .HasMaxLength(200);

        builder.Property(x => x.CreatedOn)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.UpdatedOn);

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.Status })
            .HasDatabaseName("IX_LocalShifts_Tenant_Terminal_Status");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_LocalShifts_Tenant_Terminal_Sequence");
    }
}
