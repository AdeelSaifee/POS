using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.CashControl;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Shifts;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.CashControl;

public class CashControlServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private const int TenantId = 1;
    private const int LocationId = 101;
    private const int TerminalId = 999;
    private const string OperatorId = "EMP012";
    private const int EmployeeDbId = 12;
    private const string ManagerId = "MGR001";
    private const int ManagerDbId = 99;

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    private async Task SetupDatabaseStateAsync(
        PosLocalDbContext db,
        bool openSession = true,
        bool openShift = true,
        bool addReasonCodes = true,
        decimal openingFloat = 1000m)
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
            db.LocalReasonCodes.AddRange(rc1, rc2);
        }

        var employee = new LocalEmployee
        {
            Id = EmployeeDbId,
            TenantId = TenantId,
            EmployeeNumber = OperatorId,
            DisplayName = "Adeel Cashier",
            Status = EmployeeStatus.Active,
            IsActive = true
        };
        db.LocalEmployees.Add(employee);

        var manager = new LocalEmployee
        {
            Id = ManagerDbId,
            TenantId = TenantId,
            EmployeeNumber = ManagerId,
            DisplayName = "Adeel Manager",
            Status = EmployeeStatus.Active,
            IsActive = true
        };
        db.LocalEmployees.Add(manager);

        int terminalSessionId = 456;
        if (openSession)
        {
            var session = new LocalTerminalSession
            {
                Id = terminalSessionId,
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                EmployeeId = EmployeeDbId,
                EmployeeNumber = OperatorId,
                DisplayName = "Adeel Cashier",
                Role = "Cashier",
                Status = TerminalSessionStatus.Open,
                LoggedInOn = DateTimeOffset.UtcNow
            };
            db.LocalTerminalSessions.Add(session);
        }

        if (openShift)
        {
            var shift = new LocalShift
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                LocationId = LocationId,
                TerminalId = TerminalId,
                OpenedByEmployeeId = EmployeeDbId,
                BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TerminalSequence = 1,
                Status = ShiftStatus.Open,
                OpeningCashAmount = openingFloat,
                OpenedOn = DateTimeOffset.UtcNow,
                IsActive = true
            };
            db.LocalShifts.Add(shift);

            if (openSession)
            {
                var session = await db.LocalTerminalSessions.FindAsync(terminalSessionId);
                if (session != null)
                {
                    session.ShiftId = shift.Id;
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private OperatorSessionService CreateSessionService(bool active = true, string sessionId = "456")
    {
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        if (active)
        {
            var session = new OperatorSession(
                OperatorId: OperatorId,
                DisplayName: "Adeel Cashier",
                Role: "Cashier",
                LoginTime: DateTimeOffset.UtcNow,
                TerminalId: TerminalId.ToString(),
                SessionId: sessionId
            );
            sessionService.StartSession(session);
        }
        return sessionService;
    }

    private IOptions<ShiftOpenPolicyOptions> CreatePolicyOptions(decimal limit = 25000m, decimal threshold = 20000m)
    {
        return Microsoft.Extensions.Options.Options.Create(new ShiftOpenPolicyOptions
        {
            CashDrawerLimit = limit,
            AutoSafeDropThreshold = threshold
        });
    }

    private class FakeAuthService : IAuthService
    {
        public bool ManagerPinValid { get; set; } = true;
        public string ExpectedManagerId { get; set; } = ManagerId;

        public Task<AuthResult> ValidatePinAsync(string operatorId, string pin, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthResult(true, new OperatorDetails(operatorId, "Operator", "Cashier")));
        }

        public Task<AuthResult> ValidateManagerPinAsync(string operatorId, string pin, CancellationToken cancellationToken = default)
        {
            if (ManagerPinValid && operatorId == ExpectedManagerId && pin == "9999")
            {
                return Task.FromResult(new AuthResult(true, new OperatorDetails(operatorId, "Manager Display", "Manager")));
            }
            return Task.FromResult(new AuthResult(false, null, "INVALID_CREDENTIALS"));
        }
    }

    [Fact]
    public async Task RecordMovementAsync_TerminalUnprovisioned_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateUnprovisionedDbContext();
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext();
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "End of day drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UNPROVISIONED_TERMINAL", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_NoActiveSession_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        var sessionService = CreateSessionService(active: false);
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Safe drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_ACTIVE_SESSION", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_NoOpenShift_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openShift: false);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Safe drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_OPEN_SHIFT", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_InvalidAmount_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 0m,
            ReasonCodeId: 1,
            Comment: "Safe drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_MissingRequiredReasonCode_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 0,
            Comment: "Safe drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("REASON_CODE_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_InvalidReasonCode_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, addReasonCodes: false);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 99,
            Comment: "Safe drop",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_REASON_CODE", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_ValidDrop_PersistsMovement()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500.50m,
            ReasonCodeId: 1,
            Comment: "Safe drop",
            IdempotencyKey: "Key-ValidDrop"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MovementId);
        Assert.Equal(CashDrawerMovementType.Drop, result.MovementType);
        Assert.Equal(500.50m, result.Amount);
        Assert.Equal(1, result.ReasonCodeId);
        Assert.Equal(1, result.TerminalSequence);

        var dbRecord = await db.LocalCashDrawerMovements.FindAsync(result.MovementId.Value);
        Assert.NotNull(dbRecord);
        Assert.Equal(TenantId, dbRecord.TenantId);
        Assert.Equal(LocationId, dbRecord.LocationId);
        Assert.Equal(TerminalId, dbRecord.TerminalId);
        Assert.Equal(500.50m, dbRecord.Amount);
        Assert.Equal(CashDrawerMovementType.Drop, dbRecord.MovementType);
        Assert.Equal("PKR", dbRecord.CurrencyCode);
        Assert.Equal("Safe drop", dbRecord.Comment);
        Assert.Equal("Key-ValidDrop", dbRecord.IdempotencyKey);
    }

    [Fact]
    public async Task RecordMovementAsync_Correction_ReturnsInvalidMovementType()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Correction,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Attempt float injection",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_MOVEMENT_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_Payout_ReturnsInvalidMovementType()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Payout,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Vendor payout",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_MOVEMENT_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_DuplicateIdempotencySamePayload_ReturnsExistingMovement()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "First drop attempt",
            IdempotencyKey: "idem-key-1"
        );

        // Act
        var firstResult = await service.RecordMovementAsync(request);
        var secondResult = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);
        Assert.Equal(firstResult.MovementId, secondResult.MovementId);
        Assert.Equal(firstResult.TerminalSequence, secondResult.TerminalSequence);

        var count = await db.LocalCashDrawerMovements.CountAsync(m => m.IdempotencyKey == "idem-key-1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordMovementAsync_DuplicateIdempotencyDifferentPayload_ReturnsConflict()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request1 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop PKR 500",
            IdempotencyKey: "idem-key-2"
        );

        var request2 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 600m,
            ReasonCodeId: 1,
            Comment: "Drop PKR 500",
            IdempotencyKey: "idem-key-2"
        );

        // Act
        var result1 = await service.RecordMovementAsync(request1);
        var result2 = await service.RecordMovementAsync(request2);

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        Assert.Equal("IDEMPOTENCY_CONFLICT", result2.ErrorCode);

        var count = await db.LocalCashDrawerMovements.CountAsync(m => m.IdempotencyKey == "idem-key-2");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordMovementAsync_CreatesAppendOnlyRows_AndDoesNotMutateExistingRows()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request1 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "First Drop",
            IdempotencyKey: "drop-1"
        );

        var request2 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 800m,
            ReasonCodeId: 1,
            Comment: "Second Drop",
            IdempotencyKey: "drop-2"
        );

        // Act
        var result1 = await service.RecordMovementAsync(request1);
        var result2 = await service.RecordMovementAsync(request2);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(result1.MovementId, result2.MovementId);
        Assert.Equal(1, result1.TerminalSequence);
        Assert.Equal(2, result2.TerminalSequence);

        var count = await db.LocalCashDrawerMovements.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RecordMovementAsync_ManagerPinRequired_WhenReasonCodeRequiresApproval_ReturnsManagerAuthRequired()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1500m,
            ReasonCodeId: 2, // Requires manager approval
            Comment: "High amount drop",
            IdempotencyKey: "key-manager-required"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("MANAGER_AUTH_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_BadManagerPin_Rejected_ReturnsManagerAuthFailed()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService { ManagerPinValid = false }; // Manager validation will fail
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1500m,
            ReasonCodeId: 2, // Requires manager approval
            Comment: "High amount drop",
            IdempotencyKey: "key-manager-failed",
            ManagerOperatorId: ManagerId,
            ManagerPin: "1111" // Bad PIN
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("MANAGER_AUTH_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_ValidManagerPin_PersistsAuthorizedByEmployeeId()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1500m,
            ReasonCodeId: 2, // Requires manager approval
            Comment: "High amount drop",
            IdempotencyKey: "key-manager-success",
            ManagerOperatorId: ManagerId,
            ManagerPin: "9999" // Valid PIN
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MovementId);

        var record = await db.LocalCashDrawerMovements.FindAsync(result.MovementId.Value);
        Assert.NotNull(record);
        Assert.Equal(ManagerDbId, record.AuthorizedByEmployeeId);
    }

    [Fact]
    public async Task RecordMovementAsync_ManagerPinNotRequired_WhenReasonCodeDoesNotRequireApproval_Succeeds()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1, // RequiresManagerApproval = false
            Comment: "Regular drop",
            IdempotencyKey: "key-regular-success"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MovementId);

        var record = await db.LocalCashDrawerMovements.FindAsync(result.MovementId.Value);
        Assert.NotNull(record);
        Assert.Null(record.AuthorizedByEmployeeId);
    }

    [Fact]
    public async Task RecordMovementAsync_DuplicateIdempotencySamePayload_DoesNotRequireManagerPinAgain()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 1500m,
            ReasonCodeId: 2, // Requires manager approval
            Comment: "High amount drop",
            IdempotencyKey: "idem-mgr-bypass",
            ManagerOperatorId: ManagerId,
            ManagerPin: "9999"
        );

        // Act
        var result1 = await service.RecordMovementAsync(request);

        // Change fake service so manager PIN validation fails on retry
        fakeAuth.ManagerPinValid = false;

        var result2 = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success); // Bypasses PIN auth and succeeds directly from database record
        Assert.Equal(result1.MovementId, result2.MovementId);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_ReturnsClosedDrawerResult_WhenNoOpenShift()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openShift: false);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.False(summary.IsOpen);
        Assert.Null(summary.ShiftId);
        Assert.Equal(0m, summary.OpeningFloat);
        Assert.Equal(0m, summary.CashSales);
        Assert.Equal(0m, summary.SafeDrops);
        Assert.Equal(0m, summary.ExpectedDrawerBalance);
        Assert.Equal("CLOSED", summary.AlertCode);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_ReturnsCorrectDrawerBalance()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 1000m);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 5000m, threshold: 4000m);
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        // 1. Seed regular cash tender method
        var tenderCash = new LocalTenderMethod
        {
            Id = 1,
            TenantId = TenantId,
            Code = "CASH",
            Name = "Cash",
            TenderType = "Cash",
            AllowsChange = true
        };
        db.LocalTenderMethods.Add(tenderCash);
        await db.SaveChangesAsync();

        // 2. Seed cash sales and change
        // Sale 1: Cash 500 PKR, Change 50 PKR (Net Cash Sale = 450 PKR)
        var order1Id = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = order1Id, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 1, Status = OrderStatus.Completed, SubtotalAmount = 450m, TotalAmount = 450m,
            ChangeAmount = 50m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-001", IdempotencyKey = "order-idem-1"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = order1Id, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 1, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 500m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        // Sale 2: Cash 300 PKR, Change 0 PKR (Net Cash Sale = 300 PKR)
        var order2Id = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = order2Id, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 2, Status = OrderStatus.Completed, SubtotalAmount = 300m, TotalAmount = 300m,
            ChangeAmount = 0m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-002", IdempotencyKey = "order-idem-2"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = order2Id, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 1, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 300m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        // Total cash sales = 450 + 300 = 750 PKR

        // 3. Seed a Safe Drop of 400 PKR
        db.LocalCashDrawerMovements.Add(new LocalCashDrawerMovement
        {
            Id = Guid.NewGuid(), TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, ReasonCodeId = 1, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 1, MovementType = CashDrawerMovementType.Drop, Amount = 400m, CurrencyCode = "PKR",
            OccurredOn = DateTimeOffset.UtcNow, IdempotencyKey = "key-drop-summary", CorrelationId = Guid.NewGuid().ToString("N"),
            IsActive = true, CreatedBy = "SystemTest", CreatedOn = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.True(summary.IsOpen);
        Assert.Equal(activeShift.Id, summary.ShiftId);
        Assert.Equal(1000m, summary.OpeningFloat);
        Assert.Equal(750m, summary.CashSales);
        Assert.Equal(400m, summary.SafeDrops);
        Assert.Equal(0m, summary.FloatInjections);
        Assert.Equal(1350m, summary.ExpectedDrawerBalance); // 1000 + 750 - 400
        Assert.Equal(3, summary.TransactionCount); // 2 distinct orders + 1 drop
        Assert.NotNull(summary.LastMovementAt);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_NonCashPaymentsIgnored()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 1000m);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        // 1. Seed card tender method (not cash)
        var tenderCard = new LocalTenderMethod
        {
            Id = 2,
            TenantId = TenantId,
            Code = "CARD",
            Name = "Card",
            TenderType = "Card", // Card Type
            AllowsChange = false
        };
        db.LocalTenderMethods.Add(tenderCard);
        await db.SaveChangesAsync();

        // 2. Seed card payment of 500 PKR
        var orderId = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = orderId, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 1, Status = OrderStatus.Completed, SubtotalAmount = 500m, TotalAmount = 500m,
            ChangeAmount = 0m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-003", IdempotencyKey = "order-idem-3"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = orderId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 2, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 500m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal(0m, summary.CashSales); // Card payment is ignored for cash sales
        Assert.Equal(1000m, summary.ExpectedDrawerBalance); // Remains opening float only
        Assert.Equal(0, summary.TransactionCount); // Card sales count is 0
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_AlertState_OK()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 1500m); // Balance 1500 PKR
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 5000m, threshold: 3000m);
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal("OK", summary.AlertCode);
        Assert.False(summary.IsSafeDropRecommended);
        Assert.False(summary.IsOverLimit);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_AlertState_SafeDropRecommended()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 3500m); // Balance 3500 PKR
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 5000m, threshold: 3000m);
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal("SAFE_DROP_RECOMMENDED", summary.AlertCode);
        Assert.True(summary.IsSafeDropRecommended);
        Assert.False(summary.IsOverLimit);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_AlertState_OverLimit()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 5500m); // Balance 5500 PKR
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 5000m, threshold: 3000m);
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal("OVER_LIMIT", summary.AlertCode);
        Assert.True(summary.IsSafeDropRecommended);
        Assert.True(summary.IsOverLimit);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_CaseInsensitiveCashTenderDetection()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 1000m);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed a tender method with casing "caSH" and code "caSH"
        var tenderCashCustom = new LocalTenderMethod
        {
            Id = 5,
            TenantId = TenantId,
            Code = "caSH",
            Name = "Custom Cash",
            TenderType = "caSH",
            AllowsChange = true
        };
        db.LocalTenderMethods.Add(tenderCashCustom);
        await db.SaveChangesAsync();

        // Seed a completed order with this tender method payment
        var orderId = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = orderId, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 1, Status = OrderStatus.Completed, SubtotalAmount = 250m, TotalAmount = 250m,
            ChangeAmount = 0m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-CUSTOM-CASH", IdempotencyKey = "order-idem-custom"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = orderId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 5, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 250m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal(250m, summary.CashSales);
        Assert.Equal(1250m, summary.ExpectedDrawerBalance);
        Assert.Equal(1, summary.TransactionCount);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_NonCompletedCashPaidOrders_Ignored()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 1000m);

        // Seed tender method 1 as Cash
        var tenderCash = new LocalTenderMethod
        {
            Id = 1,
            TenantId = TenantId,
            Code = "CASH",
            Name = "Cash",
            TenderType = "Cash",
            AllowsChange = true
        };
        db.LocalTenderMethods.Add(tenderCash);
        await db.SaveChangesAsync();

        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        // 1. Seed completed order (should be included)
        var order1Id = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = order1Id, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 1, Status = OrderStatus.Completed, SubtotalAmount = 200m, TotalAmount = 200m,
            ChangeAmount = 0m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-OK-ORDER", IdempotencyKey = "order-ok-idem"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = order1Id, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 1, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 200m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        // 2. Seed Draft order with paid cash payment (should be ignored)
        var order2Id = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = order2Id, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 2, Status = OrderStatus.Draft, SubtotalAmount = 300m, TotalAmount = 300m,
            ChangeAmount = 0m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-DRAFT-ORDER", IdempotencyKey = "order-draft-idem"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = order2Id, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 1, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 300m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        // 3. Seed Completed but Inactive (IsActive=false) order (should be ignored)
        var order3Id = Guid.NewGuid();
        db.LocalOrders.Add(new LocalOrder
        {
            Id = order3Id, TenantId = TenantId, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, EmployeeId = EmployeeDbId, BusinessDate = activeShift.BusinessDate,
            TerminalSequence = 3, Status = OrderStatus.Completed, SubtotalAmount = 400m, TotalAmount = 400m,
            ChangeAmount = 0m, IsActive = false, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow,
            ReceiptNumber = "REC-INACTIVE-ORDER", IdempotencyKey = "order-inactive-idem"
        });
        db.LocalPayments.Add(new LocalPayment
        {
            Id = Guid.NewGuid(), TenantId = TenantId, OrderId = order3Id, LocationId = LocationId, TerminalId = TerminalId,
            ShiftId = activeShift.Id, TenderMethodId = 1, PaymentType = PaymentType.Sale, Status = PaymentStatus.Paid,
            Amount = 400m, IsActive = true, CreatedBy = "Adeel", CreatedOn = DateTimeOffset.UtcNow, ProcessedOn = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal(200m, summary.CashSales); // Only completed active order is counted
        Assert.Equal(1200m, summary.ExpectedDrawerBalance);
        Assert.Equal(1, summary.TransactionCount); // Only 1 completed active cash-paid order
    }

    [Fact]
    public async Task RecordMovementAsync_ShiftClosed_ReturnsNoOpenShift()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openShift: false);

        // Add a closed shift
        var closedShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = TerminalId,
            OpenedByEmployeeId = EmployeeDbId,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = ShiftStatus.Closed,
            OpeningCashAmount = 1000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(closedShift);
        await db.SaveChangesAsync();

        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop with closed shift",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_OPEN_SHIFT", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_ShiftOnOtherTerminal_ReturnsNoOpenShift()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openShift: false);

        // Add an open shift on a different terminal
        var otherTerminalShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = 888, // different terminal
            OpenedByEmployeeId = EmployeeDbId,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 1000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(otherTerminalShift);
        await db.SaveChangesAsync();

        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop with other terminal shift",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_OPEN_SHIFT", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_ShiftOnOtherLocation_ReturnsNoOpenShift()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openShift: false);

        // Add an open shift on a different location
        var otherLocationShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            LocationId = 102, // different location
            TerminalId = TerminalId,
            OpenedByEmployeeId = EmployeeDbId,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 1000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(otherLocationShift);
        await db.SaveChangesAsync();

        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop with other location shift",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_OPEN_SHIFT", result.ErrorCode);
    }

    [Fact]
    public async Task RecordMovementAsync_SessionShiftIdNull_ButOpenShiftExists_Succeeds()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openSession: false, openShift: false);

        // Seed shift
        var openShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = TerminalId,
            OpenedByEmployeeId = EmployeeDbId,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 1000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShift);

        // Seed session with ShiftId = null
        var session = new LocalTerminalSession
        {
            Id = 456,
            TenantId = TenantId,
            LocationId = LocationId,
            TerminalId = TerminalId,
            EmployeeId = EmployeeDbId,
            EmployeeNumber = OperatorId,
            DisplayName = "Adeel Cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow,
            ShiftId = null // ShiftId is null
        };
        db.LocalTerminalSessions.Add(session);
        await db.SaveChangesAsync();

        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop with null session shift ID",
            IdempotencyKey: "key-null-session-shift"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(openShift.Id, result.ShiftId);
    }

    [Fact]
    public async Task RecordMovementAsync_ValidDrop_PersistsActiveShiftIdAndBusinessDate()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Valid Drop Active Shift check",
            IdempotencyKey: "key-shift-details-check"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        var dbRecord = await db.LocalCashDrawerMovements.FindAsync(result.MovementId!.Value);
        Assert.NotNull(dbRecord);
        Assert.Equal(activeShift.Id, dbRecord.ShiftId);
        Assert.Equal(activeShift.BusinessDate, dbRecord.BusinessDate);
    }

    [Fact]
    public async Task RecordMovementAsync_ValidDrop_PersistsProvisionedLocationAndTerminal()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Valid Drop Terminal Context check",
            IdempotencyKey: "key-terminal-context-check"
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.True(result.Success);
        var dbRecord = await db.LocalCashDrawerMovements.FindAsync(result.MovementId!.Value);
        Assert.NotNull(dbRecord);
        Assert.Equal(LocationId, dbRecord.LocationId);
        Assert.Equal(TerminalId, dbRecord.TerminalId);
    }

    [Fact]
    public async Task RecordMovementAsync_DuplicateIdempotencyRetry_RemainsShiftSafe()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Idempotency retry shift check",
            IdempotencyKey: "key-idempotency-shift-check"
        );

        // Act & Assert
        var result1 = await service.RecordMovementAsync(request);
        Assert.True(result1.Success);
        Assert.Equal(activeShift.Id, result1.ShiftId);

        var result2 = await service.RecordMovementAsync(request);
        Assert.True(result2.Success);
        Assert.Equal(result1.MovementId, result2.MovementId);
        Assert.Equal(activeShift.Id, result2.ShiftId);
    }

    [Theory]
    [InlineData(CashDrawerMovementType.Correction)]
    [InlineData(CashDrawerMovementType.Payout)]
    [InlineData(CashDrawerMovementType.NoSale)]
    [InlineData(CashDrawerMovementType.OpeningFloat)]
    [InlineData(CashDrawerMovementType.SaleCashIn)]
    [InlineData(CashDrawerMovementType.RefundCashOut)]
    public async Task RecordMovementAsync_NonDropTypes_AreRejected(CashDrawerMovementType nonDropType)
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions();
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var request = new CashControlMovementRequest(
            MovementType: nonDropType,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: $"Drop with {nonDropType}",
            IdempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await service.RecordMovementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_MOVEMENT_TYPE", result.ErrorCode);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_StateTransitionAfterSafeDrop()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 22000m); // Balance starts at 22,000
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 25000m, threshold: 20000m);
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        var activeShift = await db.LocalShifts.FirstAsync();

        // Seed Cash tender method so that summary resolves cash payments
        var tenderCash = new LocalTenderMethod
        {
            Id = 1,
            TenantId = TenantId,
            Code = "CASH",
            Name = "Cash",
            TenderType = "Cash",
            AllowsChange = true
        };
        db.LocalTenderMethods.Add(tenderCash);
        await db.SaveChangesAsync();

        // 1. Verify initial summary shows SAFE_DROP_RECOMMENDED
        var summary1 = await service.GetDrawerSummaryAsync();
        Assert.Equal(22000m, summary1.ExpectedDrawerBalance);
        Assert.Equal("SAFE_DROP_RECOMMENDED", summary1.AlertCode);
        Assert.True(summary1.IsSafeDropRecommended);
        Assert.False(summary1.IsOverLimit);

        // 2. Perform a safe drop of 5,000
        var request = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 5000m,
            ReasonCodeId: 1,
            Comment: "Drop 5000 to move below threshold",
            IdempotencyKey: "key-transition-drop"
        );
        var dropResult = await service.RecordMovementAsync(request);
        Assert.True(dropResult.Success);

        // 3. Verify second summary expected balance is 17,000 and alert code is OK
        var summary2 = await service.GetDrawerSummaryAsync();
        Assert.Equal(17000m, summary2.ExpectedDrawerBalance); // 22000 - 5000
        Assert.Equal("OK", summary2.AlertCode);
        Assert.False(summary2.IsSafeDropRecommended);
        Assert.False(summary2.IsOverLimit);
    }

    [Fact]
    public async Task GetDrawerSummaryAsync_FallsBackToDefaultLimits_WhenConfiguredLimitsInvalid()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(TenantId, LocationId, TerminalId);
        await SetupDatabaseStateAsync(db, openingFloat: 22000m); // Balance 22,000 (exceeds default threshold 20,000)
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var fakeAuth = new FakeAuthService();
        var policy = CreatePolicyOptions(limit: 0m, threshold: -100m); // Invalid limits
        var service = new CashControlService(db, sessionService, provisioningContext, fakeAuth, policy, NullLogger<CashControlService>.Instance);

        // Act
        var summary = await service.GetDrawerSummaryAsync();

        // Assert
        Assert.Equal(25000m, summary.CashDrawerLimit); // Falls back to default 25,000
        Assert.Equal(20000m, summary.SafeDropThreshold); // Falls back to default 20,000
        Assert.Equal("SAFE_DROP_RECOMMENDED", summary.AlertCode); // Exceeds default threshold
        Assert.True(summary.IsSafeDropRecommended);
        Assert.False(summary.IsOverLimit);
    }
}
