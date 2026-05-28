using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.CashControl;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Shell;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public class CashControlBridgeHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private const int TenantId = 42;
    private const int LocationId = 101;
    private const int TerminalId = 999;
    private const int EmployeeDbId = 12;
    private const string OperatorId = "EMP012";

    public void Dispose() => _dbHarness.Dispose();

    private (PosWebMessageRouter Router, StubCashControlService Service, PosLocalDbContext Db, ProvisionedTerminalContext Provisioning) CreateRouterWithStub(bool provisioned = true)
    {
        var record = new ProvisioningRecord(TenantId, LocationId, TerminalId);
        var provisioning = provisioned ? new ProvisionedTerminalContext(record) : new ProvisionedTerminalContext();
        var db = _dbHarness.CreateDbContext(provisioning);
        var stub = new StubCashControlService();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ICashControlService>(_ => stub);
        services.AddScoped<IProvisionedTerminalContext>(_ => provisioning);
        services.AddScoped(_ => db);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var router = new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
        return (router, stub, db, provisioning);
    }

    private async Task SetupDatabaseStateAsync(
        PosLocalDbContext db,
        bool openShift = true,
        bool addReasonCodes = true)
    {
        if (addReasonCodes)
        {
            var rc1 = new LocalReasonCode
            {
                Id = 1,
                TenantId = TenantId,
                Code = "DROP_REGULAR",
                Name = "Regular drop",
                ReasonCategory = "CashControl",
                RequiresManagerApproval = false,
                SortOrder = 1
            };
            var rc2 = new LocalReasonCode
            {
                Id = 2,
                TenantId = TenantId,
                Code = "DROP_HIGH",
                Name = "High amount drop",
                ReasonCategory = "CashControl",
                RequiresManagerApproval = true,
                SortOrder = 2
            };
            var rc3 = new LocalReasonCode
            {
                Id = 3,
                TenantId = TenantId,
                Code = "OTHER",
                Name = "Other reason",
                ReasonCategory = "General",
                RequiresManagerApproval = false,
                SortOrder = 3
            };
            db.LocalReasonCodes.AddRange(rc1, rc2, rc3);
        }

        if (openShift)
        {
            var shift = new LocalShift
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                Status = ShiftStatus.Open,
                OpenedByEmployeeId = EmployeeDbId,
                OpeningCashAmount = 1000m,
                BusinessDate = DateOnly.FromDateTime(DateTime.Today),
                TerminalSequence = 1,
                IsActive = true,
                CreatedBy = OperatorId,
                CreatedOn = DateTimeOffset.UtcNow
            };
            db.LocalShifts.Add(shift);
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public void RouterRegistersCashHandlers()
    {
        var (router, _, _, _) = CreateRouterWithStub();
        Assert.True(router.CanHandle("cash.getSummary"));
        Assert.True(router.CanHandle("cash.recordMovement"));
        Assert.True(router.CanHandle("cash.getLedger"));
        Assert.True(router.CanHandle("cash.getReasonCodes"));
    }

    [Fact]
    public async Task HandleCashGetSummary_ReturnsSummaryPayload()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var shiftId = Guid.NewGuid();
        var businessDate = DateOnly.FromDateTime(DateTime.Today);
        service.SummaryResult = new CashDrawerSummaryResult(
            IsOpen: true,
            ShiftId: shiftId,
            BusinessDate: businessDate,
            OpeningFloat: 1000m,
            CashSales: 500m,
            SafeDrops: 200m,
            FloatInjections: 0m,
            ExpectedDrawerBalance: 1300m,
            TransactionCount: 5,
            LastMovementAt: DateTimeOffset.UtcNow,
            AlertCode: "SAFE_DROP_RECOMMENDED",
            AlertMessage: "Please drop safe.",
            IsSafeDropRecommended: true,
            IsOverLimit: false,
            CashDrawerLimit: 10000m,
            SafeDropThreshold: 8000m
        );

        var request = new BridgeRequestEnvelope { Type = "cash.getSummary", RequestId = "req-1", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("isOpen").GetBoolean());
        Assert.Equal(shiftId.ToString(), root.GetProperty("shiftId").GetString());
        Assert.Equal(businessDate.ToString("yyyy-MM-dd"), root.GetProperty("businessDate").GetString());
        Assert.Equal(1000m, root.GetProperty("openingFloat").GetDecimal());
        Assert.Equal(500m, root.GetProperty("cashSales").GetDecimal());
        Assert.Equal(200m, root.GetProperty("safeDrops").GetDecimal());
        Assert.Equal(0m, root.GetProperty("floatInjections").GetDecimal());
        Assert.Equal(1300m, root.GetProperty("expectedDrawerBalance").GetDecimal());
        Assert.Equal(5, root.GetProperty("transactionCount").GetInt32());
        Assert.Equal("SAFE_DROP_RECOMMENDED", root.GetProperty("alertCode").GetString());
        Assert.Equal("Please drop safe.", root.GetProperty("alertMessage").GetString());
        Assert.True(root.GetProperty("isSafeDropRecommended").GetBoolean());
        Assert.False(root.GetProperty("isOverLimit").GetBoolean());
        Assert.Equal(10000m, root.GetProperty("cashDrawerLimit").GetDecimal());
        Assert.Equal(8000m, root.GetProperty("safeDropThreshold").GetDecimal());
    }

    [Fact]
    public async Task HandleCashGetSummary_NoShift_ReturnsIsOpenFalse()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        service.SummaryResult = new CashDrawerSummaryResult(
            IsOpen: false,
            ShiftId: null,
            BusinessDate: null,
            OpeningFloat: 0m,
            CashSales: 0m,
            SafeDrops: 0m,
            FloatInjections: 0m,
            ExpectedDrawerBalance: 0m,
            TransactionCount: 0,
            LastMovementAt: null,
            AlertCode: "CLOSED",
            AlertMessage: "No open shift",
            IsSafeDropRecommended: false,
            IsOverLimit: false,
            CashDrawerLimit: 0m,
            SafeDropThreshold: 0m
        );

        var request = new BridgeRequestEnvelope { Type = "cash.getSummary", RequestId = "req-2", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("isOpen").GetBoolean());
        Assert.Null(root.GetProperty("shiftId").GetString());
    }

    [Fact]
    public async Task HandleCashRecordMovement_Success_MapsPayload()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var movementId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var occurred = DateTimeOffset.UtcNow;
        service.RecordMovementResult = new CashControlMovementResult(
            Success: true,
            MovementId: movementId,
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1000m,
            ReasonCodeId: 1,
            ShiftId: shiftId,
            BusinessDate: date,
            TerminalSequence: 15,
            OccurredOn: occurred
        );

        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-1",
            "amount": 1000.0,
            "reasonCodeId": 1,
            "movementType": "Drop",
            "comment": "Safe drop regular"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "cash.recordMovement",
            RequestId = "req-record-1",
            Version = "v1",
            Payload = payload
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.RecordMovementCalled);
        Assert.Equal("idem-key-1", service.LastRecordMovementRequest!.IdempotencyKey);
        Assert.Equal(1000m, service.LastRecordMovementRequest.Amount);
        Assert.Equal(1, service.LastRecordMovementRequest.ReasonCodeId);
        Assert.Equal(CashDrawerMovementType.Drop, service.LastRecordMovementRequest.MovementType);
        Assert.Equal("Safe drop regular", service.LastRecordMovementRequest.Comment);

        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(movementId.ToString(), root.GetProperty("movementId").GetString());
        Assert.Equal("Drop", root.GetProperty("movementType").GetString());
        Assert.Equal(15, root.GetProperty("terminalSequence").GetInt64());
    }

    [Fact]
    public async Task HandleCashRecordMovement_AcceptsDropString()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-drop-string",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": "drop"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-3", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.RecordMovementCalled);
        Assert.Equal(CashDrawerMovementType.Drop, service.LastRecordMovementRequest!.MovementType);
    }

    [Fact]
    public async Task HandleCashRecordMovement_AcceptsDropNumericValue4()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-drop-num",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": 4
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-4", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.RecordMovementCalled);
        Assert.Equal(CashDrawerMovementType.Drop, service.LastRecordMovementRequest!.MovementType);
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsNumericZero()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-zero",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": 0
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-5", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsCorrection()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-corr",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": "Correction"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-6", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsPayout()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-payout",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": "Payout"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-7", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_MissingIdempotencyKey_ReturnsMalformedRequest()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": "Drop"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-8", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_MalformedAmount_ReturnsMalformedRequest()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-1",
            "amount": "abc",
            "reasonCodeId": 1,
            "movementType": "Drop"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-9", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_ServiceFailure_MapsError()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        service.RecordMovementResult = new CashControlMovementResult(
            Success: false,
            ErrorCode: "MANAGER_AUTH_FAILED",
            ErrorMessage: "Manager PIN verification failed."
        );

        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-fail",
            "amount": 1000,
            "reasonCodeId": 2,
            "movementType": "Drop",
            "managerOperatorId": "MGR01",
            "managerPin": "1234"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-10", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MANAGER_AUTH_FAILED", response.Error!.Code);
        Assert.Equal("Manager PIN verification failed.", response.Error.Message);
    }

    [Fact]
    public async Task HandleCashRecordMovement_DoesNotReturnManagerPin()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var movementId = Guid.NewGuid();
        service.RecordMovementResult = new CashControlMovementResult(
            Success: true,
            MovementId: movementId,
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1000m,
            ReasonCodeId: 2,
            TerminalSequence: 1,
            OccurredOn: DateTimeOffset.UtcNow
        );

        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-pin",
            "amount": 1000,
            "reasonCodeId": 2,
            "movementType": "Drop",
            "managerOperatorId": "MGR01",
            "managerPin": "1234"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-11", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        Assert.DoesNotContain("1234", json);
        Assert.DoesNotContain("managerPin", json);
    }

    [Fact]
    public async Task HandleCashGetLedger_ReturnsEmpty_WhenNoOpenShift()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: false);

        var request = new BridgeRequestEnvelope { Type = "cash.getLedger", RequestId = "req-ledger-empty", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isOpen").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("movements").GetArrayLength());
    }

    [Fact]
    public async Task HandleCashGetLedger_ReturnsMovementRowsWithReasonCodes_SortedByTerminalSequenceDescending()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: true);

        // Fetch shift
        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed movements with reason codes
        db.LocalCashDrawerMovements.AddRange(
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                EmployeeId = EmployeeDbId,
                ShiftId = activeShift.Id,
                Amount = 100m,
                TerminalSequence = 1,
                OccurredOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ReasonCodeId = 1,
                IsActive = true,
                Comment = "First Drop",
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = "idem-test-1",
                CreatedBy = OperatorId,
                CreatedOn = DateTimeOffset.UtcNow
            },
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                EmployeeId = EmployeeDbId,
                ShiftId = activeShift.Id,
                Amount = 200m,
                TerminalSequence = 3,
                OccurredOn = DateTimeOffset.UtcNow,
                ReasonCodeId = 2,
                IsActive = true,
                Comment = "Third Drop",
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = "idem-test-2",
                CreatedBy = OperatorId,
                CreatedOn = DateTimeOffset.UtcNow
            },
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                EmployeeId = EmployeeDbId,
                ShiftId = activeShift.Id,
                Amount = 150m,
                TerminalSequence = 2,
                OccurredOn = DateTimeOffset.UtcNow.AddMinutes(-2),
                ReasonCodeId = 1,
                IsActive = true,
                Comment = "Second Drop",
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = "idem-test-3",
                CreatedBy = OperatorId,
                CreatedOn = DateTimeOffset.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "cash.getLedger", RequestId = "req-ledger-rows", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("isOpen").GetBoolean());
        Assert.Equal(activeShift.Id.ToString(), root.GetProperty("shiftId").GetString());

        var movements = root.GetProperty("movements");
        Assert.Equal(3, movements.GetArrayLength());

        // Sorted by sequence desc: sequence 3, then 2, then 1
        Assert.Equal(3, movements[0].GetProperty("terminalSequence").GetInt64());
        Assert.Equal(200m, movements[0].GetProperty("amount").GetDecimal());
        Assert.Equal("DROP_HIGH", movements[0].GetProperty("reasonCode").GetString());
        Assert.Equal("High amount drop", movements[0].GetProperty("reasonName").GetString());

        Assert.Equal(2, movements[1].GetProperty("terminalSequence").GetInt64());
        Assert.Equal("DROP_REGULAR", movements[1].GetProperty("reasonCode").GetString());

        Assert.Equal(1, movements[2].GetProperty("terminalSequence").GetInt64());
        Assert.Equal("DROP_REGULAR", movements[2].GetProperty("reasonCode").GetString());
    }

    [Fact]
    public async Task HandleCashGetReasonCodes_FiltersCashControlCategory()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: false, addReasonCodes: true);

        var request = new BridgeRequestEnvelope { Type = "cash.getReasonCodes", RequestId = "req-rc-1", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var reasonCodes = root.GetProperty("reasonCodes");
        // rc1 & rc2 are CashControl category, rc3 is General category
        Assert.Equal(2, reasonCodes.GetArrayLength());
        Assert.False(root.GetProperty("usedFallback").GetBoolean());
        Assert.Equal("CashControl", root.GetProperty("reasonCategoryFilter").GetString());

        Assert.Equal("DROP_REGULAR", reasonCodes[0].GetProperty("code").GetString());
        Assert.Equal("DROP_HIGH", reasonCodes[1].GetProperty("code").GetString());
    }

    [Fact]
    public async Task HandleCashGetReasonCodes_FallsBackToAll_WhenCashControlMissing()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        // Seed only General category
        db.LocalReasonCodes.Add(new LocalReasonCode
        {
            Id = 3,
            TenantId = TenantId,
            Code = "OTHER",
            Name = "Other reason",
            ReasonCategory = "General",
            RequiresManagerApproval = false,
            SortOrder = 1
        });
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "cash.getReasonCodes", RequestId = "req-rc-fallback", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var reasonCodes = root.GetProperty("reasonCodes");
        Assert.Equal(1, reasonCodes.GetArrayLength());
        Assert.True(root.GetProperty("usedFallback").GetBoolean());
        Assert.Equal("OTHER", reasonCodes[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsInjectionString()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-inject",
            "amount": 500,
            "reasonCodeId": 1,
            "movementType": "Injection"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-inject", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsNoSale()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-nosale",
            "amount": 0,
            "reasonCodeId": 1,
            "movementType": "NoSale"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-nosale", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashRecordMovement_RejectsOpeningFloat()
    {
        var (router, service, _, _) = CreateRouterWithStub();
        var payload = JsonDocument.Parse("""
        {
            "idempotencyKey": "idem-key-openfloat",
            "amount": 1000,
            "reasonCodeId": 1,
            "movementType": "OpeningFloat"
        }
        """).RootElement;

        var request = new BridgeRequestEnvelope { Type = "cash.recordMovement", RequestId = "req-openfloat", Version = "v1", Payload = payload };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("INVALID_MOVEMENT_TYPE", response.Error!.Code);
        Assert.False(service.RecordMovementCalled);
    }

    [Fact]
    public async Task HandleCashGetLedger_ReturnsOnlyActiveShiftMovements()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: true);

        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed an active shift movement
        db.LocalCashDrawerMovements.Add(new LocalCashDrawerMovement
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = TerminalId,
            EmployeeId = EmployeeDbId,
            ShiftId = activeShift.Id,
            Amount = 150m,
            TerminalSequence = 1,
            OccurredOn = DateTimeOffset.UtcNow,
            ReasonCodeId = 1,
            IsActive = true,
            Comment = "Active Shift Drop",
            CorrelationId = Guid.NewGuid().ToString("N"),
            IdempotencyKey = "idem-active",
            CreatedBy = OperatorId,
            CreatedOn = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "cash.getLedger", RequestId = "req-ledger-active-only", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("isOpen").GetBoolean());
        var movements = root.GetProperty("movements");
        Assert.Equal(1, movements.GetArrayLength());
        Assert.Equal("Active Shift Drop", movements[0].GetProperty("comment").GetString());
    }

    [Fact]
    public async Task HandleCashGetLedger_IgnoresClosedShiftMovements()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: true);

        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed a closed shift and a movement for it
        var closedShiftId = Guid.NewGuid();
        db.LocalShifts.Add(new LocalShift
        {
            Id = closedShiftId,
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = TerminalId,
            Status = ShiftStatus.Closed,
            OpenedByEmployeeId = EmployeeDbId,
            OpeningCashAmount = 1000m,
            BusinessDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            TerminalSequence = 2,
            IsActive = true,
            CreatedBy = OperatorId,
            CreatedOn = DateTimeOffset.UtcNow.AddDays(-1)
        });

        db.LocalCashDrawerMovements.AddRange(
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(), TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
                EmployeeId = EmployeeDbId, ShiftId = activeShift.Id, Amount = 150m, TerminalSequence = 1,
                OccurredOn = DateTimeOffset.UtcNow, ReasonCodeId = 1, IsActive = true, Comment = "Active Shift Drop",
                CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = "idem-active-2", CreatedBy = OperatorId, CreatedOn = DateTimeOffset.UtcNow
            },
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(), TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
                EmployeeId = EmployeeDbId, ShiftId = closedShiftId, Amount = 250m, TerminalSequence = 2,
                OccurredOn = DateTimeOffset.UtcNow.AddDays(-1), ReasonCodeId = 1, IsActive = true, Comment = "Closed Shift Drop",
                CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = "idem-closed", CreatedBy = OperatorId, CreatedOn = DateTimeOffset.UtcNow.AddDays(-1)
            }
        );
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "cash.getLedger", RequestId = "req-ledger-ignores-closed", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var movements = root.GetProperty("movements");
        Assert.Equal(1, movements.GetArrayLength());
        Assert.Equal("Active Shift Drop", movements[0].GetProperty("comment").GetString());
    }

    [Fact]
    public async Task HandleCashGetLedger_IgnoresOtherTerminalMovements()
    {
        var (router, _, db, _) = CreateRouterWithStub();
        await SetupDatabaseStateAsync(db, openShift: true);

        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed a shift on another terminal and a movement for it
        var otherTerminalShiftId = Guid.NewGuid();
        db.LocalShifts.Add(new LocalShift
        {
            Id = otherTerminalShiftId,
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = 888, // different terminal
            Status = ShiftStatus.Open,
            OpenedByEmployeeId = EmployeeDbId,
            OpeningCashAmount = 1000m,
            BusinessDate = DateOnly.FromDateTime(DateTime.Today),
            TerminalSequence = 1,
            IsActive = true,
            CreatedBy = OperatorId,
            CreatedOn = DateTimeOffset.UtcNow
        });

        db.LocalCashDrawerMovements.AddRange(
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(), TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
                EmployeeId = EmployeeDbId, ShiftId = activeShift.Id, Amount = 150m, TerminalSequence = 1,
                OccurredOn = DateTimeOffset.UtcNow, ReasonCodeId = 1, IsActive = true, Comment = "Current Terminal Drop",
                CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = "idem-current-term", CreatedBy = OperatorId, CreatedOn = DateTimeOffset.UtcNow
            },
            new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(), TenantId = TenantId, LocationId = LocationId, TerminalId = 888,
                EmployeeId = EmployeeDbId, ShiftId = otherTerminalShiftId, Amount = 250m, TerminalSequence = 2,
                OccurredOn = DateTimeOffset.UtcNow, ReasonCodeId = 1, IsActive = true, Comment = "Other Terminal Drop",
                CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = "idem-other-term", CreatedBy = OperatorId, CreatedOn = DateTimeOffset.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "cash.getLedger", RequestId = "req-ledger-ignores-other-term", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var movements = root.GetProperty("movements");
        Assert.Equal(1, movements.GetArrayLength());
        Assert.Equal("Current Terminal Drop", movements[0].GetProperty("comment").GetString());
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private class StubCashControlService : ICashControlService
    {
        public bool RecordMovementCalled { get; private set; }
        public CashControlMovementRequest? LastRecordMovementRequest { get; private set; }
        public CashControlMovementResult RecordMovementResult { get; set; } = new(Success: true);
        public CashDrawerSummaryResult SummaryResult { get; set; } = new(
            IsOpen: true,
            ShiftId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            OpeningFloat: 1000m,
            CashSales: 500m,
            SafeDrops: 200m,
            FloatInjections: 0m,
            ExpectedDrawerBalance: 1300m,
            TransactionCount: 5,
            LastMovementAt: DateTimeOffset.UtcNow,
            AlertCode: "OK",
            AlertMessage: "Normal",
            IsSafeDropRecommended: false,
            IsOverLimit: false,
            CashDrawerLimit: 10000m,
            SafeDropThreshold: 8000m
        );
        public Exception? ExceptionToThrow { get; set; }

        public Task<CashControlMovementResult> RecordMovementAsync(
            CashControlMovementRequest request,
            CancellationToken cancellationToken = default)
        {
            RecordMovementCalled = true;
            LastRecordMovementRequest = request;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(RecordMovementResult);
        }

        public Task<CashDrawerSummaryResult> GetDrawerSummaryAsync(CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(SummaryResult);
        }
    }
}
