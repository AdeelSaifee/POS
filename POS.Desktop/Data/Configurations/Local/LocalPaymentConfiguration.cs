using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// Entity Framework Core mapping configuration for the <see cref="LocalPayment"/> entity.
/// </summary>
public class LocalPaymentConfiguration : IEntityTypeConfiguration<LocalPayment>
{
    public void Configure(EntityTypeBuilder<LocalPayment> builder)
    {
        builder.ToTable("LocalPayments", t =>
        {
            t.HasCheckConstraint("CK_LocalPayment_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalPayment_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalPayment_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalPayment_TenderMethodId", "TenderMethodId > 0");
            t.HasCheckConstraint("CK_LocalPayment_Amount", "Amount > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

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
            .IsRequired();

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.PaymentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.AuthorizedAmount);
        builder.Property(x => x.CapturedAmount);

        builder.Property(x => x.PaymentToken)
            .HasMaxLength(200);

        builder.Property(x => x.ExternalPaymentReference)
            .HasMaxLength(200);

        builder.Property(x => x.AuthorizationCode)
            .HasMaxLength(100);

        builder.Property(x => x.CardBrand)
            .HasMaxLength(50);

        builder.Property(x => x.CardLast4)
            .HasMaxLength(10);

        builder.Property(x => x.FailureCode)
            .HasMaxLength(100);

        builder.Property(x => x.FailureMessage)
            .HasMaxLength(500);

        builder.Property(x => x.RequiresReconciliation)
            .IsRequired();

        builder.Property(x => x.ReconciledOn);

        builder.Property(x => x.ProcessedOn)
            .IsRequired();

        builder.Property(x => x.SyncedOn);
        builder.Property(x => x.MetadataJson);

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

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_LocalPayments_OrderId");

        builder.HasIndex(x => new { x.TenantId, x.OrderId, x.TenderMethodId })
            .HasDatabaseName("IX_LocalPayments_Tenant_OrderId_TenderMethod");
    }
}
