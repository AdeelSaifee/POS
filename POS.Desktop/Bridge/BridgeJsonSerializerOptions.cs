using System.Text.Json;

namespace POS.Desktop.Bridge;

/// <summary>
/// Provides centralized JSON serialization settings for the WebView2 bridge.
/// Ensures C# and JavaScript use compatible camelCase naming conventions.
/// </summary>
public static class BridgeJsonSerializerOptions
{
    /// <summary>
    /// Gets the standard serializer options for the bridge.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        // Explicitly set camelCase naming policy to match JavaScript conventions.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Ensure null fields are preserved in the output JSON for response stability.
        // This ensures 'payload' and 'error' always exist in the JS object per v1 schema.
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };
}
