using System.Text.Json;

namespace POS.Desktop.Bridge;

/// <summary>
/// Represents the formal v1 envelope for messages sent from JavaScript to C#.
/// </summary>
public sealed record BridgeRequestEnvelope
{
    /// <summary>
    /// Gets the envelope version (e.g., "v1").
    /// </summary>
    public string Version { get; init; } = BridgeEnvelopeVersion.V1;

    /// <summary>
    /// Gets the action or command name (e.g., "catalog.search").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique ID used to correlate the response with this request.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the action-specific parameters.
    /// Uses JsonElement to allow safe, deferred parsing of the underlying JSON.
    /// </summary>
    public JsonElement? Payload { get; init; }

    /// <summary>
    /// Gets optional context metadata (e.g., timestamp, source screen).
    /// </summary>
    public JsonElement? Metadata { get; init; }
}
