using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class PrintQueueConfiguration : IEntityTypeConfiguration<PrintQueue>
{
    public void Configure(EntityTypeBuilder<PrintQueue> builder)
    {
        builder.ToTable("PrintQueue");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.OrderId);

        builder.Property(x => x.ZReportId);

        builder.Property(x => x.PrintJobType)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(100);

        builder.Property(x => x.ReceiptTemplateId);

        builder.Property(x => x.PayloadJson)
            .IsRequired();

        builder.Property(x => x.RenderedContent);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.LastAttemptOn);

        builder.Property(x => x.PrintedOn);

        builder.Property(x => x.LastErrorCode)
            .HasMaxLength(80);

        builder.Property(x => x.LastErrorMessage)
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
            .HasDatabaseName("UX_PrintQueue_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.Status, x.Priority, x.CreatedOn })
            .HasDatabaseName("IX_PrintQueue_Status_Priority");

        builder.HasIndex(x => new { x.TenantId, x.OrderId })
            .HasDatabaseName("IX_PrintQueue_Tenant_Order");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_PrintQueue_CorrelationId");
    }
}
