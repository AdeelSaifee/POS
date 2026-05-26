using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Dictionary<string, BridgeMessageHandler> _handlers = new(StringComparer.Ordinal);

    public PosWebMessageRouter()
    {
        // Built-in handlers for proving the router foundation without business logic.
        Register("transport.echo", HandleTransportEchoAsync);
    }

    /// <summary>
    /// Registers a handler for a specific message type.
    /// </summary>
    private void Register(string type, BridgeMessageHandler handler)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Message type cannot be null or empty.", nameof(type));
        }

        if (_handlers.ContainsKey(type))
        {
            throw new InvalidOperationException($"A handler for message type '{type}' is already registered.");
        }

        _handlers[type] = handler ?? throw new ArgumentNullException(nameof(handler));
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
    /// Attempts to retrieve the registered handler for the given type.
    /// </summary>
    public bool TryGetHandler(string type, out BridgeMessageHandler handler)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            handler = null!;
            return false;
        }

        return _handlers.TryGetValue(type, out handler!);
    }

    /// <summary>
    /// Retrieves all registered message types.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredTypes()
    {
        return _handlers.Keys;
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
