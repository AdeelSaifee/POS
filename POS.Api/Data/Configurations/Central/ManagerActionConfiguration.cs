using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Shared.Domain.Entities.Central;

namespace POS.Api.Data.Configurations.Central;

public class ManagerActionConfiguration : IEntityTypeConfiguration<ManagerAction>
{
    public void Configure(EntityTypeBuilder<ManagerAction> builder)
    {
        builder.ToTable("ManagerActions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ManagerActions_Tenant_Id");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LocationId)
            .IsRequired();

        builder.Property(x => x.TerminalId)
            .IsRequired();

        builder.Property(x => x.ShiftId);

        builder.Property(x => x.OrderId);

        builder.Property(x => x.OrderLineId);

        builder.Property(x => x.PerformedByEmployeeId)
            .IsRequired();

        builder.Property(x => x.AuthorizedByEmployeeId);

        builder.Property(x => x.ReasonCodeId);

        builder.Property(x => x.BusinessDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TerminalSequence)
            .IsRequired();

        builder.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(80);

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
            .HasDatabaseName("UX_ManagerActions_Tenant_IdempotencyKey");

        builder.HasIndex(x => new { x.TenantId, x.TerminalId, x.TerminalSequence })
            .IsUnique()
            .HasDatabaseName("UX_ManagerActions_Tenant_Terminal_Sequence");

        builder.HasIndex(x => new { x.TenantId, x.OrderId })
            .HasDatabaseName("IX_ManagerActions_Tenant_Order");

        builder.HasIndex(x => new { x.TenantId, x.OrderLineId })
            .HasDatabaseName("IX_ManagerActions_Tenant_OrderLine");

        builder.HasIndex(x => new { x.TenantId, x.AuthorizedByEmployeeId, x.BusinessDate })
            .HasDatabaseName("IX_ManagerActions_Tenant_AuthorizedBy_Date");

        builder.HasIndex(x => new { x.TenantId, x.ActionType, x.BusinessDate })
            .HasDatabaseName("IX_ManagerActions_Tenant_Action_Date");

        builder.HasIndex(x => new { x.TenantId, x.ReasonCodeId })
            .HasDatabaseName("IX_ManagerActions_Tenant_ReasonCode");

        builder.HasIndex(x => new { x.TenantId, x.CorrelationId })
            .HasDatabaseName("IX_ManagerActions_Tenant_CorrelationId");

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

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderLine>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.OrderLineId })
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

        builder.HasOne<ReasonCode>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReasonCodeId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
