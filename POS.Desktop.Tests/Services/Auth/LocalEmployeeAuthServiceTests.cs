using System;
using System.Threading.Tasks;
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
}
