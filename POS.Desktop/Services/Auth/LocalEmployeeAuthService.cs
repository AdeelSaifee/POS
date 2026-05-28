using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Auth;

/// <summary>
/// Production-grade authentication service validating credentials against local SQLite database tables.
/// Enforces terminal provisioning, tenant isolation, location scoping, and secure PIN hashing.
/// </summary>
public sealed class LocalEmployeeAuthService : IAuthService
{
    private readonly PosLocalDbContext _db;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;
    private readonly IPinVerifier _pinVerifier;
    private readonly ILogger<LocalEmployeeAuthService> _logger;

    public LocalEmployeeAuthService(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisionedTerminalContext,
        IPinVerifier pinVerifier,
        ILogger<LocalEmployeeAuthService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
        _pinVerifier = pinVerifier ?? throw new ArgumentNullException(nameof(pinVerifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AuthResult> ValidatePinAsync(
        string operatorId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        // 1. Unprovisioned terminal must fail closed
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("Authentication failed: Terminal is not provisioned.");
            return new AuthResult(false, null, "TERMINAL_UNPROVISIONED");
        }

        if (string.IsNullOrWhiteSpace(operatorId) || string.IsNullOrWhiteSpace(pin))
        {
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;

        // 2. Fetch employee (TenantId matches automatically via global query filter)
        var employee = await _db.LocalEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeNumber == operatorId && e.IsActive, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Authentication failed: Employee number '{EmployeeNumber}' not found or inactive.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        // Validate employee status
        if (employee.Status != EmployeeStatus.Active)
        {
            _logger.LogWarning("Authentication failed: Employee '{EmployeeNumber}' status is '{Status}'.", operatorId, employee.Status);
            return new AuthResult(false, null, "OPERATOR_INACTIVE");
        }

        // 3. Verify PIN using the secure verifier (do not log PIN/hash/salt)
        if (string.IsNullOrEmpty(employee.PinHash) || string.IsNullOrEmpty(employee.PinSalt) || string.IsNullOrEmpty(employee.PinHashAlgorithm))
        {
            _logger.LogWarning("Authentication failed: Employee '{EmployeeNumber}' has no PIN credentials configured.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        bool isPinValid = _pinVerifier.VerifyPin(pin, employee.PinSalt, employee.PinHash, employee.PinHashAlgorithm);
        if (!isPinValid)
        {
            _logger.LogWarning("Authentication failed: Invalid PIN for employee '{EmployeeNumber}'.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        // 4. Resolve active location role mapping
        var now = DateTime.UtcNow;
        var locationRoleQuery = _db.LocalEmployeeLocationRoles
            .AsNoTracking()
            .Where(r => r.EmployeeId == employee.Id && r.IsActive)
            .Where(r => r.LocationId == currentLocationId || r.LocationId == null)
            .Where(r => r.StartsOn == null || r.StartsOn <= now)
            .Where(r => r.EndsOn == null || r.EndsOn >= now)
            .OrderByDescending(r => r.LocationId == currentLocationId)
            .ThenBy(r => r.Id);

        var locationRole = await locationRoleQuery.FirstOrDefaultAsync(cancellationToken);

        if (locationRole == null)
        {
            _logger.LogWarning("Authentication failed: Employee '{EmployeeNumber}' has no active role mapped to location {LocationId}.", operatorId, currentLocationId);
            return new AuthResult(false, null, "LOCATION_NOT_AUTHORIZED");
        }

        _logger.LogInformation("Authentication successful for employee '{EmployeeNumber}' at location {LocationId} as role '{Role}'.", operatorId, currentLocationId, locationRole.Role);

        // 5. Generate next terminal sequence number
        long nextSequence = 1;
        var lastSession = await _db.LocalTerminalSessions
            .AsNoTracking()
            .Where(s => s.TerminalId == _provisionedTerminalContext.CurrentTerminalId)
            .OrderByDescending(s => s.TerminalSequence)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastSession != null)
        {
            nextSequence = lastSession.TerminalSequence + 1;
        }

        // 6. Create and persist LocalTerminalSession
        var terminalSession = new LocalTerminalSession
        {
            TenantId = employee.TenantId,
            LocationId = currentLocationId,
            TerminalId = _provisionedTerminalContext.CurrentTerminalId,
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            DisplayName = employee.DisplayName,
            Role = locationRole.Role,
            ShiftId = null,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = nextSequence,
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow,
            LoggedOutOn = null,
            MetadataJson = null
        };

        _db.LocalTerminalSessions.Add(terminalSession);
        await _db.SaveChangesAsync(cancellationToken);

        var details = new OperatorDetails(
            OperatorId: employee.EmployeeNumber,
            DisplayName: employee.DisplayName,
            Role: locationRole.Role,
            PermissionSetCode: locationRole.PermissionSetCode,
            LocationId: locationRole.LocationId,
            MustChangePin: employee.MustChangePin,
            SessionId: terminalSession.Id.ToString()
        );

        return new AuthResult(true, details);
    }

    /// <inheritdoc />
    public async Task<AuthResult> ValidateManagerPinAsync(
        string operatorId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        // 1. Unprovisioned terminal must fail closed
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("Manager validation failed: Terminal is not provisioned.");
            return new AuthResult(false, null, "TERMINAL_UNPROVISIONED");
        }

        if (string.IsNullOrWhiteSpace(operatorId) || string.IsNullOrWhiteSpace(pin))
        {
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;

        // 2. Fetch employee
        var employee = await _db.LocalEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeNumber == operatorId && e.IsActive, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Manager validation failed: Employee '{EmployeeNumber}' not found or inactive.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        // Validate employee status
        if (employee.Status != EmployeeStatus.Active)
        {
            _logger.LogWarning("Manager validation failed: Employee '{EmployeeNumber}' status is '{Status}'.", operatorId, employee.Status);
            return new AuthResult(false, null, "OPERATOR_INACTIVE");
        }

        // 3. Verify PIN
        if (string.IsNullOrEmpty(employee.PinHash) || string.IsNullOrEmpty(employee.PinSalt) || string.IsNullOrEmpty(employee.PinHashAlgorithm))
        {
            _logger.LogWarning("Manager validation failed: Employee '{EmployeeNumber}' has no PIN credentials configured.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        bool isPinValid = _pinVerifier.VerifyPin(pin, employee.PinSalt, employee.PinHash, employee.PinHashAlgorithm);
        if (!isPinValid)
        {
            _logger.LogWarning("Manager validation failed: Invalid PIN for employee '{EmployeeNumber}'.", operatorId);
            return new AuthResult(false, null, "INVALID_CREDENTIALS");
        }

        // 4. Resolve active location role mapping which matches Manager or Supervisor
        var now = DateTime.UtcNow;
        var locationRoleQuery = _db.LocalEmployeeLocationRoles
            .AsNoTracking()
            .Where(r => r.EmployeeId == employee.Id && r.IsActive)
            .Where(r => r.LocationId == currentLocationId || r.LocationId == null)
            .Where(r => r.StartsOn == null || r.StartsOn <= now)
            .Where(r => r.EndsOn == null || r.EndsOn >= now)
            .Where(r => r.Role == "Manager" || r.Role == "Supervisor" || r.Role == "manager" || r.Role == "supervisor")
            .OrderByDescending(r => r.LocationId == currentLocationId)
            .ThenBy(r => r.Id);

        var locationRole = await locationRoleQuery.FirstOrDefaultAsync(cancellationToken);

        if (locationRole == null)
        {
            _logger.LogWarning("Manager validation failed: Employee '{EmployeeNumber}' does not have a Manager or Supervisor role at location {LocationId}.", operatorId, currentLocationId);
            return new AuthResult(false, null, "LOCATION_NOT_AUTHORIZED");
        }

        _logger.LogInformation("Manager validation successful for employee '{EmployeeNumber}' at location {LocationId} as role '{Role}'.", operatorId, currentLocationId, locationRole.Role);

        var details = new OperatorDetails(
            OperatorId: employee.EmployeeNumber,
            DisplayName: employee.DisplayName,
            Role: locationRole.Role,
            PermissionSetCode: locationRole.PermissionSetCode,
            LocationId: locationRole.LocationId,
            MustChangePin: employee.MustChangePin
        );

        return new AuthResult(true, details);
    }
}
