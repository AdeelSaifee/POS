using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Shell;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

/// <summary>
/// Verifies the behavior of the terminal provisioning bridge handlers and store logic.
/// </summary>
public sealed class TerminalProvisioningStoreHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public TerminalProvisioningStoreHandlerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private (PosLocalDbContext DbContext, ProvisionedTerminalContext Context, EfTerminalProvisioningStore Store, PosWebMessageRouter Router) CreateContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register SQLite DbContext using open connection
        services.AddDbContext<PosLocalDbContext>(opt => opt.UseSqlite(_connection));

        // Register in-memory context (default unprovisioned)
        var inMemoryContext = new ProvisionedTerminalContext(ProvisioningRecord.Unprovisioned);
        services.AddSingleton<IProvisionedTerminalContext>(inMemoryContext);

        // Register store
        services.AddScoped<ITerminalProvisioningStore, EfTerminalProvisioningStore>();

        // Register Router
        services.AddSingleton<PosWebMessageRouter>(sp =>
            new PosWebMessageRouter(sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PosWebMessageRouter>.Instance));

        var provider = services.BuildServiceProvider();

        // Create the DB schema
        var db = provider.GetRequiredService<PosLocalDbContext>();
        db.Database.EnsureCreated();

        var store = (EfTerminalProvisioningStore)provider.GetRequiredService<ITerminalProvisioningStore>();
        var router = provider.GetRequiredService<PosWebMessageRouter>();

        return (db, inMemoryContext, store, router);
    }

    private JsonElement CreatePayloadElement(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
    }

    [Fact]
    public async Task ProvisionTerminal_ValidPayload_PersistsRowIdOneAndUpdatesContext()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.provisionTerminal",
            RequestId = "req-prov-1",
            Payload = CreatePayloadElement(new { tenantId = 42, locationId = 101, terminalId = 999 })
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Null(response.Error);

        // Verify row exists in DB with Id = 1
        var dbRow = db.TerminalProvisioning.SingleOrDefault(x => x.Id == 1);
        Assert.NotNull(dbRow);
        Assert.Equal(42, dbRow.TenantId);
        Assert.Equal(101, dbRow.LocationId);
        Assert.Equal(999, dbRow.TerminalId);
        Assert.NotNull(dbRow.UpdatedAt);

        // Verify in-memory context was updated
        Assert.True(context.IsProvisioned);
        Assert.Equal(42, context.CurrentTenantId);
        Assert.Equal(101, context.CurrentLocationId);
        Assert.Equal(999, context.CurrentTerminalId);
    }

    [Theory]
    [InlineData(0, 101, 999)]
    [InlineData(42, -1, 999)]
    [InlineData(42, 101, 0)]
    public async Task ProvisionTerminal_InvalidOrNegativeIds_FailsSafely(int tenantId, int locationId, int terminalId)
    {
        // Arrange
        var (db, context, store, router) = CreateContext();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.provisionTerminal",
            RequestId = "req-prov-2",
            Payload = CreatePayloadElement(new { tenantId, locationId, terminalId })
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_PAYLOAD", response.Error.Code);

        // Verify no row persisted
        var count = db.TerminalProvisioning.Count();
        Assert.Equal(0, count);

        // Verify context is still unprovisioned
        Assert.False(context.IsProvisioned);
    }

    [Fact]
    public async Task ProvisionTerminal_NullOrMissingFields_FailsSafely()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.provisionTerminal",
            RequestId = "req-prov-3",
            Payload = CreatePayloadElement(new { tenantId = 42 }) // Missing locationId and terminalId
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task ProvisionTerminal_AlreadyProvisioned_RejectsReprovisioningWithDifferentValues()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();

        // 1. Provision once
        var req1 = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.provisionTerminal",
            RequestId = "req-prov-4",
            Payload = CreatePayloadElement(new { tenantId = 42, locationId = 101, terminalId = 999 })
        };
        var res1 = await router.RouteAsync(req1, CancellationToken.None);
        Assert.True(res1.Ok);

        // 2. Attempt to provision with different values
        var req2 = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.provisionTerminal",
            RequestId = "req-prov-5",
            Payload = CreatePayloadElement(new { tenantId = 99, locationId = 101, terminalId = 999 }) // Different TenantId
        };

        // Act
        var res2 = await router.RouteAsync(req2, CancellationToken.None);

        // Assert
        Assert.NotNull(res2);
        Assert.False(res2.Ok);
        Assert.NotNull(res2.Error);
        Assert.Equal("REPROVISION_BLOCKED", res2.Error.Code);

        // Ensure database row was not updated
        var dbRow = db.TerminalProvisioning.Single(x => x.Id == 1);
        Assert.Equal(42, dbRow.TenantId);
    }

    [Fact]
    public async Task GetProvisioningStatus_NoRowExists_ReturnsUnprovisioned()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.getProvisioningStatus",
            RequestId = "req-status-1"
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.NotNull(response.Payload);

        // Deserialize response payload
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("isProvisioned").GetBoolean());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("tenantId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("locationId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("terminalId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("updatedAt").ValueKind);
    }

    [Fact]
    public async Task GetProvisioningStatus_ValidRowExists_ReturnsProvisionedStatus()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var row = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = 101,
            TerminalId = 999,
            UpdatedAt = now
        };
        db.TerminalProvisioning.Add(row);
        db.SaveChanges();

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.getProvisioningStatus",
            RequestId = "req-status-2"
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.NotNull(response.Payload);

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("isProvisioned").GetBoolean());
        Assert.Equal(42, root.GetProperty("tenantId").GetInt32());
        Assert.Equal(101, root.GetProperty("locationId").GetInt32());
        Assert.Equal(999, root.GetProperty("terminalId").GetInt32());

        // Assert updatedAt is present and within range
        var updatedAtStr = root.GetProperty("updatedAt").GetString();
        Assert.NotNull(updatedAtStr);
        var updatedAtVal = DateTimeOffset.Parse(updatedAtStr);
        Assert.True(Math.Abs((updatedAtVal - now).TotalSeconds) < 5);
    }

    [Fact]
    public async Task GetProvisioningStatus_PartialOrInvalidRow_FailsClosed()
    {
        // Arrange
        var (db, context, store, router) = CreateContext();

        // Bypass EF/SQLite CHECK constraint by entering a row with missing locationId/terminalId
        var row = new TerminalProvisioning
        {
            Id = 1,
            TenantId = 42,
            LocationId = null,
            TerminalId = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.TerminalProvisioning.Add(row);
        db.SaveChanges();

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "provisioning.getProvisioningStatus",
            RequestId = "req-status-3"
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.NotNull(response.Payload);

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        // Must fail closed: isProvisioned = false and IDs are null
        Assert.False(root.GetProperty("isProvisioned").GetBoolean());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("tenantId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("locationId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("terminalId").ValueKind);
    }
}
