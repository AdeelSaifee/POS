using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Data;
using POS.Desktop.Data.Seeding;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Catalog;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Shell;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Shell;

/// <summary>
/// End-to-end bridge handler tests for the four catalog message types.
/// Each test seeds the database and routes a bridge request through
/// PosWebMessageRouter, verifying the full service + serialisation path.
/// </summary>
public sealed class CatalogBridgeHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public CatalogBridgeHandlerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Dispose();

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Builds a fully wired router whose DI scope can resolve ICatalogService
    /// backed by the shared in-memory SQLite connection. Seeds catalog for
    /// tenantId when > 0; leaves DB empty for unprovisioned scenarios.
    /// </summary>
    private async Task<PosWebMessageRouter> BuildRouterAsync(int tenantId = 42)
    {
        // Create schema and seed using IgnoreQueryFilters-capable seeder.
        using (var seedDb = new PosLocalDbContext(_options, new NoProvisionedTerminalContext()))
        {
            seedDb.Database.EnsureCreated();
            if (tenantId > 0)
                await new LocalCatalogSeeder(seedDb).SeedAsync(tenantId);
        }

        IProvisionedTerminalContext terminalContext = tenantId > 0
            ? new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, 101, 999))
            : (IProvisionedTerminalContext)new NoProvisionedTerminalContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISessionService, OperatorSessionService>();
        services.AddSingleton<IAuthService, StubAuthService>();
        services.AddSingleton<IProvisionedTerminalContext>(terminalContext);
        services.AddDbContext<PosLocalDbContext>(opt => opt.UseSqlite(_connection));
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ILocalCatalogSeeder, LocalCatalogSeeder>();
        services.AddScoped<ITerminalProvisioningStore, EfTerminalProvisioningStore>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
    }

    /// <summary>Creates a bridge request with an optional payload object (serialised via BridgeJsonSerializerOptions).</summary>
    private static BridgeRequestEnvelope MakeRequest(string type, object? payload = null)
    {
        JsonElement? element = null;
        if (payload != null)
        {
            // Clone() makes the element independent of its owning JsonDocument.
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, BridgeJsonSerializerOptions.Default);
            element = JsonDocument.Parse(bytes).RootElement.Clone();
        }
        return new BridgeRequestEnvelope
        {
            Type = type,
            RequestId = Guid.NewGuid().ToString("N"),
            Version = "v1",
            Payload = element
        };
    }

    /// <summary>Parses the response payload JSON into a JsonElement for assertions.</summary>
    private static JsonElement ParsePayload(BridgeResponseEnvelope response)
    {
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        return JsonDocument.Parse(json).RootElement;
    }

    // ---------------------------------------------------------------
    // Registration
    // ---------------------------------------------------------------

    [Fact]
    public async Task CatalogHandlers_AreRegisteredInRouter()
    {
        var router = await BuildRouterAsync();

        Assert.True(router.CanHandle("catalog.listCategories"));
        Assert.True(router.CanHandle("catalog.listItems"));
        Assert.True(router.CanHandle("catalog.searchItems"));
        Assert.True(router.CanHandle("catalog.lookupByIdentifier"));
    }

    // ---------------------------------------------------------------
    // catalog.listCategories
    // ---------------------------------------------------------------

    [Fact]
    public async Task ListCategories_ReturnsSeededCategories()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(MakeRequest("catalog.listCategories"), CancellationToken.None);

        Assert.True(response.Ok);
        var payload = ParsePayload(response);
        var categories = payload.GetProperty("categories");
        Assert.Equal(3, categories.GetArrayLength());
    }

    [Fact]
    public async Task ListCategories_UnprovisionedContext_ReturnsEmptyArray()
    {
        var router = await BuildRouterAsync(tenantId: 0);
        var response = await router.RouteAsync(MakeRequest("catalog.listCategories"), CancellationToken.None);

        Assert.True(response.Ok);
        var payload = ParsePayload(response);
        Assert.Equal(0, payload.GetProperty("categories").GetArrayLength());
    }

    // ---------------------------------------------------------------
    // catalog.listItems
    // ---------------------------------------------------------------

    [Fact]
    public async Task ListItems_NoPayload_ReturnsAllSeededItems()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(MakeRequest("catalog.listItems"), CancellationToken.None);

        Assert.True(response.Ok);
        var payload = ParsePayload(response);
        Assert.Equal(3, payload.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ListItems_CategoryFilter_ReturnsOnlyMatchingItems()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.listItems", new { categoryId = 2, limit = 50 }),
            CancellationToken.None);

        Assert.True(response.Ok);
        var items = ParsePayload(response).GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("ITEM-001", items[0].GetProperty("itemCode").GetString());
    }

    [Fact]
    public async Task ListItems_UnprovisionedContext_ReturnsEmptyArray()
    {
        var router = await BuildRouterAsync(tenantId: 0);
        var response = await router.RouteAsync(MakeRequest("catalog.listItems"), CancellationToken.None);

        Assert.True(response.Ok);
        Assert.Equal(0, ParsePayload(response).GetProperty("items").GetArrayLength());
    }

    // ---------------------------------------------------------------
    // catalog.searchItems
    // ---------------------------------------------------------------

    [Fact]
    public async Task SearchItems_ByName_ReturnsMatchingItem()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.searchItems", new { searchText = "Water", limit = 50 }),
            CancellationToken.None);

        Assert.True(response.Ok);
        var items = ParsePayload(response).GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("ITEM-001", items[0].GetProperty("itemCode").GetString());
    }

    [Fact]
    public async Task SearchItems_NoMatch_ReturnsEmptyArray()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.searchItems", new { searchText = "ZZNOTFOUND" }),
            CancellationToken.None);

        Assert.True(response.Ok);
        Assert.Equal(0, ParsePayload(response).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task SearchItems_EmptyPayload_ReturnsAllItems()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.searchItems"),
            CancellationToken.None);

        Assert.True(response.Ok);
        Assert.Equal(3, ParsePayload(response).GetProperty("items").GetArrayLength());
    }

    // ---------------------------------------------------------------
    // catalog.lookupByIdentifier
    // ---------------------------------------------------------------

    [Fact]
    public async Task LookupByIdentifier_KnownBarcode_ReturnsFindAndItem()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier", new { identifierValue = "5000001000010" }),
            CancellationToken.None);

        Assert.True(response.Ok);
        var payload = ParsePayload(response);
        Assert.True(payload.GetProperty("found").GetBoolean());
        var item = payload.GetProperty("item");
        Assert.Equal("ITEM-001", item.GetProperty("itemCode").GetString());
    }

    [Fact]
    public async Task LookupByIdentifier_UnknownBarcode_ReturnsFoundFalseAndNullItem()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier", new { identifierValue = "9999999999999" }),
            CancellationToken.None);

        Assert.True(response.Ok);
        var payload = ParsePayload(response);
        Assert.False(payload.GetProperty("found").GetBoolean());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("item").ValueKind);
    }

    [Fact]
    public async Task LookupByIdentifier_MissingPayload_ReturnsMalformedError()
    {
        var router = await BuildRouterAsync(42);
        // No payload at all
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier"),
            CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error?.Code);
    }

    [Fact]
    public async Task LookupByIdentifier_MissingIdentifierValueProperty_ReturnsMalformedError()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier", new { wrongField = "oops" }),
            CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error?.Code);
    }

    [Fact]
    public async Task LookupByIdentifier_UnprovisionedContext_ReturnsFoundFalse()
    {
        var router = await BuildRouterAsync(tenantId: 0);
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier", new { identifierValue = "5000001000010" }),
            CancellationToken.None);

        Assert.True(response.Ok);
        Assert.False(ParsePayload(response).GetProperty("found").GetBoolean());
    }

    // ---------------------------------------------------------------
    // Error safety - responses must not leak internals
    // ---------------------------------------------------------------

    [Fact]
    public async Task LookupByIdentifier_ResponseDoesNotLeakStackTraceOrFilePath()
    {
        var router = await BuildRouterAsync(42);
        var response = await router.RouteAsync(
            MakeRequest("catalog.lookupByIdentifier"),
            CancellationToken.None);

        Assert.False(response.Ok);
        var errorMsg = response.Error?.Message ?? string.Empty;
        Assert.DoesNotContain("Exception", errorMsg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", errorMsg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(":\\", errorMsg); // no file paths
    }
}
