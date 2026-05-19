using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_OrderLines_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ItemId);

        builder.Property(x => x.ItemVariantId);

        builder.Property(x => x.OriginalOrderLineId);

        builder.Property(x => x.ReasonCodeId);

        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.LineType)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.SKU)
            .HasMaxLength(80);

        builder.Property(x => x.Barcode)
            .HasMaxLength(150);

        builder.Property(x => x.ItemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.VariantName)
            .HasMaxLength(200);

        builder.Property(x => x.UnitOfMeasureCode)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.GrossAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.NetAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxRuleId);

        builder.Property(x => x.TaxRate)
            .HasPrecision(9, 4);

        builder.Property(x => x.PriceListId);

        builder.Property(x => x.CatalogVersion)
            .IsRequired();

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
            .HasDatabaseName("UX_OrderLines_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.OrderId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_OrderLines_Tenant_Order_LineNumber");

        builder.HasIndex(x => new { x.TenantId, x.OrderId })
            .HasDatabaseName("IX_OrderLines_Tenant_Order");

        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId })
            .HasDatabaseName("IX_OrderLines_Tenant_ItemVariant");

        builder.HasIndex(x => new { x.TenantId, x.OriginalOrderLineId })
            .HasDatabaseName("IX_OrderLines_Tenant_OriginalLine");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_OrderLines_Tenant_CorrelationId");

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

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ItemVariant>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ItemVariantId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OriginalOrderLineId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReasonCodeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AuthorizedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TaxRule>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.TaxRuleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PriceListId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
