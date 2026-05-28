using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Session;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.CashControl;

/// <summary>
/// Service implementing local cash control movement validations and SQLite atomic persistence.
/// </summary>
public sealed class CashControlService : ICashControlService
{
    private readonly PosLocalDbContext _db;
    private readonly ISessionService _sessionService;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;
    private readonly ILogger<CashControlService> _logger;

    public CashControlService(
        PosLocalDbContext db,
        ISessionService sessionService,
        IProvisionedTerminalContext provisionedTerminalContext,
        ILogger<CashControlService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CashControlMovementResult> RecordMovementAsync(
        CashControlMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 1. Validate IdempotencyKey is non-empty
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            _logger.LogWarning("Cash control failed: IdempotencyKey is missing.");
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "IDEMPOTENCY_KEY_REQUIRED",
                ErrorMessage: "An idempotency key is required to record a movement.");
        }

        // 2. Validate terminal is provisioned
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("Cash control failed: Terminal is not provisioned.");
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "UNPROVISIONED_TERMINAL",
                ErrorMessage: "The terminal has not been provisioned.");
        }

        int currentTenantId = _provisionedTerminalContext.CurrentTenantId;
        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;
        int currentTerminalId = _provisionedTerminalContext.CurrentTerminalId;

        // 3. Validate active operator session exists
        if (!_sessionService.IsActive || _sessionService.CurrentSession == null)
        {
            _logger.LogWarning("Cash control failed: No active operator session.");
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "NO_ACTIVE_SESSION",
                ErrorMessage: "An active operator session is required to perform cash control.");
        }

        var currentSession = _sessionService.CurrentSession;

        // 4. Validate current session has parseable LocalTerminalSession Id
        if (string.IsNullOrWhiteSpace(currentSession.SessionId) || !int.TryParse(currentSession.SessionId, out int sessionIdInt))
        {
            _logger.LogWarning("Cash control failed: Session ID '{SessionId}' is not a valid integer.", currentSession.SessionId);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "INVALID_SESSION_ID",
                ErrorMessage: "The current session identifier is invalid.");
        }

        // 5. Load LocalTerminalSession by Id & validate open
        var terminalSession = await _db.LocalTerminalSessions
            .FirstOrDefaultAsync(s => s.Id == sessionIdInt, cancellationToken);

        if (terminalSession == null)
        {
            _logger.LogWarning("Cash control failed: Terminal session with ID '{SessionId}' not found.", sessionIdInt);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "NO_ACTIVE_SESSION",
                ErrorMessage: "The terminal session is not open.");
        }

        if (terminalSession.Status != TerminalSessionStatus.Open)
        {
            _logger.LogWarning("Cash control failed: Terminal session with ID '{SessionId}' is closed.", sessionIdInt);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "SESSION_CLOSED",
                ErrorMessage: "The terminal session is closed.");
        }

        // 6. Validate LocalTerminalSession matches current tenant/location/terminal context
        if (terminalSession.TenantId != currentTenantId ||
            terminalSession.LocationId != currentLocationId ||
            terminalSession.TerminalId != currentTerminalId)
        {
            _logger.LogWarning("Cash control failed: Terminal session context mismatch.");
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "SESSION_MISMATCH",
                ErrorMessage: "The active session context does not match the provisioned terminal.");
        }

        // 7. Resolve LocalEmployee from current session OperatorId
        var employee = await _db.LocalEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeNumber == currentSession.OperatorId && e.TenantId == currentTenantId, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Cash control failed: Employee '{EmployeeNumber}' not found.", currentSession.OperatorId);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "EMPLOYEE_NOT_FOUND",
                ErrorMessage: "Operator not found in database.");
        }

        // 8. Validate active open LocalShift exists
        var activeShift = await _db.LocalShifts
            .FirstOrDefaultAsync(s => s.LocationId == currentLocationId && s.TerminalId == currentTerminalId && s.Status == ShiftStatus.Open, cancellationToken);

        if (activeShift == null)
        {
            _logger.LogWarning("Cash control failed: No active open shift exists on location {LocationId} and terminal {TerminalId}.", currentLocationId, currentTerminalId);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "NO_OPEN_SHIFT",
                ErrorMessage: "No active shift is open on this terminal.");
        }

        // 9. Validate amount > 0
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Cash control failed: Amount must be greater than zero. Received: {Amount}", request.Amount);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "INVALID_AMOUNT",
                ErrorMessage: "The cash amount must be greater than zero.");
        }

        // 10. Validate MovementType (Drop only)
        if (request.MovementType != CashDrawerMovementType.Drop)
        {
            _logger.LogWarning("Cash control failed: Unsupported movement type '{MovementType}'.", request.MovementType);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "INVALID_MOVEMENT_TYPE",
                ErrorMessage: "Only safe drop operations are allowed.");
        }

        // 11. Validate ReasonCodeId > 0
        if (request.ReasonCodeId <= 0)
        {
            _logger.LogWarning("Cash control failed: ReasonCodeId is invalid. Received: {ReasonCodeId}", request.ReasonCodeId);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "REASON_CODE_REQUIRED",
                ErrorMessage: "A valid reason code is required.");
        }

        // 12. Validate LocalReasonCode exists
        var reasonCodeExists = await _db.LocalReasonCodes
            .AnyAsync(r => r.Id == request.ReasonCodeId && r.TenantId == currentTenantId, cancellationToken);

        if (!reasonCodeExists)
        {
            _logger.LogWarning("Cash control failed: Reason code '{ReasonCodeId}' does not exist.", request.ReasonCodeId);
            return new CashControlMovementResult(
                Success: false,
                ErrorCode: "INVALID_REASON_CODE",
                ErrorMessage: "The specified reason code is invalid or does not exist.");
        }

        // 13. Early Idempotency Check
        var existingMovement = await _db.LocalCashDrawerMovements
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == currentTenantId && m.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingMovement != null)
        {
            if (IsPayloadMatch(existingMovement, request, activeShift.Id, employee.Id))
            {
                _logger.LogInformation("Cash control success: Returning existing movement for key '{IdempotencyKey}'", request.IdempotencyKey);
                return BuildSuccessResult(existingMovement);
            }
            else
            {
                _logger.LogWarning("Cash control failed: Idempotency conflict for key '{IdempotencyKey}'", request.IdempotencyKey);
                return new CashControlMovementResult(
                    Success: false,
                    ErrorCode: "IDEMPOTENCY_CONFLICT",
                    ErrorMessage: "A movement with this idempotency key already exists with a different payload.");
            }
        }

        // 14. Persistence with isolated SQLite Transaction
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Compute next TerminalSequence
            long nextSequence = 1;
            var maxSequence = await _db.LocalCashDrawerMovements
                .Where(m => m.LocationId == currentLocationId && m.TerminalId == currentTerminalId)
                .MaxAsync(m => (long?)m.TerminalSequence, cancellationToken);

            if (maxSequence.HasValue)
            {
                nextSequence = maxSequence.Value + 1;
            }

            var newMovement = new LocalCashDrawerMovement
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                LocationId = currentLocationId,
                TerminalId = currentTerminalId,
                ShiftId = activeShift.Id,
                EmployeeId = employee.Id,
                AuthorizedByEmployeeId = null, // Deferred to Task 5.5.4 (Group 2)
                ReasonCodeId = request.ReasonCodeId,
                BusinessDate = activeShift.BusinessDate,
                TerminalSequence = nextSequence,
                MovementType = CashDrawerMovementType.Drop,
                Amount = request.Amount,
                CurrencyCode = "PKR",
                Comment = request.Comment,
                OccurredOn = DateTimeOffset.UtcNow,
                IdempotencyKey = request.IdempotencyKey,
                CorrelationId = Guid.NewGuid().ToString("N"),
                IsActive = true,
                CreatedBy = employee.EmployeeNumber,
                CreatedOn = DateTimeOffset.UtcNow
            };

            _db.LocalCashDrawerMovements.Add(newMovement);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Cash control success: Created new safe drop movement {MovementId} with sequence {TerminalSequence}.",
                newMovement.Id, newMovement.TerminalSequence);

            return BuildSuccessResult(newMovement);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning("Cash control: Unique constraint race encountered for key '{IdempotencyKey}'. Reloading...", request.IdempotencyKey);

            var reloadedMovement = await _db.LocalCashDrawerMovements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.TenantId == currentTenantId && m.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (reloadedMovement != null)
            {
                if (IsPayloadMatch(reloadedMovement, request, activeShift.Id, employee.Id))
                {
                    return BuildSuccessResult(reloadedMovement);
                }
                else
                {
                    return new CashControlMovementResult(
                        Success: false,
                        ErrorCode: "IDEMPOTENCY_CONFLICT",
                        ErrorMessage: "A movement with this idempotency key already exists with a different payload.");
                }
            }

            throw;
        }
    }

    private static bool IsPayloadMatch(LocalCashDrawerMovement existing, CashControlMovementRequest request, Guid shiftId, int employeeId)
    {
        return existing.MovementType == request.MovementType &&
               existing.Amount == request.Amount &&
               existing.ReasonCodeId == request.ReasonCodeId &&
               existing.Comment == request.Comment &&
               existing.ShiftId == shiftId &&
               existing.EmployeeId == employeeId;
    }

    private static CashControlMovementResult BuildSuccessResult(LocalCashDrawerMovement m)
    {
        return new CashControlMovementResult(
            Success: true,
            MovementId: m.Id,
            MovementType: m.MovementType,
            Amount: m.Amount,
            ReasonCodeId: m.ReasonCodeId,
            ShiftId: m.ShiftId,
            BusinessDate: m.BusinessDate,
            TerminalSequence: m.TerminalSequence,
            OccurredOn: m.OccurredOn);
    }

    private bool IsUniqueConstraintViolation(Exception ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner.Message.Contains("UNIQUE constraint failed") ||
                (inner.Message.Contains("SqliteException") && inner.Message.Contains("19")))
            {
                return true;
            }
            inner = inner.InnerException;
        }
        return false;
    }
}
