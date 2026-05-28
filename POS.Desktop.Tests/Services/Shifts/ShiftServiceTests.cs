using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Shifts;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Shifts;

public class ShiftServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    [Fact]
    public async Task OpenShiftAsync_Succeeds_WithValidState()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;

        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        // 1. Setup local terminal session in SQLite
        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            ShiftId = null,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        // 2. Setup in-memory operator session
        var inMemorySession = new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        );
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(inMemorySession);

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
        Assert.NotNull(result.Shift);
        Assert.Equal(5000m, result.Shift.OpeningCashAmount);
        Assert.Equal(ShiftStatus.Open, result.Shift.Status);
        Assert.Equal(1, result.Shift.TerminalSequence); // First shift sequence starts at 1
        Assert.Equal(12, result.Shift.OpenedByEmployeeId);
        Assert.Equal(tenantId, result.Shift.TenantId);
        Assert.Equal(locationId, result.Shift.LocationId);
        Assert.Equal(terminalId, result.Shift.TerminalId);
        Assert.False(string.IsNullOrWhiteSpace(result.Shift.IdempotencyKey));
        Assert.False(string.IsNullOrWhiteSpace(result.Shift.CorrelationId));

        // Verify the terminal session was linked to the shift
        var updatedSession = await db.LocalTerminalSessions.FindAsync(terminalSession.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(result.Shift.Id, updatedSession.ShiftId);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenTerminalUnprovisioned()
    {
        // Arrange
        using var db = _dbHarness.CreateUnprovisionedDbContext();
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var provisioningContext = new ProvisionedTerminalContext(); // Unprovisioned by default
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TERMINAL_UNPROVISIONED", result.ErrorCode);
        Assert.Null(result.Shift);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenNoActiveSession()
    {
        // Arrange
        int tenantId = 1;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId);
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance); // No active session
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, 101, 999));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NO_ACTIVE_SESSION", result.ErrorCode);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenSessionNotFoundInDb()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: "99999" // Non-existent Session ID
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenSessionClosed()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Closed, // Session is closed
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_CLOSED", result.ErrorCode);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenSessionContextMismatch()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = 999, // Mismatch location
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_MISMATCH", result.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task OpenShiftAsync_Fails_WhenOpeningFloatZeroOrNegative(decimal invalidFloat)
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(invalidFloat);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_OPENING_FLOAT", result.ErrorCode);
    }

    [Fact]
    public async Task OpenShiftAsync_Fails_WhenShiftAlreadyOpen()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);

        // Setup an already active open shift
        var openShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 3000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShift);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SHIFT_ALREADY_OPEN", result.ErrorCode);
    }

    [Fact]
    public async Task OpenShiftAsync_IncrementsTerminalSequence()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);

        // Add a previous closed shift
        var previousShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Closed,
            OpeningCashAmount = 3000m,
            TerminalSequence = 7, // Sequence 7
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(previousShift);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Shift);
        Assert.Equal(8, result.Shift.TerminalSequence); // Should increment previous sequence 7 -> 8
    }

    [Fact]
    public async Task GetCurrentShiftAsync_ReturnsIsOpenTrue_AfterShiftOpened()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);

        var openShift = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 3000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShift);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.GetCurrentShiftAsync();

        // Assert
        Assert.True(result.IsOpen);
        Assert.Equal(openShift.Id, result.ShiftId);
        Assert.Equal(openShift.OpeningCashAmount, result.OpeningFloat);
        Assert.Equal("Open", result.Status);
    }

    [Fact]
    public async Task GetCurrentShiftAsync_ReturnsIsOpenFalse_WhenNoShiftOpen()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.GetCurrentShiftAsync();

        // Assert
        Assert.False(result.IsOpen);
        Assert.Null(result.ShiftId);
    }

    [Fact]
    public async Task GetCurrentShiftAsync_ReturnsIsOpenFalse_WhenTerminalUnprovisioned()
    {
        // Arrange
        using var db = _dbHarness.CreateUnprovisionedDbContext();
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var provisioningContext = new ProvisionedTerminalContext(); // Unprovisioned by default
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.GetCurrentShiftAsync();

        // Assert
        Assert.False(result.IsOpen);
        Assert.Null(result.ShiftId);
    }

    [Fact]
    public async Task GetCurrentShiftAsync_IgnoresOpenShiftFromDifferentLocation()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);

        // Setup an open shift from a different location but same terminalId and tenantId
        var openShiftFromDiffLocation = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 999, // Different location
            TerminalId = terminalId,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 3000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShiftFromDiffLocation);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.GetCurrentShiftAsync();

        // Assert
        Assert.False(result.IsOpen);
        Assert.Null(result.ShiftId);
    }

    [Fact]
    public async Task OpenShiftAsync_DoubleOpenGuard_AllowsShiftOnDifferentLocation()
    {
        // Arrange
        int tenantId = 1;
        int locationId = 101;
        int terminalId = 999;
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId, locationId, terminalId);

        var terminalSession = new LocalTerminalSession
        {
            TenantId = tenantId,
            LocationId = locationId,
            TerminalId = terminalId,
            EmployeeId = 12,
            EmployeeNumber = "EMP012",
            DisplayName = "Adeel cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow
        };
        db.LocalTerminalSessions.Add(terminalSession);

        // Setup an open shift from a different location but same terminalId and tenantId
        var openShiftFromDiffLocation = new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = 999, // Different location
            TerminalId = terminalId,
            OpenedByEmployeeId = 12,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 3000m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.LocalShifts.Add(openShiftFromDiffLocation);
        await db.SaveChangesAsync();

        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(new OperatorSession(
            OperatorId: "EMP012",
            DisplayName: "Adeel cashier",
            Role: "Cashier",
            LoginTime: DateTimeOffset.UtcNow,
            TerminalId: terminalId.ToString(),
            SessionId: terminalSession.Id.ToString()
        ));

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(tenantId, locationId, terminalId));
        var shiftService = new ShiftService(db, sessionService, provisioningContext, NullLogger<ShiftService>.Instance);

        // Act
        var result = await shiftService.OpenShiftAsync(5000m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Shift);
        Assert.Equal(ShiftStatus.Open, result.Shift.Status);
    }
}
