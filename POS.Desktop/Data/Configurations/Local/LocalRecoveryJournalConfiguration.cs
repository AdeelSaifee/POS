using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class LocalRecoveryJournalConfiguration : IEntityTypeConfiguration<LocalRecoveryJournal>
{
    public void Configure(EntityTypeBuilder<LocalRecoveryJournal> builder)
    {
        builder.ToTable("LocalRecoveryJournal");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.OrderId);

        builder.Property(x => x.PaymentId);

        builder.Property(x => x.RecoveryType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(80);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.StatePayloadJson)
            .IsRequired();

        builder.Property(x => x.RequiredAction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(120);

        builder.Property(x => x.ResolvedByEmployeeId);

        builder.Property(x => x.ResolvedOn);

        builder.Property(x => x.ResolutionComment)
            .HasMaxLength(500);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.RetainUntil);

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
            .HasDatabaseName("UX_LocalRecoveryJournal_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.Status, x.RecoveryType })
            .HasDatabaseName("IX_LocalRecoveryJournal_Status_Type");

        builder.HasIndex(x => new { x.TenantId, x.OrderId })
            .HasDatabaseName("IX_LocalRecoveryJournal_Tenant_Order");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_LocalRecoveryJournal_CorrelationId");
    }
}
