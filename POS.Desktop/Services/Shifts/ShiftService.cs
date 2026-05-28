using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Session;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Shifts;

/// <summary>
/// Service to manage and persist shift lifecycle events (opening, closing).
/// </summary>
public sealed class ShiftService : IShiftService
{
    private readonly PosLocalDbContext _db;
    private readonly ISessionService _sessionService;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;
    private readonly ILogger<ShiftService> _logger;
    private readonly IOptions<ShiftOpenPolicyOptions> _policyOptions;

    public ShiftService(
        PosLocalDbContext db,
        ISessionService sessionService,
        IProvisionedTerminalContext provisionedTerminalContext,
        ILogger<ShiftService> logger,
        IOptions<ShiftOpenPolicyOptions> policyOptions)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _policyOptions = policyOptions ?? throw new ArgumentNullException(nameof(policyOptions));
    }

    /// <inheritdoc />
    public async Task<ShiftOpenResult> OpenShiftAsync(decimal openingFloat, CancellationToken cancellationToken = default)
    {
        // 1. Terminal must be provisioned
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("Shift open failed: Terminal is not provisioned.");
            return new ShiftOpenResult(false, "TERMINAL_UNPROVISIONED", "The terminal is not provisioned.");
        }

        // 2. Active operator session is required in memory
        if (!_sessionService.IsActive || _sessionService.CurrentSession == null)
        {
            _logger.LogWarning("Shift open failed: No active operator session.");
            return new ShiftOpenResult(false, "NO_ACTIVE_SESSION", "No operator session is active.");
        }

        var currentSession = _sessionService.CurrentSession;

        if (string.IsNullOrWhiteSpace(currentSession.SessionId))
        {
            _logger.LogWarning("Shift open failed: Operator session has no session identifier.");
            return new ShiftOpenResult(false, "INVALID_SESSION_ID", "The current session identifier is invalid.");
        }

        // 3. Resolve active LocalTerminalSession from SQLite and validate
        if (!int.TryParse(currentSession.SessionId, out int sessionIdInt))
        {
            _logger.LogWarning("Shift open failed: Session ID '{SessionId}' is not a valid integer.", currentSession.SessionId);
            return new ShiftOpenResult(false, "INVALID_SESSION_ID", "The current session identifier is not formatted correctly.");
        }

        var terminalSession = await _db.LocalTerminalSessions
            .FirstOrDefaultAsync(s => s.Id == sessionIdInt, cancellationToken);

        if (terminalSession == null)
        {
            _logger.LogWarning("Shift open failed: Terminal session with ID '{SessionId}' not found in DB.", sessionIdInt);
            return new ShiftOpenResult(false, "SESSION_NOT_FOUND", "The active session was not found in the local database.");
        }

        if (terminalSession.Status != TerminalSessionStatus.Open)
        {
            _logger.LogWarning("Shift open failed: Terminal session with ID '{SessionId}' is not open (Current Status: {Status}).", sessionIdInt, terminalSession.Status);
            return new ShiftOpenResult(false, "SESSION_CLOSED", "The active session has already been closed.");
        }

        // Enforce strict tenant/location/terminal isolation boundaries
        if (terminalSession.TenantId != _provisionedTerminalContext.CurrentTenantId ||
            terminalSession.LocationId != _provisionedTerminalContext.CurrentLocationId ||
            terminalSession.TerminalId != _provisionedTerminalContext.CurrentTerminalId)
        {
            _logger.LogWarning("Shift open failed: Terminal session context mismatch.");
            return new ShiftOpenResult(false, "SESSION_MISMATCH", "The active session context does not match the provisioned terminal.");
        }

        // 4. Validate opening float
        if (openingFloat <= 0)
        {
            _logger.LogWarning("Shift open failed: Opening float must be greater than zero. Received: {OpeningFloat}", openingFloat);
            return new ShiftOpenResult(false, "INVALID_OPENING_FLOAT", "The opening float amount must be greater than zero.");
        }

        int currentTerminalId = _provisionedTerminalContext.CurrentTerminalId;
        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;

        // 5. Prevent double-open on this terminal
        var activeShiftExists = await _db.LocalShifts
            .AnyAsync(s => s.LocationId == currentLocationId && s.TerminalId == currentTerminalId && s.Status == ShiftStatus.Open, cancellationToken);

        if (activeShiftExists)
        {
            _logger.LogWarning("Shift open failed: An active open shift already exists on terminal {TerminalId} and location {LocationId}.", currentTerminalId, currentLocationId);
            return new ShiftOpenResult(false, "SHIFT_ALREADY_OPEN", "A shift is already open on this terminal.");
        }

        // 6. Determine next shift terminal sequence
        long nextSequence = 1;
        var lastShift = await _db.LocalShifts
            .AsNoTracking()
            .Where(s => s.LocationId == currentLocationId && s.TerminalId == currentTerminalId)
            .OrderByDescending(s => s.TerminalSequence)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastShift != null)
        {
            nextSequence = lastShift.TerminalSequence + 1;
        }

        // 7. Persist the new LocalShift
        var newShiftId = Guid.NewGuid();
        var localShift = new LocalShift
        {
            Id = newShiftId,
            TenantId = _provisionedTerminalContext.CurrentTenantId,
            LocationId = currentLocationId,
            TerminalId = currentTerminalId,
            OpenedByEmployeeId = terminalSession.EmployeeId,
            ClosedByEmployeeId = null,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = nextSequence,
            Status = ShiftStatus.Open,
            OpeningCashAmount = openingFloat,
            ExpectedCashAmount = null,
            CountedCashAmount = null,
            VarianceAmount = null,
            OpenedOn = DateTimeOffset.UtcNow,
            ClosedOn = null,
            SyncedOn = null,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            IsActive = true,
            CreatedBy = terminalSession.EmployeeNumber,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = null,
            UpdatedOn = null
        };

        _db.LocalShifts.Add(localShift);

        // 8. Link the current LocalTerminalSession to the newly opened shift
        terminalSession.ShiftId = newShiftId;
        _db.LocalTerminalSessions.Update(terminalSession);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shift {ShiftId} successfully opened on terminal {TerminalId} with float {OpeningFloat}.",
            newShiftId, currentTerminalId, openingFloat);

        return new ShiftOpenResult(true, Shift: localShift);
    }

    /// <inheritdoc />
    public async Task<ShiftDetailsResult> GetCurrentShiftAsync(CancellationToken cancellationToken = default)
    {
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("GetCurrentShift failed: Terminal is not provisioned.");
            return new ShiftDetailsResult(false);
        }

        int currentTerminalId = _provisionedTerminalContext.CurrentTerminalId;
        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;

        var openShift = await _db.LocalShifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.LocationId == currentLocationId && s.TerminalId == currentTerminalId && s.Status == ShiftStatus.Open, cancellationToken);

        if (openShift == null)
        {
            return new ShiftDetailsResult(false);
        }

        return new ShiftDetailsResult(
            IsOpen: true,
            ShiftId: openShift.Id,
            BusinessDate: openShift.BusinessDate,
            OpeningFloat: openShift.OpeningCashAmount,
            Status: openShift.Status.ToString()
        );
    }

    /// <inheritdoc />
    public Task<ShiftOpenPolicyResult> GetOpenPolicyAsync(CancellationToken cancellationToken = default)
    {
        var opts = _policyOptions.Value;

        var cashLimit = opts.CashDrawerLimit > 0
            ? opts.CashDrawerLimit
            : ShiftOpenPolicyOptions.DefaultCashDrawerLimit;

        var threshold = opts.AutoSafeDropThreshold > 0
            ? opts.AutoSafeDropThreshold
            : ShiftOpenPolicyOptions.DefaultAutoSafeDropThreshold;

        var rawChecklist = opts.Checklist ?? [];
        var checklist = rawChecklist
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Take(ShiftOpenPolicyOptions.MaxChecklistItems)
            .ToList();

        if (checklist.Count == 0)
        {
            checklist = [..ShiftOpenPolicyOptions.DefaultChecklist()];
        }

        return Task.FromResult(new ShiftOpenPolicyResult(cashLimit, threshold, checklist));
    }
}
