using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class SyncCursorConfiguration : IEntityTypeConfiguration<SyncCursor>
{
    public void Configure(EntityTypeBuilder<SyncCursor> builder)
    {
        builder.ToTable("SyncCursors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.StreamName)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.LastPullToken)
            .HasMaxLength(500);

        builder.Property(x => x.LastSuccessfulPullOn);

        builder.Property(x => x.LastPushedChunkSequence);

        builder.Property(x => x.LastAckedChunkSequence);

        builder.Property(x => x.ServerBackoffUntil);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.LastErrorCode)
            .HasMaxLength(80);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(500);

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

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.StreamName })
            .IsUnique()
            .HasDatabaseName("UX_SyncCursors_Tenant_Terminal_Stream");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_SyncCursors_Status");
    }
}
