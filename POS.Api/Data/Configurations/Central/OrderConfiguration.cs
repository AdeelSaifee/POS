using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Orders_Tenant_Id");

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
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OrderType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.FulfillmentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CatalogVersion)
            .IsRequired();

        builder.Property(x => x.PriceListId);

        builder.Property(x => x.RuleVersion)
            .IsRequired();

        builder.Property(x => x.ReceiptTemplateId);

        builder.Property(x => x.SubtotalAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.PaidAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.ChangeAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(x => x.GuestName)
            .HasMaxLength(200);

        builder.Property(x => x.GuestPhone)
            .HasMaxLength(40);

        builder.Property(x => x.CompletedOn);

        builder.Property(x => x.VoidedOn);

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
            .HasDatabaseName("UX_Orders_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_Orders_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.ReceiptNumber })
            .IsUnique()
            .HasDatabaseName("UX_Orders_Tenant_Location_ReceiptNumber");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_Orders_Tenant_BusinessDate");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Orders_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_Orders_Tenant_CorrelationId");

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

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OriginalOrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PriceListId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReceiptTemplate>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReceiptTemplateId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
