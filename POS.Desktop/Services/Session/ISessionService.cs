namespace POS.Desktop.Services.Session;

/// <summary>
/// Defines the contract for managing the current operator session in C# memory.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Gets the current active operator session, or null if no operator is logged in.
    /// </summary>
    OperatorSession? CurrentSession { get; }

    /// <summary>
    /// Gets a value indicating whether an operator session is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Sets the current operator session.
    /// </summary>
    /// <param name="session">The session to activate.</param>
    void StartSession(OperatorSession session);

    /// <summary>
    /// Clears the current operator session.
    /// </summary>
    void ClearSession();
}
