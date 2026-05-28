using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.CashControl;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
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

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    private async Task SetupDatabaseStateAsync(
        PosLocalDbContext db,
        bool openSession = true,
        bool openShift = true,
        bool addReasonCode = true,
        int reasonCodeId = 1)
    {
        if (addReasonCode)
        {
            var reasonCode = new LocalReasonCode
            {
                Id = reasonCodeId,
                TenantId = TenantId,
                Code = "DROP_SAFE",
                Name = "Drop to Safe",
                ReasonCategory = "CashControl",
                RequiresManagerApproval = false,
                SortOrder = 1
            };
            db.LocalReasonCodes.Add(reasonCode);
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
                OpeningCashAmount = 1000m,
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

    [Fact]
    public async Task RecordMovementAsync_TerminalUnprovisioned_ReturnsError()
    {
        // Arrange
        using var db = _dbHarness.CreateUnprovisionedDbContext();
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext();
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        await SetupDatabaseStateAsync(db, addReasonCode: false);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        await SetupDatabaseStateAsync(db, reasonCodeId: 1);
        var sessionService = CreateSessionService();
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(TenantId, LocationId, TerminalId));
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

        var request1 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "Drop PKR 500",
            IdempotencyKey: "idem-key-2"
        );

        var request2 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 600m, // Changed payload amount
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
        var service = new CashControlService(db, sessionService, provisioningContext, NullLogger<CashControlService>.Instance);

        var request1 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 500m,
            ReasonCodeId: 1,
            Comment: "First Drop",
            IdempotencyKey: "drop-1"
        );

        var request2 = new CashControlMovementRequest(
            MovementType: CashDrawerMovementType.Drop,
            Amount: 800m, // Different amount and idempotency key
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

        // Check first record remains unchanged
        var firstRecord = await db.LocalCashDrawerMovements.FindAsync(result1.MovementId.Value);
        Assert.NotNull(firstRecord);
        Assert.Equal(500m, firstRecord.Amount);
        Assert.Equal("First Drop", firstRecord.Comment);

        // Check second record
        var secondRecord = await db.LocalCashDrawerMovements.FindAsync(result2.MovementId.Value);
        Assert.NotNull(secondRecord);
        Assert.Equal(800m, secondRecord.Amount);
        Assert.Equal("Second Drop", secondRecord.Comment);
    }
}
