using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class LocalRetentionStateConfiguration : IEntityTypeConfiguration<LocalRetentionState>
{
    public void Configure(EntityTypeBuilder<LocalRetentionState> builder)
    {
        builder.ToTable("LocalRetentionState");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.RetentionDays)
            .IsRequired();

        builder.Property(x => x.LastCleanupOn);

        builder.Property(x => x.OldestRetainedBusinessDate)
            .HasColumnType("TEXT");

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

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.Category })
            .IsUnique()
            .HasDatabaseName("UX_LocalRetentionState_Tenant_Terminal_Category");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_LocalRetentionState_Status");
    }
}
