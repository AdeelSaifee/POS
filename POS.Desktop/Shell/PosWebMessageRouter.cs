using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Session;

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
}
