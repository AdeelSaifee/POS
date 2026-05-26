using System.Text.Json;
using POS.Desktop.Bridge;

namespace POS.Desktop.Tests.Bridge;

public class BridgeEnvelopeContractTests
{
    [Fact]
    public void SuccessfulResponse_SerializesStableCamelCaseShape()
    {
        // Arrange
        var payload = new { testKey = "testValue" };
        var response = BridgeResponseEnvelope.Success("test.action", "req-123", payload);

        // Act
        var json = JsonSerializer.Serialize(response, BridgeJsonSerializerOptions.Default);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("version", out var versionElement));
        Assert.Equal("v1", versionElement.GetString());

        Assert.True(root.TryGetProperty("type", out var typeElement));
        Assert.Equal("test.action", typeElement.GetString());

        Assert.True(root.TryGetProperty("requestId", out var reqIdElement));
        Assert.Equal("req-123", reqIdElement.GetString());

        Assert.True(root.TryGetProperty("ok", out var okElement));
        Assert.True(okElement.GetBoolean());

        Assert.True(root.TryGetProperty("payload", out var payloadElement));
        Assert.Equal(JsonValueKind.Object, payloadElement.ValueKind);
        Assert.Equal("testValue", payloadElement.GetProperty("testKey").GetString());

        Assert.True(root.TryGetProperty("error", out var errorElement));
        Assert.Equal(JsonValueKind.Null, errorElement.ValueKind);
    }

    [Fact]
    public void FailedResponse_SerializesStableErrorShape()
    {
        // Arrange
        var response = BridgeResponseEnvelope.Failure("test.action", "req-123", "TEST_ERROR", "Test error message", "Test details");

        // Act
        var json = JsonSerializer.Serialize(response, BridgeJsonSerializerOptions.Default);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("requestId", out _));

        Assert.True(root.TryGetProperty("ok", out var okElement));
        Assert.False(okElement.GetBoolean());

        Assert.True(root.TryGetProperty("payload", out var payloadElement));
        Assert.Equal(JsonValueKind.Null, payloadElement.ValueKind);

        Assert.True(root.TryGetProperty("error", out var errorElement));
        Assert.Equal(JsonValueKind.Object, errorElement.ValueKind);
        
        Assert.True(errorElement.TryGetProperty("code", out var codeElement));
        Assert.Equal("TEST_ERROR", codeElement.GetString());

        Assert.True(errorElement.TryGetProperty("message", out var messageElement));
        Assert.Equal("Test error message", messageElement.GetString());

        Assert.True(errorElement.TryGetProperty("details", out var detailsElement));
        Assert.Equal("Test details", detailsElement.GetString());
        
        // Ensure keys are camelCase
        var expectedKeys = new[] { "version", "type", "requestId", "ok", "payload", "error" };
        var actualKeys = root.EnumerateObject().Select(p => p.Name).ToList();
        Assert.All(expectedKeys, key => Assert.Contains(key, actualKeys));
    }

    [Fact]
    public void RequestEnvelope_DeserializesCamelCaseV1Json()
    {
        // Arrange
        var json = """
        {
            "version": "v1",
            "type": "test.action",
            "requestId": "req-123",
            "payload": {
                "someData": "value"
            },
            "metadata": {
                "source": "ui"
            }
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<BridgeRequestEnvelope>(json, BridgeJsonSerializerOptions.Default);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("v1", request.Version);
        Assert.Equal("test.action", request.Type);
        Assert.Equal("req-123", request.RequestId);
        
        Assert.NotNull(request.Payload);
        Assert.Equal("value", request.Payload.Value.GetProperty("someData").GetString());

        Assert.NotNull(request.Metadata);
        Assert.Equal("ui", request.Metadata.Value.GetProperty("source").GetString());

        // Re-serialize and verify camelCase
        var reserialized = JsonSerializer.Serialize(request, BridgeJsonSerializerOptions.Default);
        using var document = JsonDocument.Parse(reserialized);
        var root = document.RootElement;
        
        Assert.True(root.TryGetProperty("requestId", out _));
        Assert.True(root.TryGetProperty("payload", out _));
        Assert.True(root.TryGetProperty("metadata", out _));
    }

    [Fact]
    public void VersionConstant_RemainsV1()
    {
        // Assert
        Assert.Equal("v1", BridgeEnvelopeVersion.V1);
    }

    [Fact]
    public void NullPreservation_IncludesPayloadAndErrorPropertiesWhenNull()
    {
        // Arrange
        var successResponse = BridgeResponseEnvelope.Success("test.action", "req-123", payload: null);
        var failureResponse = BridgeResponseEnvelope.Failure("test.action", "req-123", "E", "M");

        // Act
        var successJson = JsonSerializer.Serialize(successResponse, BridgeJsonSerializerOptions.Default);
        var failureJson = JsonSerializer.Serialize(failureResponse, BridgeJsonSerializerOptions.Default);

        using var successDoc = JsonDocument.Parse(successJson);
        using var failureDoc = JsonDocument.Parse(failureJson);

        // Assert
        Assert.True(successDoc.RootElement.TryGetProperty("error", out var successError));
        Assert.Equal(JsonValueKind.Null, successError.ValueKind);

        Assert.True(failureDoc.RootElement.TryGetProperty("payload", out var failurePayload));
        Assert.Equal(JsonValueKind.Null, failurePayload.ValueKind);
    }
}
