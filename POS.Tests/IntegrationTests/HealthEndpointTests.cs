using System.Net;
using POS.Tests.Integration;

namespace POS.Tests.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HealthEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_DoesNotExposeSecretsOrConnectionStrings()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var payload = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        Assert.DoesNotContain("ConnectionStrings", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SigningKey", payload, StringComparison.OrdinalIgnoreCase);
    }
}
