using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using POS.Shared.Contracts;

namespace POS.Desktop.Data.DesignTime;

public sealed class PosLocalDbContextFactory : IDesignTimeDbContextFactory<PosLocalDbContext>
{
    private const string DesignTimeConnectionEnvironmentVariable = "POS_LOCAL_DESIGNTIME_CONNECTION";
    private const string FallbackConnectionString = "Data Source=pos_local_designtime.db";

    public PosLocalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PosLocalDbContext>();
        var connectionString = Environment.GetEnvironmentVariable(DesignTimeConnectionEnvironmentVariable);

        optionsBuilder.UseSqlite(
            string.IsNullOrWhiteSpace(connectionString) ? FallbackConnectionString : connectionString);

        return new PosLocalDbContext(optionsBuilder.Options, new DesignTimeProvisionedTerminalContext());
    }

    private sealed class DesignTimeProvisionedTerminalContext : IProvisionedTerminalContext
    {
        public int CurrentTenantId => 0;

        public int CurrentLocationId => 0;

        public int CurrentTerminalId => 0;

        public bool IsProvisioned => false;
    }
}
