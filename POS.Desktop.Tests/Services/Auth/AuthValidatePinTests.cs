using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Session;
using POS.Desktop.Shell;
using Xunit;

namespace POS.Desktop.Tests.Services.Auth;

public class AuthValidatePinTests
{
    private PosWebMessageRouter CreateRouter(
        ISessionService sessionService,
        IAuthService authService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sessionService);
        services.AddSingleton(authService);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
    }

    private JsonElement CreatePayloadElement(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    [Fact]
    public void Router_CanHandle_AuthValidatePin()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        // Act & Assert
        Assert.True(router.CanHandle("auth.validatePin"));
    }

    [Fact]
    public async Task ValidatePin_ValidCredentials_ReturnsIsValidTrue_AndStartsSession()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-1",
            Payload = CreatePayloadElement(new { operatorId = "OP001", pin = "1111" })
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Equal("auth.validatePin", response.Type);
        Assert.Equal("req-val-1", response.RequestId);
        Assert.Null(response.Error);

        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("isValid").GetBoolean());

        var opProp = doc.RootElement.GetProperty("operator");
        Assert.Equal("OP001", opProp.GetProperty("operatorId").GetString());
        Assert.Equal("Adeel Saifee", opProp.GetProperty("displayName").GetString());
        Assert.Equal("Sr. Cashier", opProp.GetProperty("role").GetString());
        Assert.Equal("POS-01", opProp.GetProperty("terminalId").GetString());
        Assert.NotNull(opProp.GetProperty("sessionId").GetString());
        Assert.NotNull(opProp.GetProperty("loginTime").GetString());

        // Verify C# Session is set
        Assert.True(sessionService.IsActive);
        Assert.NotNull(sessionService.CurrentSession);
        Assert.Equal("OP001", sessionService.CurrentSession.OperatorId);
    }

    [Fact]
    public async Task ValidatePin_InvalidPin_ReturnsIsValidFalse_AndDoesNotStartSession()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-2",
            Payload = CreatePayloadElement(new { operatorId = "OP001", pin = "9999" }) // OP001 PIN is 1111, not 9999
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Null(response.Error);

        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("operator").ValueKind);

        // Verify C# Session is NOT set
        Assert.False(sessionService.IsActive);
        Assert.Null(sessionService.CurrentSession);
    }

    [Fact]
    public async Task ValidatePin_UnknownOperator_ReturnsIsValidFalse_AndDoesNotStartSession()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-3",
            Payload = CreatePayloadElement(new { operatorId = "OP-UNKNOWN", pin = "1111" })
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);

        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("operator").ValueKind);

        Assert.False(sessionService.IsActive);
        Assert.Null(sessionService.CurrentSession);
    }

    [Fact]
    public async Task ValidatePin_MalformedPayload_ReturnsStructuredErrorSafely()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-4",
            Payload = CreatePayloadElement(new { wrongField = "OP001" }) // Missing operatorId and pin
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
        Assert.Contains("Required parameters", response.Error.Message);

        // Assert no PIN or sensitive data is inside error details
        var json = JsonSerializer.Serialize(response.Error, BridgeJsonSerializerOptions.Default);
        Assert.DoesNotContain("1111", json);

        Assert.False(sessionService.IsActive);
    }

    [Fact]
    public async Task ValidatePin_NullPayload_ReturnsStructuredErrorSafely()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-5",
            Payload = null
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);

        Assert.False(sessionService.IsActive);
    }

    [Fact]
    public async Task SessionGet_ReflectsActiveSession_AfterSuccessfulAuth()
    {
        // Arrange
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        var authService = new StubAuthService(NullLogger<StubAuthService>.Instance);
        var router = CreateRouter(sessionService, authService);

        var loginRequest = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "auth.validatePin",
            RequestId = "req-val-6",
            Payload = CreatePayloadElement(new { operatorId = "OP001", pin = "1111" })
        };

        var getRequest = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "session.get",
            RequestId = "req-get-6"
        };

        // Act
        var loginResponse = await router.RouteAsync(loginRequest, CancellationToken.None);
        var getResponse = await router.RouteAsync(getRequest, CancellationToken.None);

        // Assert
        Assert.True(loginResponse.Ok);
        Assert.True(getResponse.Ok);

        var json = JsonSerializer.Serialize(getResponse.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());

        var sessionProp = doc.RootElement.GetProperty("currentSession");
        Assert.Equal("OP001", sessionProp.GetProperty("operatorId").GetString());
        Assert.Equal("Adeel Saifee", sessionProp.GetProperty("displayName").GetString());
        Assert.Equal("Sr. Cashier", sessionProp.GetProperty("role").GetString());
    }
}
