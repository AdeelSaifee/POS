using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using POS.Shared.Contracts;

namespace POS.Api.Data.DesignTime;

public sealed class PosCentralDbContextFactory : IDesignTimeDbContextFactory<PosCentralDbContext>
{
    private const string DesignTimeConnectionEnvironmentVariable = "POS_CENTRAL_DESIGNTIME_CONNECTION";
    private const string FallbackConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=POS_Central_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

    public PosCentralDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PosCentralDbContext>();
        var connectionString = Environment.GetEnvironmentVariable(DesignTimeConnectionEnvironmentVariable);

        optionsBuilder.UseSqlServer(
            string.IsNullOrWhiteSpace(connectionString) ? FallbackConnectionString : connectionString);

        return new PosCentralDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ICurrentTenantContext
    {
        public int CurrentTenantId => 0;

        public bool HasTenant => false;

        public bool IsSystemScope => false;
    }
}
