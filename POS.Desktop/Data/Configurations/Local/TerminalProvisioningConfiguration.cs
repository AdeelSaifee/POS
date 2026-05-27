using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

/// <summary>
/// Configuration for the <see cref="TerminalProvisioning"/> entity.
/// </summary>
public sealed class TerminalProvisioningConfiguration : IEntityTypeConfiguration<TerminalProvisioning>
{
    public void Configure(EntityTypeBuilder<TerminalProvisioning> builder)
    {
        builder.ToTable("TerminalProvisioning", t =>
        {
            t.HasCheckConstraint("CK_TerminalProvisioning_Id", "Id = 1");
            t.HasCheckConstraint("CK_TerminalProvisioning_TenantId", "TenantId IS NULL OR TenantId > 0");
            t.HasCheckConstraint("CK_TerminalProvisioning_LocationId", "LocationId IS NULL OR LocationId > 0");
            t.HasCheckConstraint("CK_TerminalProvisioning_TerminalId", "TerminalId IS NULL OR TerminalId > 0");
        });

        builder.HasKey(x => x.Id);

        // Enforce the single-row invariant. By generating never, we must explicitly set Id = 1.
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId);

        builder.Property(x => x.LocationId);

        builder.Property(x => x.TerminalId);

        builder.Property(x => x.UpdatedAt);
    }
}
