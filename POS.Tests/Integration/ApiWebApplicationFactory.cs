using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using POS.Api.Data;
using POS.Tests.Integration.Auth;

namespace POS.Tests.Integration;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"POS_Api_IntegrationTests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CentralDatabase"] = ConnectionString,
                ["Authentication:Jwt:Issuer"] = "integration-tests",
                ["Authentication:Jwt:Audience"] = "integration-tests",
                ["Authentication:Jwt:SigningKey"] = "integration-tests-signing-key-should-be-long-enough"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PosCentralDbContext>>();
            services.RemoveAll<PosCentralDbContext>();

            services.AddDbContext<PosCentralDbContext>(options =>
            {
                options.UseSqlServer(ConnectionString);
            });

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultScheme = TestAuthenticationDefaults.AuthenticationScheme;
                })
                .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                    TestAuthenticationDefaults.AuthenticationScheme,
                    _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PosCentralDbContext>();

        await dbContext.Database.MigrateAsync();
        await ApiTestDataSeeder.SeedAsync(dbContext);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PosCentralDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
