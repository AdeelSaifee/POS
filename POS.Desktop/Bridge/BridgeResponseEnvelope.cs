namespace POS.Desktop.Bridge;

/// <summary>
/// Represents the formal v1 envelope for messages sent from C# to JavaScript.
/// </summary>
public sealed record BridgeResponseEnvelope
{
    /// <summary>
    /// Gets the envelope version, matching the request version.
    /// </summary>
    public string Version { get; init; } = BridgeEnvelopeVersion.V1;

    /// <summary>
    /// Gets the action type, matching the request type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the ID echoed from the matching request.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the request was handled successfully.
    /// </summary>
    public bool Ok { get; init; }

    /// <summary>
    /// Gets the result data. Present only if Ok is true.
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    /// Gets the error details. Present only if Ok is false.
    /// </summary>
    public BridgeMessageError? Error { get; init; }

    /// <summary>
    /// Helper to create a successful response.
    /// </summary>
    public static BridgeResponseEnvelope Success(string type, string requestId, object? payload = null)
    {
        return new BridgeResponseEnvelope
        {
            Type = type,
            RequestId = requestId,
            Ok = true,
            Payload = payload,
            Error = null
        };
    }

    /// <summary>
    /// Helper to create an error response.
    /// </summary>
    public static BridgeResponseEnvelope Failure(string type, string requestId, string code, string message, object? details = null)
    {
        return new BridgeResponseEnvelope
        {
            Type = type,
            RequestId = requestId,
            Ok = false,
            Payload = null,
            Error = new BridgeMessageError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}
