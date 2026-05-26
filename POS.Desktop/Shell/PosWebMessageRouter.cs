using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Desktop.Bridge;

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

        // Built-in handlers for proving the router foundation without business logic.
        Register("transport.echo", _ => HandleTransportEchoAsync);
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
    /// Provisional minimal scope verification support. Full dispatch pipeline (Task 3.3.5),
    /// unknown type handling (Task 3.3.6), and exception handling (Task 3.3.7) will be completed later.
    /// </summary>
    public async Task<BridgeResponseEnvelope> RouteAsync(BridgeRequestEnvelope request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!TryGetHandlerFactory(request.Type, out var factory))
        {
            // Provisional: Task 3.3.6 will formally handle unknown types.
            throw new KeyNotFoundException($"No handler registered for message type '{request.Type}'.");
        }

        _logger.LogDebug("Creating DI scope for message type '{Type}' (RequestId: {RequestId}).", request.Type, request.RequestId);
        using var scope = _scopeFactory.CreateScope();

        // Task 3.3.3 & 3.3.4: Resolve service and execute in scope.
        // Task 3.3.5 will formalize the return pipeline.
        var handler = factory(scope.ServiceProvider);
        return await handler(request, cancellationToken);
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
}
