namespace POS.Desktop.Bridge;

/// <summary>
/// Represents a structured error within the bridge response envelope.
/// This shape ensures that internal details like stack traces are not leaked to the UI.
/// </summary>
public sealed record BridgeMessageError
{
    /// <summary>
    /// Gets a machine-readable error code (e.g., "UNSUPPORTED_TYPE").
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets a human-readable, operator-safe message explaining the error.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets optional non-sensitive additional error details.
    /// </summary>
    public object? Details { get; init; }
}
