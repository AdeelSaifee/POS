namespace POS.Desktop.Services.Sync;

/// <summary>
/// Mapped outcomes of outbox events Central API acknowledgment application.
/// </summary>
public sealed class SyncAckApplyResult
{
    /// <summary>
    /// Gets a value indicating whether the application succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the number of rows successfully marked as Acked.
    /// </summary>
    public int AckedRowCount { get; }

    /// <summary>
    /// Gets the chunk sequence that was processed and advanced.
    /// </summary>
    public long? LastAckedChunkSequence { get; }

    /// <summary>
    /// Gets the error code if application failed.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the detailed error message if application failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private SyncAckApplyResult(
        bool success,
        int ackedRowCount,
        long? lastAckedChunkSequence,
        string? errorCode,
        string? errorMessage)
    {
        Success = success;
        AckedRowCount = ackedRowCount;
        LastAckedChunkSequence = lastAckedChunkSequence;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Factory for successful application.
    /// </summary>
    public static SyncAckApplyResult Succeeded(int ackedRowCount, long lastAckedChunkSequence)
    {
        return new SyncAckApplyResult(true, ackedRowCount, lastAckedChunkSequence, null, null);
    }

    /// <summary>
    /// Factory for failed application.
    /// </summary>
    public static SyncAckApplyResult Failed(string errorCode, string errorMessage)
    {
        return new SyncAckApplyResult(false, 0, null, errorCode, errorMessage);
    }
}
