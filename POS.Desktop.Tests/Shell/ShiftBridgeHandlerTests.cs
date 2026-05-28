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
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Shifts;
using POS.Desktop.Shell;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public sealed class ShiftBridgeHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PosLocalDbContext> _options;

    public ShiftBridgeHandlerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<PosLocalDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Dispose();

    private async Task<(PosWebMessageRouter Router, ISessionService SessionService, PosLocalDbContext Db)> BuildRouterAndServicesAsync(int tenantId = 1, bool isProvisioned = true)
    {
        // Enforce DB schema creation
        var db = new PosLocalDbContext(_options, new NoProvisionedTerminalContext());
        db.Database.EnsureCreated();

        IProvisionedTerminalContext terminalContext = isProvisioned
            ? new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, 101, 999))
            : (IProvisionedTerminalContext)new NoProvisionedTerminalContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISessionService, OperatorSessionService>();
        services.AddSingleton<IAuthService, StubAuthService>();
        services.AddSingleton<IProvisionedTerminalContext>(terminalContext);
        services.AddDbContext<PosLocalDbContext>(opt => opt.UseSqlite(_connection));
        services.AddScoped<IShiftService, ShiftService>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var sessionService = provider.GetRequiredService<ISessionService>();
        var scopedDb = provider.GetRequiredService<PosLocalDbContext>();

        var router = new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
        return (router, sessionService, scopedDb);
    }

    private static BridgeRequestEnvelope MakeRequest(string type, object? payload = null)
    {
        JsonElement? element = null;
        if (payload != null)
        {
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

    private static JsonElement ParsePayload(object? payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, BridgeJsonSerializerOptions.Default);
        return JsonDocument.Parse(bytes).RootElement;
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsSuccess_ForValidPayload()
    {
        // Arrange
        var (router, sessionService, db) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);

        // Setup active terminal session in DB
        var terminalSession = new LocalTerminalSession
        {
            TenantId = 1,
            LocationId = 101,
            TerminalId = 999,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel Cashier",
            Role = "Cashier",
            ShiftId = null,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        // Setup active session in memory
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel Cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: "999",
            SessionId: terminalSession.Id.ToString()
        ));

        var request = MakeRequest("shift.open", new { openingFloat = 5000 });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.Null(response.Error);
        Assert.NotNull(response.Payload);

        var payloadJson = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(payloadJson);
        Assert.True(doc.RootElement.TryGetProperty("shiftId", out var shiftIdProp));
        Assert.True(doc.RootElement.TryGetProperty("businessDate", out var bizDateProp));
        Assert.True(doc.RootElement.TryGetProperty("openingFloat", out var floatProp));
        Assert.True(doc.RootElement.TryGetProperty("status", out var statusProp));

        Assert.Equal("5000", floatProp.GetRawText());
        Assert.Equal("Open", statusProp.GetString());
        Assert.False(string.IsNullOrWhiteSpace(shiftIdProp.GetString()));
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsMalformedRequest_ForMissingPayload()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync();
        var request = MakeRequest("shift.open", null);

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsMalformedRequest_ForMissingOpeningFloat()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync();
        var request = MakeRequest("shift.open", new { wrongProperty = "value" });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsMalformedRequest_ForInvalidFloatValue()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync();
        var request = MakeRequest("shift.open", new { openingFloat = "not-a-number" });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsFailure_ForUnprovisionedTerminal()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: false);
        var request = MakeRequest("shift.open", new { openingFloat = 5000 });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("TERMINAL_UNPROVISIONED", response.Error.Code);
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsFailure_ForNoActiveSession()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);
        var request = MakeRequest("shift.open", new { openingFloat = 5000 });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("NO_ACTIVE_SESSION", response.Error.Code);
    }

    [Fact]
    public async Task HandleShiftOpenAsync_ReturnsFailure_ForNegativeOpeningFloat()
    {
        // Arrange
        var (router, sessionService, db) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);

        // Setup active terminal session in DB
        var terminalSession = new LocalTerminalSession
        {
            TenantId = 1,
            LocationId = 101,
            TerminalId = 999,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel Cashier",
            Role = "Cashier",
            ShiftId = null,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        // Setup active session in memory
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel Cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: "999",
            SessionId: terminalSession.Id.ToString()
        ));

        var request = MakeRequest("shift.open", new { openingFloat = -150 });

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_OPENING_FLOAT", response.Error.Code);
    }

    [Fact]
    public async Task HandleGetCurrentShiftAsync_ReturnsSuccessPayload_WithIsOpenTrue_WhenOpenShiftExists()
    {
        // Arrange
        var (router, _, db) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);

        // Setup an active open shift in SQLite
        var openShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            LocationId = 101,
            TerminalId = 999,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 3000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShift);
        await db.SaveChangesAsync();

        var request = MakeRequest("shift.getCurrent");

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.Null(response.Error);
        Assert.NotNull(response.Payload);

        var payloadJson = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(payloadJson);
        Assert.True(doc.RootElement.GetProperty("isOpen").GetBoolean());
        Assert.Equal(openShift.Id.ToString(), doc.RootElement.GetProperty("shiftId").GetString());
        Assert.Equal(3000m, doc.RootElement.GetProperty("openingFloat").GetDecimal());
        Assert.Equal("Open", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HandleGetCurrentShiftAsync_ReturnsSuccessPayload_WithIsOpenFalse_WhenNoShiftExists()
    {
        // Arrange
        var (router, _, _) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);
        var request = MakeRequest("shift.getCurrent");

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.Null(response.Error);
        Assert.NotNull(response.Payload);

        var payloadJson = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(payloadJson);
        Assert.False(doc.RootElement.GetProperty("isOpen").GetBoolean());
        Assert.Null(doc.RootElement.GetProperty("shiftId").GetString());
    }

    [Fact]
    public async Task HandleGetCurrentShiftAsync_DoesNotExposeExceptionDetails_OnFailure()
    {
        // Arrange
        var (router, _, db) = await BuildRouterAndServicesAsync(tenantId: 1, isProvisioned: true);

        // Force database querying to fail by closing SQLite connection
        _connection.Close();

        var request = MakeRequest("shift.getCurrent");

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("SHIFT_QUERY_FAILED", response.Error.Code);
        Assert.Equal("Failed to query current shift status.", response.Error.Message);
    }
}
