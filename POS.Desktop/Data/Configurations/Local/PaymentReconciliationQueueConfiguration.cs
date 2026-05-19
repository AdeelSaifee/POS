using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class PaymentReconciliationQueueConfiguration : IEntityTypeConfiguration<PaymentReconciliationQueue>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationQueue> builder)
    {
        builder.ToTable("PaymentReconciliationQueue");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.PaymentId)
            .IsRequired();

        builder.Property(x => x.TenderMethodId)
            .IsRequired();

        builder.Property(x => x.ExternalPaymentReference)
            .HasMaxLength(200);

        builder.Property(x => x.PaymentToken)
            .HasMaxLength(300);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.NextAttemptOn);

        builder.Property(x => x.LastAttemptOn);

        builder.Property(x => x.LastResultCode)
            .HasMaxLength(80);

        builder.Property(x => x.LastResultMessage)
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
            .HasDatabaseName("UX_PaymentReconciliationQueue_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.PaymentId })
            .IsUnique()
            .HasDatabaseName("UX_PaymentReconciliationQueue_Tenant_Payment");

        builder.HasIndex(x => new { x.Status, x.NextAttemptOn })
            .HasDatabaseName("IX_PaymentReconciliationQueue_Status_NextAttempt");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_PaymentReconciliationQueue_CorrelationId");
    }
}
