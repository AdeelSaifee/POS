using System;

namespace POS.Api.Application.Sync;

/// <summary>
/// Exception thrown when a synchronization ingest conflict occurs centrally.
/// </summary>
public sealed class SyncConflictException : Exception
{
    /// <summary>
    /// Gets the error code associated with this conflict.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncConflictException"/>.
    /// </summary>
    /// <param name="errorCode">The unique error code identifying the conflict type.</param>
    /// <param name="message">The description of the conflict.</param>
    public SyncConflictException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
