using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Payments_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.TenderMethodId)
            .IsRequired();

        builder.Property(x => x.OriginalPaymentId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.PaymentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(x => x.AuthorizedAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.CapturedAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.PaymentToken)
            .HasMaxLength(300);

        builder.Property(x => x.ExternalPaymentReference)
            .HasMaxLength(200);

        builder.Property(x => x.AuthorizationCode)
            .HasMaxLength(100);

        builder.Property(x => x.CardBrand)
            .HasMaxLength(40);

        builder.Property(x => x.CardLast4)
            .HasMaxLength(4)
            .IsFixedLength();

        builder.Property(x => x.FailureCode)
            .HasMaxLength(80);

        builder.Property(x => x.FailureMessage)
            .HasMaxLength(500);

        builder.Property(x => x.RequiresReconciliation)
            .IsRequired();

        builder.Property(x => x.ReconciledOn);

        builder.Property(x => x.ProcessedOn)
            .IsRequired();

        builder.Property(x => x.SyncedOn);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MetadataJson);

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
            .HasDatabaseName("UX_Payments_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_Payments_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.OrderId })
            .HasDatabaseName("IX_Payments_Tenant_Order");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Payments_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.ExternalPaymentReference })
            .HasFilter("[ExternalPaymentReference] IS NOT NULL")
            .HasDatabaseName("IX_Payments_Tenant_ExternalReference");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_Payments_Tenant_CorrelationId");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
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

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TenderMethod>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TenderMethodId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OriginalPaymentId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
