using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// EF Core configuration mapping for the <see cref="LocalCashDrawerMovement"/> entity.
/// </summary>
public class LocalCashDrawerMovementConfiguration : IEntityTypeConfiguration<LocalCashDrawerMovement>
{
    public void Configure(EntityTypeBuilder<LocalCashDrawerMovement> builder)
    {
        builder.ToTable("LocalCashDrawerMovements", t =>
        {
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_EmployeeId", "EmployeeId > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_Amount", "Amount > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_ReasonCodeId", "ReasonCodeId > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_IdempotencyKey", "length(IdempotencyKey) > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_CorrelationId", "length(CorrelationId) > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_TerminalSequence", "TerminalSequence > 0");
            t.HasCheckConstraint("CK_LocalCashDrawerMovement_ShiftId", "ShiftId <> '00000000-0000-0000-0000-000000000000'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.LocationId).IsRequired();
        builder.Property(x => x.TerminalId).IsRequired();
        builder.Property(x => x.ShiftId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.ReasonCodeId).IsRequired();
        builder.Property(x => x.BusinessDate).IsRequired();
        builder.Property(x => x.TerminalSequence).IsRequired();

        builder.Property(x => x.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Comment).HasMaxLength(500);
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.SyncedOn);

        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CreatedOn).IsRequired();

        // 1. Unique TenantId + IdempotencyKey
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_LocalCashDrawerMovements_Tenant_IdempotencyKey");

        // 2. TenantId + ShiftId
        builder.HasIndex(x => new { x.TenantId, x.ShiftId })
            .HasDatabaseName("IX_LocalCashDrawerMovements_Tenant_ShiftId");

        // 3. TenantId + LocationId + TerminalId + BusinessDate
        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.TerminalId, x.BusinessDate })
            .HasDatabaseName("IX_LocalCashDrawerMovements_Tenant_Location_Terminal_BusinessDate");

        // 4. TenantId + LocationId + TerminalId + TerminalSequence unique
        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_LocalCashDrawerMovements_Tenant_Location_Terminal_Sequence");

        // 5. TenantId + ReasonCodeId
        builder.HasIndex(x => new { x.TenantId, x.ReasonCodeId })
            .HasDatabaseName("IX_LocalCashDrawerMovements_Tenant_ReasonCodeId");
    }
}
