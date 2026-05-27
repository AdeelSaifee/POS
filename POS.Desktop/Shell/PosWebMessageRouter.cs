using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Provisioning;

namespace POS.Desktop.Shell;

/// <summary>
/// A delegate representing a bridge message handler.
/// </summary>
public delegate Task<BridgeResponseEnvelope> BridgeMessageHandler(BridgeRequestEnvelope request, CancellationToken cancellationToken);

/// <summary>
/// Centralizes the registration and lookup of bridge message handlers.
/// </summary>
public sealed class PosWebMessageRouter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PosWebMessageRouter> _logger;
    private readonly Dictionary<string, Func<IServiceProvider, BridgeMessageHandler>> _handlers = new(StringComparer.Ordinal);

    public PosWebMessageRouter(IServiceScopeFactory scopeFactory, ILogger<PosWebMessageRouter> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Task 3.3.8 & 3.3.9: Register built-in handlers for proving the router foundation.
        // Ergonomic registration pattern: One line, DI-ready.
        // Example for a separate handler class: Register("auth.login", sp => sp.GetRequiredService<LoginHandler>().HandleAsync);
        Register("transport.echo", _ => HandleTransportEchoAsync);

        // Task 3.4.4 - 3.4.6: Session management handlers.
        Register("session.get", sp => (req, ct) => HandleSessionGetAsync(sp.GetRequiredService<ISessionService>(), req, ct));
        Register("session.clear", sp => (req, ct) => HandleSessionClearAsync(sp.GetRequiredService<ISessionService>(), req, ct));

        // Task 3.5.2: Auth validate pin handler
        Register("auth.validatePin", sp => (req, ct) => HandleAuthValidatePinAsync(
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ISessionService>(),
            req,
            ct));

        // Task 4.2.2 & 4.2.3: Provisioning handlers
        Register("provisioning.provisionTerminal", sp => (req, ct) => HandleProvisionTerminalAsync(
            sp.GetRequiredService<ITerminalProvisioningStore>(),
            req,
            ct));
        Register("provisioning.getProvisioningStatus", sp => (req, ct) => HandleGetProvisioningStatusAsync(
            sp.GetRequiredService<ITerminalProvisioningStore>(),
            req,
            ct));
    }

    /// <summary>
    /// Registers a handler factory for a specific message type.
    /// </summary>
    public void Register(string type, Func<IServiceProvider, BridgeMessageHandler> handlerFactory)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Message type cannot be null or empty.", nameof(type));
        }

        if (_handlers.ContainsKey(type))
        {
            throw new InvalidOperationException($"A handler for message type '{type}' is already registered.");
        }

        _handlers[type] = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
    }

    /// <summary>
    /// Determines whether the router has a registered handler for the given type.
    /// </summary>
    public bool CanHandle(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return _handlers.ContainsKey(type);
    }

    /// <summary>
    /// Attempts to retrieve the registered handler factory for the given type.
    /// </summary>
    public bool TryGetHandlerFactory(string type, out Func<IServiceProvider, BridgeMessageHandler> handlerFactory)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            handlerFactory = null!;
            return false;
        }

        return _handlers.TryGetValue(type, out handlerFactory!);
    }

    /// <summary>
    /// Retrieves all registered message types.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredTypes()
    {
        return _handlers.Keys;
    }

    /// <summary>
    /// Routes the incoming request to the appropriate handler within a dedicated dependency injection scope.
    /// Handles dispatch (Task 3.3.5), unknown type mapping (Task 3.3.6), and safe exception recovery (Task 3.3.7).
    /// </summary>
    public async Task<BridgeResponseEnvelope> RouteAsync(BridgeRequestEnvelope request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var type = request.Type;
        var requestId = request.RequestId;

        try
        {
            if (!TryGetHandlerFactory(type, out var factory))
            {
                _logger.LogWarning("Unsupported bridge message type '{Type}' (RequestId: {RequestId}).", type, requestId);

                return BridgeResponseEnvelope.Failure(
                    type: string.IsNullOrWhiteSpace(type) ? "unknown" : type,
                    requestId: string.IsNullOrWhiteSpace(requestId) ? "unrecognized" : requestId,
                    code: "UNSUPPORTED_TYPE",
                    message: "The requested action is not implemented.",
                    details: new { type }
                );
            }

            _logger.LogDebug("Creating DI scope for message type '{Type}' (RequestId: {RequestId}).", type, requestId);
            using var scope = _scopeFactory.CreateScope();

            var handler = factory(scope.ServiceProvider);
            return await handler(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler error for message type '{Type}' (RequestId: {RequestId}).", type, requestId);

            return BridgeResponseEnvelope.Failure(
                type: type,
                requestId: requestId,
                code: "HANDLER_ERROR",
                message: "The requested action could not be completed."
            );
        }
    }

    /// <summary>
    /// A built-in echo handler to prove the router map functions correctly.
    /// </summary>
    private Task<BridgeResponseEnvelope> HandleTransportEchoAsync(BridgeRequestEnvelope request, CancellationToken cancellationToken)
    {
        var response = BridgeResponseEnvelope.Success(
            type: request.Type,
            requestId: request.RequestId,
            payload: new { message = "echo-routed", receivedType = request.Type }
        );

        return Task.FromResult(response);
    }

    /// <summary>
    /// Handles the session.get message, returning the current operator session status.
    /// </summary>
    private Task<BridgeResponseEnvelope> HandleSessionGetAsync(ISessionService sessionService, BridgeRequestEnvelope request, CancellationToken cancellationToken)
    {
        var response = BridgeResponseEnvelope.Success(
            type: request.Type,
            requestId: request.RequestId,
            payload: new
            {
                isActive = sessionService.IsActive,
                currentSession = sessionService.CurrentSession
            }
        );

        return Task.FromResult(response);
    }

    /// <summary>
    /// Handles the session.clear message, clearing the current operator session.
    /// </summary>
    private Task<BridgeResponseEnvelope> HandleSessionClearAsync(ISessionService sessionService, BridgeRequestEnvelope request, CancellationToken cancellationToken)
    {
        sessionService.ClearSession();

        var response = BridgeResponseEnvelope.Success(
            type: request.Type,
            requestId: request.RequestId,
            payload: new
            {
                cleared = true,
                isActive = false
            }
        );

        return Task.FromResult(response);
    }

    /// <summary>
    /// Handles the auth.validatePin message, validating credentials against the stub database
    /// and initializing a session upon success.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleAuthValidatePinAsync(
        IAuthService authService,
        ISessionService sessionService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing.");
        }

        // Deserialize request payload parameters safely
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(request.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);

        if (!doc.RootElement.TryGetProperty("operatorId", out var opIdProp) ||
            !doc.RootElement.TryGetProperty("pin", out var pinProp))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Required parameters 'operatorId' or 'pin' were missing.");
        }

        string operatorId = opIdProp.GetString() ?? string.Empty;
        string pin = pinProp.GetString() ?? string.Empty;

        var result = await authService.ValidatePinAsync(operatorId, pin, cancellationToken);
        if (result.IsValid && result.Operator != null)
        {
            var session = new OperatorSession(
                OperatorId: result.Operator.OperatorId,
                DisplayName: result.Operator.DisplayName,
                Role: result.Operator.Role,
                LoginTime: DateTimeOffset.UtcNow,
                TerminalId: "POS-01",
                SessionId: Guid.NewGuid().ToString()
            );

            sessionService.StartSession(session);

            return BridgeResponseEnvelope.Success(
                type: request.Type,
                requestId: request.RequestId,
                payload: new
                {
                    isValid = true,
                    @operator = session
                }
            );
        }

        return BridgeResponseEnvelope.Success(
            type: request.Type,
            requestId: request.RequestId,
            payload: new
            {
                isValid = false,
                @operator = (OperatorSession?)null
            }
        );
    }

    /// <summary>
    /// Handles the provisioning.provisionTerminal message.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleProvisionTerminalAsync(
        ITerminalProvisioningStore provisioningStore,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing.");
        }

        try
        {
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(request.Payload, BridgeJsonSerializerOptions.Default);
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);

            if (!doc.RootElement.TryGetProperty("tenantId", out var tenantIdProp) ||
                !doc.RootElement.TryGetProperty("locationId", out var locationIdProp) ||
                !doc.RootElement.TryGetProperty("terminalId", out var terminalIdProp))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Required parameters 'tenantId', 'locationId', or 'terminalId' were missing.");
            }

            if (tenantIdProp.ValueKind != System.Text.Json.JsonValueKind.Number ||
                locationIdProp.ValueKind != System.Text.Json.JsonValueKind.Number ||
                terminalIdProp.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "IDs must be numeric.");
            }

            if (!tenantIdProp.TryGetInt32(out var tenantId) ||
                !locationIdProp.TryGetInt32(out var locationId) ||
                !terminalIdProp.TryGetInt32(out var terminalId))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "IDs must be valid 32-bit integers.");
            }

            bool allowReprovision = false;
            if (doc.RootElement.TryGetProperty("allowReprovision", out var allowReprovisionProp))
            {
                if (allowReprovisionProp.ValueKind == System.Text.Json.JsonValueKind.True || allowReprovisionProp.ValueKind == System.Text.Json.JsonValueKind.False)
                {
                    allowReprovision = allowReprovisionProp.GetBoolean();
                }
                else
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "'allowReprovision' must be a boolean.");
                }
            }

            var result = await provisioningStore.ProvisionTerminalAsync(tenantId, locationId, terminalId, allowReprovision, cancellationToken);
            if (!result.Success)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, result.ErrorCode ?? "PROVISIONING_FAILED", result.ErrorMessage ?? "Provisioning failed.");
            }

            return BridgeResponseEnvelope.Success(
                type: request.Type,
                requestId: request.RequestId,
                payload: new { success = true }
            );
        }
        catch (System.Text.Json.JsonException)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
    }

    /// <summary>
    /// Handles the provisioning.getProvisioningStatus message.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleGetProvisioningStatusAsync(
        ITerminalProvisioningStore provisioningStore,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        var record = await provisioningStore.GetProvisioningRecordAsync(cancellationToken);
        bool isProvisioned = record.IsFullyProvisioned;

        return BridgeResponseEnvelope.Success(
            type: request.Type,
            requestId: request.RequestId,
            payload: new
            {
                isProvisioned = isProvisioned,
                tenantId = isProvisioned ? record.TenantId : null,
                locationId = isProvisioned ? record.LocationId : null,
                terminalId = isProvisioned ? record.TerminalId : null,
                updatedAt = isProvisioned ? record.UpdatedAt : null
            }
        );
    }
}
