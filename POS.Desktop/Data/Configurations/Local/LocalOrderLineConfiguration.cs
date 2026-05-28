using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// Entity Framework Core mapping configuration for the <see cref="LocalOrderLine"/> entity.
/// </summary>
public class LocalOrderLineConfiguration : IEntityTypeConfiguration<LocalOrderLine>
{
    public void Configure(EntityTypeBuilder<LocalOrderLine> builder)
    {
        builder.ToTable("LocalOrderLines", t =>
        {
            t.HasCheckConstraint("CK_LocalOrderLine_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalOrderLine_LocationId", "LocationId > 0");
            t.HasCheckConstraint("CK_LocalOrderLine_TerminalId", "TerminalId > 0");
            t.HasCheckConstraint("CK_LocalOrderLine_LineNumber", "LineNumber > 0");
            t.HasCheckConstraint("CK_LocalOrderLine_Quantity", "Quantity > 0");
            t.HasCheckConstraint("CK_LocalOrderLine_UnitPrice", "UnitPrice >= 0");
            t.HasCheckConstraint("CK_LocalOrderLine_GrossAmount", "GrossAmount >= 0");
            t.HasCheckConstraint("CK_LocalOrderLine_NetAmount", "NetAmount >= 0");
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

        builder.Property(x => x.ItemId);
        builder.Property(x => x.ItemVariantId);
        builder.Property(x => x.OriginalOrderLineId);
        builder.Property(x => x.ReasonCodeId);
        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.LineType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.SKU)
            .HasMaxLength(100);

        builder.Property(x => x.Barcode)
            .HasMaxLength(100);

        builder.Property(x => x.ItemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.VariantName)
            .HasMaxLength(200);

        builder.Property(x => x.UnitOfMeasureCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .IsRequired();

        builder.Property(x => x.GrossAmount)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .IsRequired();

        builder.Property(x => x.NetAmount)
            .IsRequired();

        builder.Property(x => x.TaxRuleId);
        builder.Property(x => x.TaxRate);
        builder.Property(x => x.PriceListId);

        builder.Property(x => x.CatalogVersion)
            .IsRequired();

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
            .HasDatabaseName("IX_LocalOrderLines_OrderId");

        builder.HasIndex(x => new { x.TenantId, x.OrderId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("UX_LocalOrderLines_Tenant_OrderId_LineNumber");
    }
}
