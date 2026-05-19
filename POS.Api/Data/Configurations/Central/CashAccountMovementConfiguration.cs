using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class CashAccountMovementConfiguration : IEntityTypeConfiguration<CashAccountMovement>
{
    public void Configure(EntityTypeBuilder<CashAccountMovement> builder)
    {
        builder.ToTable("CashAccountMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_CashAccountMovements_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId);

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence);

        builder.Property(x => x.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(x => x.SourceCashAccountId);

        builder.Property(x => x.DestinationCashAccountId);

        builder.Property(x => x.PerformedByEmployeeId)
            .IsRequired();

        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.VerifiedByEmployeeId);

        builder.Property(x => x.VerifiedOn);

        builder.Property(x => x.ReasonCodeId);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Comment)
            .HasMaxLength(500);

        builder.Property(x => x.MetadataJson);

        builder.Property(x => x.OccurredOn)
            .IsRequired();

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
            .HasDatabaseName("UX_CashAccountMovements_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasFilter("[TerminalId] IS NOT NULL AND [TerminalSequence] IS NOT NULL")
            .HasDatabaseName("UX_CashAccountMovements_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.BusinessDate })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_BusinessDate");

        builder.HasIndex(x => new { x.TenantId, x.MovementType, x.BusinessDate })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_Type_Date");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.SourceCashAccountId })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_SourceAccount");

        builder.HasIndex(x => new { x.TenantId, x.DestinationCashAccountId })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_DestinationAccount");

        builder.HasIndex(x => new { x.TenantId, x.ReasonCodeId })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_ReasonCode");

        builder.HasIndex(x => new { x.TenantId, x.AuthorizedByEmployeeId, x.BusinessDate })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_AuthorizedBy_Date");

        builder.HasIndex(x => new { x.TenantId, x.VerifiedByEmployeeId, x.BusinessDate })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_VerifiedBy_Date");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_CashAccountMovements_Tenant_CorrelationId");

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

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SourceCashAccountId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CashAccount>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DestinationCashAccountId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.PerformedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AuthorizedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.VerifiedByEmployeeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReasonCodeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
