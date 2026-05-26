using System;
using Microsoft.Extensions.Logging;

namespace POS.Desktop.Services.Session;

/// <summary>
/// In-memory implementation of the operator session service.
/// Provides a simple, process-lifetime store for the current operator's identity and login state.
/// </summary>
public sealed class OperatorSessionService : ISessionService
{
    private readonly ILogger<OperatorSessionService> _logger;
    private OperatorSession? _currentSession;

    public OperatorSessionService(ILogger<OperatorSessionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public OperatorSession? CurrentSession => _currentSession;

    /// <inheritdoc />
    public bool IsActive => _currentSession != null;

    /// <inheritdoc />
    public void StartSession(OperatorSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));

        _currentSession = session;
        _logger.LogInformation("Operator session started for {OperatorId} ({DisplayName}) as {Role}.",
            session.OperatorId, session.DisplayName, session.Role);
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        if (_currentSession != null)
        {
            _logger.LogInformation("Operator session cleared for {OperatorId}.", _currentSession.OperatorId);
            _currentSession = null;
        }
    }
}
