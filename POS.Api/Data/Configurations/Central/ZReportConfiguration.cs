using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ZReportConfiguration : IEntityTypeConfiguration<ZReport>
{
    public void Configure(EntityTypeBuilder<ZReport> builder)
    {
        builder.ToTable("ZReports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ZReports_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId);

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.GeneratedByEmployeeId)
            .IsRequired();

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.ReportNumber)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.ReportType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.GrossSalesAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.NetSalesAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.RefundAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CashExpectedAmount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CashCountedAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.CashVarianceAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.ReportPayloadJson)
            .IsRequired();

        builder.Property(x => x.GeneratedOn)
            .IsRequired();

        builder.Property(x => x.SyncedOn);

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
            .HasMaxLength(100);

        builder.Property(x => x.CreatedOn)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedOn);

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_ZReports_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.TerminalId, x.ReportNumber })
            .IsUnique()
            .HasFilter("[TerminalId] IS NOT NULL")
            .HasDatabaseName("UX_ZReports_Tenant_Location_Terminal_ReportNumber");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.ReportNumber })
            .IsUnique()
            .HasFilter("[TerminalId] IS NULL")
            .HasDatabaseName("UX_ZReports_Tenant_Location_DayReportNumber");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_ZReports_Tenant_Date");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_ZReports_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_ZReports_Tenant_CorrelationId");

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
            .HasForeignKey(x => new { x.TenantId, x.GeneratedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
