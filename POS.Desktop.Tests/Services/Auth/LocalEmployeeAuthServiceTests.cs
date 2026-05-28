using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Auth;

public class LocalEmployeeAuthServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private readonly PinVerifier _pinVerifier = new();

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    [Fact]
    public async Task ValidatePinAsync_Succeeds_WithValidEmployeeAndRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true,
            MustChangePin = false
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.NotNull(result.Operator);
        Assert.Equal("OP001", result.Operator.OperatorId);
        Assert.Equal("Adeel Cashier", result.Operator.DisplayName);
        Assert.Equal("Cashier", result.Operator.Role);
        Assert.Equal(101, result.Operator.LocationId);
        Assert.False(result.Operator.MustChangePin);
    }

    [Fact]
    public async Task ValidatePinAsync_Fails_WithWrongPin()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "2222");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        Assert.Null(result.Operator);
    }

    [Fact]
    public async Task ValidatePinAsync_Fails_WithUnknownOperator()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("UNKNOWN", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        Assert.Null(result.Operator);
    }

    [Fact]
    public async Task ValidatePinAsync_Fails_WithInactiveOperator()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Suspended, // Inactive status
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("OPERATOR_INACTIVE", result.ErrorCode);
        Assert.Null(result.Operator);
    }

    [Fact]
    public async Task ValidatePinAsync_Fails_WithWrongLocation()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 202, // Different location
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("LOCATION_NOT_AUTHORIZED", result.ErrorCode);
        Assert.Null(result.Operator);
    }

    [Fact]
    public async Task ValidatePinAsync_FailsClosed_WhenTerminalUnprovisioned()
    {
        // Arrange
        using var db = _dbHarness.CreateUnprovisionedDbContext();
        var credentials = _pinVerifier.HashPin("1111");

        // Seed data under TenantId = 1, which the unprovisioned filter (TenantId == 0) will hide anyway
        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(); // Unprovisioned
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("TERMINAL_UNPROVISIONED", result.ErrorCode);
        Assert.Null(result.Operator);
    }

    [Fact]
    public async Task ValidatePinAsync_Succeeds_WithDateBoundedRole_Active()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            StartsOn = DateTime.UtcNow.AddDays(-1), // active
            EndsOn = DateTime.UtcNow.AddDays(1), // active
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidatePinAsync_Fails_WithDateBoundedRole_InFuture()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            StartsOn = DateTime.UtcNow.AddDays(1), // Starts tomorrow (inactive today)
            EndsOn = null,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("LOCATION_NOT_AUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task ValidatePinAsync_Succeeds_WithNullStartsOnAndEndsOn()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            StartsOn = null, // treats as active
            EndsOn = null,   // treats as active (no expiry)
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateManagerPinAsync_Succeeds_ForManagerRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("9999");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 11,
            TenantId = 1,
            EmployeeNumber = "MGR001",
            DisplayName = "Zainab Manager",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 101,
            TenantId = 1,
            EmployeeId = 11,
            LocationId = 101,
            Role = "Manager",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidateManagerPinAsync("MGR001", "9999");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Operator);
        Assert.Equal("Manager", result.Operator!.Role);
    }

    [Fact]
    public async Task ValidateManagerPinAsync_Fails_ForNormalCashierRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier", // Cashier role, not Manager/Supervisor
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidateManagerPinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("LOCATION_NOT_AUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task ValidatePinAsync_PersistsSession_AndIncrementsSequence_OnSuccess()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true,
            MustChangePin = false
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act - First successful login
        var result1 = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.True(result1.IsValid);
        Assert.NotNull(result1.Operator);
        Assert.NotNull(result1.Operator.SessionId);

        // Verify the persisted session row
        var sessions = await db.LocalTerminalSessions.ToListAsync();
        Assert.Single(sessions);
        var firstSession = sessions[0];
        Assert.Equal(1, firstSession.TenantId);
        Assert.Equal(101, firstSession.LocationId);
        Assert.Equal(999, firstSession.TerminalId);
        Assert.Equal(10, firstSession.EmployeeId);
        Assert.Equal("OP001", firstSession.EmployeeNumber);
        Assert.Equal("Adeel Cashier", firstSession.DisplayName);
        Assert.Equal("Cashier", firstSession.Role);
        Assert.Null(firstSession.ShiftId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), firstSession.BusinessDate);
        Assert.Equal(1, firstSession.TerminalSequence);
        Assert.Equal(TerminalSessionStatus.Open, firstSession.Status);
        Assert.True(firstSession.LoggedInOn <= DateTimeOffset.UtcNow);
        Assert.Null(firstSession.LoggedOutOn);
        Assert.Null(firstSession.MetadataJson);

        Assert.Equal(firstSession.Id.ToString(), result1.Operator.SessionId);

        // Act - Second successful login (different request using same db/context)
        var result2 = await authService.ValidatePinAsync("OP001", "1111");

        // Assert second login sequence increments
        Assert.True(result2.IsValid);
        Assert.NotNull(result2.Operator);
        var sessionsAfter = await db.LocalTerminalSessions.ToListAsync();
        Assert.Equal(2, sessionsAfter.Count);
        var secondSession = sessionsAfter.Find(s => s.Id != firstSession.Id);
        Assert.NotNull(secondSession);
        Assert.Equal(2, secondSession.TerminalSequence);
        Assert.Equal(secondSession.Id.ToString(), result2.Operator!.SessionId);
    }

    [Fact]
    public async Task ValidatePinAsync_DoesNotPersistSession_OnFailure()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act & Assert for wrong PIN
        var resultWrongPin = await authService.ValidatePinAsync("OP001", "wrong");
        Assert.False(resultWrongPin.IsValid);
        Assert.Empty(await db.LocalTerminalSessions.ToListAsync());

        // Act & Assert for unknown operator
        var resultUnknown = await authService.ValidatePinAsync("UNKNOWN", "1111");
        Assert.False(resultUnknown.IsValid);
        Assert.Empty(await db.LocalTerminalSessions.ToListAsync());

        // Act & Assert for unprovisioned terminal
        var unprovisionedContext = new ProvisionedTerminalContext(); // unprovisioned
        var unprovisionedAuthService = new LocalEmployeeAuthService(db, unprovisionedContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);
        var resultUnprovisioned = await unprovisionedAuthService.ValidatePinAsync("OP001", "1111");
        Assert.False(resultUnprovisioned.IsValid);
        Assert.Empty(await db.LocalTerminalSessions.ToListAsync());
    }

    [Fact]
    public async Task ValidatePinAsync_PrefersExactLocationRole_OverGlobalRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        // Add a global role (LocationId = null) and an exact location role (LocationId = 101)
        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100, // lower ID
            TenantId = 1,
            EmployeeId = 10,
            LocationId = null, // Global
            Role = "GlobalCashier",
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 101, // higher ID
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101, // Exact
            Role = "ExactCashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Operator);
        Assert.Equal("ExactCashier", result.Operator.Role);
    }

    [Fact]
    public async Task ValidatePinAsync_FailsClosed_WithEmptyLocalEmployeesTable()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        // Leave LocalEmployees table empty

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        Assert.Null(result.Operator);

        // Verify no session is created
        var sessions = await db.LocalTerminalSessions.ToListAsync();
        Assert.Empty(sessions);
    }

    [Theory]
    [InlineData(true, false, false)] // Missing PinHash
    [InlineData(false, true, false)] // Missing PinSalt
    [InlineData(false, false, true)] // Missing PinHashAlgorithm
    public async Task ValidatePinAsync_FailsClosed_WithMissingPinHashOrSaltOrAlgorithm(
        bool missingHash,
        bool missingSalt,
        bool missingAlgorithm)
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = missingHash ? null : "hash",
            PinSalt = missingSalt ? null : "salt",
            PinHashAlgorithm = missingAlgorithm ? null : "PBKDF2",
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
        Assert.Null(result.Operator);

        // Verify no session is created
        var sessions = await db.LocalTerminalSessions.ToListAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task ValidatePinAsync_FailsClosed_WithFutureStartsOnRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            StartsOn = DateTime.UtcNow.AddDays(2), // Future StartsOn
            EndsOn = null,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("LOCATION_NOT_AUTHORIZED", result.ErrorCode);
        Assert.Null(result.Operator);

        // Verify no session is created
        var sessions = await db.LocalTerminalSessions.ToListAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task ValidatePinAsync_FailsClosed_WithExpiredEndsOnRole()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1111");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            StartsOn = null,
            EndsOn = DateTime.UtcNow.AddDays(-2), // Expired EndsOn
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, NullLogger<LocalEmployeeAuthService>.Instance);

        // Act
        var result = await authService.ValidatePinAsync("OP001", "1111");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("LOCATION_NOT_AUTHORIZED", result.ErrorCode);
        Assert.Null(result.Operator);

        // Verify no session is created
        var sessions = await db.LocalTerminalSessions.ToListAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task ValidatePinAsync_DoesNotLogSensitiveData_OnSuccessAndFailure()
    {
        // Arrange
        using var db = _dbHarness.CreateProvisionedDbContext(tenantId: 1, locationId: 101, terminalId: 999);
        var credentials = _pinVerifier.HashPin("1234-SECRET-PIN");

        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = 1,
            EmployeeNumber = "OP001",
            DisplayName = "Adeel Cashier",
            PinHash = credentials.Hash,
            PinSalt = credentials.Salt,
            PinHashAlgorithm = credentials.Algorithm,
            Status = EmployeeStatus.Active,
            IsActive = true
        });

        db.LocalEmployeeLocationRoles.Add(new LocalEmployeeLocationRole
        {
            Id = 100,
            TenantId = 1,
            EmployeeId = 10,
            LocationId = 101,
            Role = "Cashier",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var provisioningContext = new ProvisionedTerminalContext(new ProvisioningRecord(1, 101, 999));
        var testLogger = new TestLogger<LocalEmployeeAuthService>();
        var authService = new LocalEmployeeAuthService(db, provisioningContext, _pinVerifier, testLogger);

        // Act 1: Success login
        var successResult = await authService.ValidatePinAsync("OP001", "1234-SECRET-PIN");
        Assert.True(successResult.IsValid);

        // Act 2: Failure login
        var failureResult = await authService.ValidatePinAsync("OP001", "wrong-pin-5678");
        Assert.False(failureResult.IsValid);

        // Assert over logged messages
        Assert.NotEmpty(testLogger.LoggedMessages);
        foreach (var msg in testLogger.LoggedMessages)
        {
            // Raw PINs must never be logged
            Assert.DoesNotContain("1234-SECRET-PIN", msg);
            Assert.DoesNotContain("wrong-pin-5678", msg);

            // Hash/Salt/Algorithm must never be logged
            Assert.DoesNotContain(credentials.Hash, msg);
            Assert.DoesNotContain(credentials.Salt, msg);
            Assert.DoesNotContain(credentials.Algorithm, msg);
        }
    }
}
