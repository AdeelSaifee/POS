using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class SyncOutboxConfiguration : IEntityTypeConfiguration<SyncOutbox>
{
    public void Configure(EntityTypeBuilder<SyncOutbox> builder)
    {
        builder.ToTable("SyncOutbox");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .IsRequired();

        builder.Property(x => x.PayloadHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ChunkSequence);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.LastAttemptOn);

        builder.Property(x => x.AckedOn);

        builder.Property(x => x.LastErrorCode)
            .HasMaxLength(80);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(500);

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
            .HasDatabaseName("UX_SyncOutbox_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence, x.EventType, x.EventId })
            .IsUnique()
            .HasDatabaseName("UX_SyncOutbox_Tenant_Terminal_Sequence_Event");

        builder.HasIndex(x => new { x.Status, x.BusinessDate, x.TerminalSequence })
            .HasDatabaseName("IX_SyncOutbox_Status_Order");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_SyncOutbox_CorrelationId");
    }
}
