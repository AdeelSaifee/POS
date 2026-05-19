using System.Net;
using System.Text.Json;
using POS.Api.Auth;
using POS.Tests.Integration;
using POS.Tests.Integration.Auth;

namespace POS.Tests.IntegrationTests;

public sealed class TenantScopedReadEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public TenantScopedReadEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/tenant/profile")]
    [InlineData("/api/locations")]
    [InlineData("/api/categories")]
    [InlineData("/api/units-of-measure")]
    public async Task ProtectedEndpoints_Unauthenticated_AreDenied(string route)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTenantProfile_Tenant101_ReturnsOnlyTenant101Profile()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/profile", "101");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(101, document.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("TENANT-101", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("Tenant 101", document.RootElement.GetProperty("name").GetString());

        var propertyNames = document.RootElement.EnumerateObject().Select(x => x.Name).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "code", "id", "logoUrl", "name", "status" }, propertyNames);
    }

    [Fact]
    public async Task GetTenantProfile_Tenant202_ReturnsOnlyTenant202Profile()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/profile", "202");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(202, document.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("TENANT-202", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("Tenant 202", document.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetLocations_Tenant101_SeesOnlyTenant101Data()
    {
        await AssertSingleItemListAsync("/api/locations", "101", "locationCode", "LOC-101-A", "LOC-202-A");
    }

    [Fact]
    public async Task GetLocations_Tenant202_SeesOnlyTenant202Data()
    {
        await AssertSingleItemListAsync("/api/locations", "202", "locationCode", "LOC-202-A", "LOC-101-A");
    }

    [Fact]
    public async Task GetCategories_Tenant101_SeesOnlyTenant101Data()
    {
        await AssertSingleItemListAsync("/api/categories", "101", "categoryCode", "CAT-101-A", "CAT-202-A");
    }

    [Fact]
    public async Task GetCategories_Tenant202_SeesOnlyTenant202Data()
    {
        await AssertSingleItemListAsync("/api/categories", "202", "categoryCode", "CAT-202-A", "CAT-101-A");
    }

    [Fact]
    public async Task GetUnitsOfMeasure_Tenant101_SeesOnlyTenant101Data()
    {
        await AssertSingleItemListAsync("/api/units-of-measure", "101", "code", "EA-101", "KG-202");
    }

    [Fact]
    public async Task GetUnitsOfMeasure_Tenant202_SeesOnlyTenant202Data()
    {
        await AssertSingleItemListAsync("/api/units-of-measure", "202", "code", "KG-202", "EA-101");
    }

    [Fact]
    public async Task GetTenantProfile_MissingTenantId_FailsClosed()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthenticatedWithoutTenantRequest("/api/tenant/profile");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/locations")]
    [InlineData("/api/categories")]
    [InlineData("/api/units-of-measure")]
    public async Task TenantScopedLists_MissingTenantId_FailClosed(string route)
    {
        await AssertEmptyListForInvalidTenantAsync(route, CreateAuthenticatedWithoutTenantRequest(route));
    }

    [Fact]
    public async Task GetTenantProfile_MalformedTenantId_FailsClosed()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/profile", "not-an-int");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/locations")]
    [InlineData("/api/categories")]
    [InlineData("/api/units-of-measure")]
    public async Task TenantScopedLists_MalformedTenantId_FailClosed(string route)
    {
        await AssertEmptyListForInvalidTenantAsync(route, CreateAuthorizedRequest(route, "not-an-int"));
    }

    [Fact]
    public async Task GetTenantProfile_NonPositiveTenantId_FailsClosed()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/profile", "0");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/locations")]
    [InlineData("/api/categories")]
    [InlineData("/api/units-of-measure")]
    public async Task TenantScopedLists_NonPositiveTenantId_FailClosed(string route)
    {
        await AssertEmptyListForInvalidTenantAsync(route, CreateAuthorizedRequest(route, "0"));
    }

    [Fact]
    public async Task QueryStringTenantId_CannotOverrideAuthenticatedClaim_ForTenantProfile()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/profile?tenantId=202", "101");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(101, document.RootElement.GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData("/api/locations?tenantId=202", "locationCode", "LOC-101-A")]
    [InlineData("/api/categories?tenantId=202", "categoryCode", "CAT-101-A")]
    [InlineData("/api/units-of-measure?tenantId=202", "code", "EA-101")]
    public async Task QueryStringTenantId_CannotOverrideAuthenticatedClaim_ForTenantLists(
        string route,
        string propertyName,
        string expectedValue)
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest(route, "101");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var firstItem = document.RootElement.EnumerateArray().Single();

        Assert.Equal(expectedValue, firstItem.GetProperty(propertyName).GetString());
    }

    [Fact]
    public async Task RouteTenantId_DoesNotBecomeAuthority_WhereNoRouteExists()
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest("/api/tenant/202/profile", "101");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLocations_ResponseExposesOnlySafeFields()
    {
        await AssertListPropertyShapeAsync(
            "/api/locations",
            "101",
            new[] { "id", "locationCode", "locationType", "name" });
    }

    [Fact]
    public async Task GetCategories_ResponseExposesOnlySafeFields()
    {
        await AssertListPropertyShapeAsync(
            "/api/categories",
            "101",
            new[] { "categoryCode", "displayOrder", "id", "imageUrl", "name", "parentCategoryId" });
    }

    [Fact]
    public async Task GetUnitsOfMeasure_ResponseExposesOnlySafeFields()
    {
        await AssertListPropertyShapeAsync(
            "/api/units-of-measure",
            "101",
            new[] { "baseUnitId", "code", "conversionFactorToBase", "id", "measurementType", "name" });
    }

    private async Task AssertSingleItemListAsync(
        string route,
        string tenantId,
        string identityProperty,
        string expectedValue,
        string unexpectedValue)
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest(route, tenantId);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.EnumerateArray().ToArray();

        Assert.Single(items);
        Assert.Equal(expectedValue, items[0].GetProperty(identityProperty).GetString());
        Assert.DoesNotContain(items, x =>
            string.Equals(x.GetProperty(identityProperty).GetString(), unexpectedValue, StringComparison.Ordinal));
    }

    private async Task AssertEmptyListForInvalidTenantAsync(string route, HttpRequestMessage request)
    {
        using var client = _factory.CreateClient();

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    private async Task AssertListPropertyShapeAsync(string route, string tenantId, string[] expectedProperties)
    {
        using var client = _factory.CreateClient();
        using var request = CreateAuthorizedRequest(route, tenantId);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var properties = document.RootElement
            .EnumerateArray()
            .First()
            .EnumerateObject()
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(expectedProperties, properties);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(string route, string tenantId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, route);
        TestRequestAuthentication.UserOrAdmin(clientType: "user", tenantId: tenantId).Apply(request);
        return request;
    }

    private static HttpRequestMessage CreateAuthenticatedWithoutTenantRequest(string route)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, route);
        TestRequestAuthentication.UserOrAdmin(clientType: "user").Apply(request);
        return request;
    }
}
