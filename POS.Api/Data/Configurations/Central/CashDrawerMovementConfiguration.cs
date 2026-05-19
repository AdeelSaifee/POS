using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class CashDrawerMovementConfiguration : IEntityTypeConfiguration<CashDrawerMovement>
{
    public void Configure(EntityTypeBuilder<CashDrawerMovement> builder)
    {
        builder.ToTable("CashDrawerMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_CashDrawerMovements_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ShiftId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.ReasonCodeId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.MovementType)
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

        builder.Property(x => x.Comment)
            .HasMaxLength(500);

        builder.Property(x => x.OccurredOn)
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
            .HasDatabaseName("UX_CashDrawerMovements_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_CashDrawerMovements_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.ShiftId })
            .HasDatabaseName("IX_CashDrawerMovements_Tenant_Shift");

        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.BusinessDate })
            .HasDatabaseName("IX_CashDrawerMovements_Tenant_Date");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_CashDrawerMovements_Tenant_CorrelationId");

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
    }
}
