using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// Entity Framework Core mapping configuration for the <see cref="LocalOrder"/> entity.
/// </summary>
public class LocalOrderConfiguration : IEntityTypeConfiguration<LocalOrder>
{
    public void Configure(EntityTypeBuilder<LocalOrder> builder)
    {
        builder.ToTable("LocalOrders", t =>
        {
            t.HasCheckConstraint("CK_LocalOrder_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalOrder_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalOrder_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalOrder_EmployeeId", "EmployeeId > 0");
            t.HasCheckConstraint("CK_LocalOrder_SubtotalAmount", "SubtotalAmount >= 0");
            t.HasCheckConstraint("CK_LocalOrder_TotalAmount", "TotalAmount >= 0");
            t.HasCheckConstraint("CK_LocalOrder_PaidAmount", "PaidAmount >= 0");
            t.HasCheckConstraint("CK_LocalOrder_ChangeAmount", "ChangeAmount >= 0");
        });

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

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.CustomerId);

        builder.Property(x => x.OriginalOrderId);

        builder.Property(x => x.BusinessDate)
            .IsRequired();

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.OrderType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.FulfillmentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.CatalogVersion)
            .IsRequired();

        builder.Property(x => x.PriceListId);

        builder.Property(x => x.RuleVersion)
            .IsRequired();

        builder.Property(x => x.ReceiptTemplateId);

        builder.Property(x => x.SubtotalAmount)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .IsRequired();

        builder.Property(x => x.PaidAmount)
            .IsRequired();

        builder.Property(x => x.ChangeAmount)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.GuestName)
            .HasMaxLength(200);

        builder.Property(x => x.GuestPhone)
            .HasMaxLength(50);

        builder.Property(x => x.CompletedOn);
        builder.Property(x => x.VoidedOn);
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

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.Status })
            .HasDatabaseName("IX_LocalOrders_Tenant_Terminal_Status");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_LocalOrders_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNumber })
            .IsUnique()
            .HasDatabaseName("UX_LocalOrders_Tenant_ReceiptNumber");

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_LocalOrders_Tenant_IdempotencyKey");
    }
}
