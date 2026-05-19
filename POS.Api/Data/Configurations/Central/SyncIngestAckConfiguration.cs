using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class SyncIngestAckConfiguration : IEntityTypeConfiguration<SyncIngestAck>
{
    public void Configure(EntityTypeBuilder<SyncIngestAck> builder)
    {
        builder.ToTable("SyncIngestAcks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_SyncIngestAcks_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ChunkSequence)
            .IsRequired();

        builder.Property(x => x.ChunkIdempotencyKey)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.RequestHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EventCount)
            .IsRequired();

        builder.Property(x => x.FirstBusinessDate)
            .HasColumnType("date");

        builder.Property(x => x.LastBusinessDate)
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.AckPayloadJson)
            .IsRequired();

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(80);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.ReceivedOn)
            .IsRequired();

        builder.Property(x => x.ExpiresOn)
            .IsRequired();

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

        builder.HasIndex(x => new { x.TenantId, x.ChunkIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_SyncIngestAcks_Tenant_ChunkKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.ChunkSequence })
            .IsUnique()
            .HasDatabaseName("UX_SyncIngestAcks_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.ExpiresOn })
            .HasDatabaseName("IX_SyncIngestAcks_Tenant_ExpiresOn");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_SyncIngestAcks_Tenant_CorrelationId");

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
    }
}
