using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Auth;
using POS.Desktop.Shell;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public class PosWebMessageRouterTests
{
    private PosWebMessageRouter CreateRouter(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISessionService, OperatorSessionService>();
        services.AddSingleton<IAuthService, StubAuthService>();
        configureServices?.Invoke(services);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
    }

    [Fact]
    public void Router_ExposesBuiltInHandlers()
    {
        // Arrange
        var router = CreateRouter();

        // Act & Assert
        Assert.True(router.CanHandle("transport.echo"));
        Assert.True(router.CanHandle("session.get"));
        Assert.True(router.CanHandle("session.clear"));
        Assert.True(router.CanHandle("auth.validatePin"));
        Assert.True(router.CanHandle("provisioning.provisionTerminal"));
        Assert.True(router.CanHandle("provisioning.getProvisioningStatus"));
        Assert.True(router.CanHandle("catalog.listCategories"));
        Assert.True(router.CanHandle("catalog.listItems"));
        Assert.True(router.CanHandle("catalog.searchItems"));
        Assert.True(router.CanHandle("catalog.lookupByIdentifier"));
        Assert.True(router.CanHandle("shift.open"));
        Assert.True(router.CanHandle("shift.getCurrent"));
        Assert.True(router.CanHandle("shift.getOpenPolicy"));
        Assert.True(router.CanHandle("order.getCart"));
        Assert.True(router.CanHandle("order.addItem"));
        Assert.True(router.CanHandle("order.updateLineQuantity"));
        Assert.True(router.CanHandle("order.removeItem"));
        Assert.True(router.CanHandle("order.clearCart"));
        Assert.True(router.CanHandle("order.applyDiscount"));
        Assert.True(router.CanHandle("order.removeDiscount"));
        Assert.True(router.CanHandle("payment.getTenderMethods"));
        Assert.True(router.CanHandle("payment.complete"));
    }

    [Fact]
    public void Router_IdentifiesKnownType()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        var found = router.TryGetHandlerFactory("transport.echo", out var factory);

        // Assert
        Assert.True(found);
        Assert.NotNull(factory);
    }

    [Fact]
    public void Router_RejectsUnknownType()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        var canHandle = router.CanHandle("unknown.type");
        var found = router.TryGetHandlerFactory("unknown.type", out var factory);

        // Assert
        Assert.False(canHandle);
        Assert.False(found);
        Assert.Null(factory);
    }

    [Fact]
    public void Router_RejectsNullOrEmptyType()
    {
        // Arrange
        var router = CreateRouter();

        // Act & Assert
        Assert.False(router.CanHandle(null!));
        Assert.False(router.CanHandle(string.Empty));
        Assert.False(router.CanHandle("   "));

        Assert.False(router.TryGetHandlerFactory(null!, out _));
        Assert.False(router.TryGetHandlerFactory(string.Empty, out _));
        Assert.False(router.TryGetHandlerFactory("   ", out _));
    }

    [Fact]
    public void Register_ThrowsArgumentException_ForNullOrEmptyType()
    {
        // Arrange
        var router = CreateRouter();
        Func<IServiceProvider, BridgeMessageHandler> factory = _ => (req, token) => Task.FromResult(BridgeResponseEnvelope.Success(req.Type, req.RequestId));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => router.Register(null!, factory));
        Assert.Throws<ArgumentException>(() => router.Register(string.Empty, factory));
        Assert.Throws<ArgumentException>(() => router.Register("   ", factory));
    }

    [Fact]
    public void Register_GetRegisteredTypes_ReturnsRegisteredTypes()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        var types = router.GetRegisteredTypes();

        // Assert
        Assert.Contains("transport.echo", types);
        Assert.Contains("session.get", types);
        Assert.Contains("session.clear", types);
        Assert.Contains("auth.validatePin", types);
        Assert.Contains("provisioning.provisionTerminal", types);
        Assert.Contains("provisioning.getProvisioningStatus", types);
        Assert.Contains("catalog.listCategories", types);
        Assert.Contains("catalog.listItems", types);
        Assert.Contains("catalog.searchItems", types);
        Assert.Contains("catalog.lookupByIdentifier", types);
        Assert.Contains("shift.open", types);
        Assert.Contains("shift.getCurrent", types);
        Assert.Contains("shift.getOpenPolicy", types);
        Assert.Contains("order.getCart", types);
        Assert.Contains("order.addItem", types);
        Assert.Contains("order.updateLineQuantity", types);
        Assert.Contains("order.removeItem", types);
        Assert.Contains("order.clearCart", types);
        Assert.Contains("order.applyDiscount", types);
        Assert.Contains("order.removeDiscount", types);
        Assert.Contains("payment.getTenderMethods", types);
        Assert.Contains("payment.complete", types);
        Assert.Equal(22, types.Count);
    }

    [Fact]
    public void Register_ThrowsArgumentNullException_ForNullHandlerFactory()
    {
        // Arrange
        var router = CreateRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.Register("test.action", null!));
    }

    [Fact]
    public void Register_ThrowsInvalidOperationException_ForDuplicateRegistration()
    {
        // Arrange
        var router = CreateRouter();
        router.Register("duplicate.action", _ => (req, token) => Task.FromResult(BridgeResponseEnvelope.Success(req.Type, req.RequestId)));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            router.Register("duplicate.action", _ => (req, token) => Task.FromResult(BridgeResponseEnvelope.Success(req.Type, req.RequestId))));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void Register_SupportsOneLineDiErgonomics()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        // Task 3.3.9: Proving one-line registration ergonomics.
        router.Register("ergonomic.action", _ => (req, token) => Task.FromResult(BridgeResponseEnvelope.Success(req.Type, req.RequestId)));

        // Assert
        Assert.True(router.CanHandle("ergonomic.action"));
    }

    [Fact]
    public async Task BuiltInTransportEcho_ReturnsValidResponse()
    {
        // Arrange
        var router = CreateRouter();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "transport.echo",
            RequestId = "req-1",
            Payload = null
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Equal("transport.echo", response.Type);
        Assert.Equal("req-1", response.RequestId);
        Assert.NotNull(response.Payload);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task SessionGet_ReturnsInactive_WhenNoSession()
    {
        // Arrange
        var router = CreateRouter();
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "session.get",
            RequestId = "req-get-1"
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.Equal("session.get", response.Type);
        Assert.Equal("req-get-1", response.RequestId);

        var json = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, doc.RootElement.GetProperty("currentSession").ValueKind);
    }

    [Fact]
    public async Task SessionGet_ReturnsActive_WhenSessionExists()
    {
        // Arrange
        var session = new OperatorSession("op-1", "Test Operator", "Cashier", DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddLogging();
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(session);
        services.AddSingleton<ISessionService>(sessionService);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var routerWithSession = new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "session.get",
            RequestId = "req-get-2"
        };

        // Act
        var response = await routerWithSession.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);

        var json = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal("op-1", doc.RootElement.GetProperty("currentSession").GetProperty("operatorId").GetString());
    }

    [Fact]
    public async Task SessionClear_ClearsSessionAndReturnsSuccess()
    {
        // Arrange
        var session = new OperatorSession("op-1", "Test Operator", "Cashier", DateTimeOffset.UtcNow);
        var sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
        sessionService.StartSession(session);

        var router = CreateRouter(services =>
        {
            services.AddSingleton<ISessionService>(sessionService);
        });

        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "session.clear",
            RequestId = "req-clear-1"
        };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);

        var json = System.Text.Json.JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("cleared").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());
        Assert.False(sessionService.IsActive);
    }

    [Fact]
    public async Task RouteAsync_ResolvesHandlerFromScopeAndDisposesIt()
    {
        // Arrange
        var disposableService = new FakeDisposableService();
        var router = CreateRouter(services =>
        {
            services.AddScoped(_ => disposableService);
        });

        router.Register("test.action", sp =>
        {
            var service = sp.GetRequiredService<FakeDisposableService>();
            return (req, token) =>
            {
                service.IsInvoked = true;
                return Task.FromResult(BridgeResponseEnvelope.Success(req.Type, req.RequestId));
            };
        });

        var request = new BridgeRequestEnvelope { Type = "test.action", RequestId = "req-2", Version = "v1" };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.True(disposableService.IsInvoked);
        // The service should be disposed after RouteAsync completes because the scope is disposed
        Assert.True(disposableService.IsDisposed);
    }

    [Fact]
    public async Task RouteAsync_ReturnsUnsupportedType_ForUnknownAction()
    {
        // Arrange
        var router = CreateRouter();
        var request = new BridgeRequestEnvelope { Type = "unknown.action", RequestId = "req-3", Version = "v1" };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.Equal("unknown.action", response.Type);
        Assert.Equal("req-3", response.RequestId);
        Assert.Null(response.Payload);
        Assert.NotNull(response.Error);
        Assert.Equal("UNSUPPORTED_TYPE", response.Error.Code);
        Assert.Equal("The requested action is not implemented.", response.Error.Message);
    }

    [Fact]
    public async Task RouteAsync_ReturnsHandlerError_WhenHandlerThrows()
    {
        // Arrange
        var router = CreateRouter();
        router.Register("fail.action", _ => (req, token) => throw new InvalidOperationException("Handler failed."));
        var request = new BridgeRequestEnvelope { Type = "fail.action", RequestId = "req-4", Version = "v1" };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.Equal("fail.action", response.Type);
        Assert.Equal("req-4", response.RequestId);
        Assert.Null(response.Payload);
        Assert.NotNull(response.Error);
        Assert.Equal("HANDLER_ERROR", response.Error.Code);
        Assert.Equal("The requested action could not be completed.", response.Error.Message);
        Assert.Null(response.Error.Details); // Details should not contain stack trace or raw exception.
    }

    private class FakeDisposableService : IDisposable
    {
        public bool IsInvoked { get; set; }
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    [Fact]
    public async Task RouteAsync_UnsupportedMessage_DoesNotCreateScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var realScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var spyScopeFactory = new SpyScopeFactory(realScopeFactory);
        var router = new PosWebMessageRouter(spyScopeFactory, NullLogger<PosWebMessageRouter>.Instance);

        var request = new BridgeRequestEnvelope { Type = "unknown.action", RequestId = "req-1", Version = "v1" };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Ok);
        Assert.Equal(0, spyScopeFactory.CreateScopeCount);
    }

    [Fact]
    public async Task RouteAsync_SupportedMessage_CreatesAndDisposesScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var realScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var spyScopeFactory = new SpyScopeFactory(realScopeFactory);
        var router = new PosWebMessageRouter(spyScopeFactory, NullLogger<PosWebMessageRouter>.Instance);

        var request = new BridgeRequestEnvelope { Type = "transport.echo", RequestId = "req-1", Version = "v1" };

        // Act
        var response = await router.RouteAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.Ok);
        Assert.Equal(1, spyScopeFactory.CreateScopeCount);
        Assert.Equal(1, spyScopeFactory.DisposeCount);
    }

    [Fact]
    public async Task RouteAsync_ConsecutiveMessages_CreateSeparateScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var realScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var spyScopeFactory = new SpyScopeFactory(realScopeFactory);
        var router = new PosWebMessageRouter(spyScopeFactory, NullLogger<PosWebMessageRouter>.Instance);

        var request1 = new BridgeRequestEnvelope { Type = "transport.echo", RequestId = "req-1", Version = "v1" };
        var request2 = new BridgeRequestEnvelope { Type = "transport.echo", RequestId = "req-2", Version = "v1" };

        // Act
        await router.RouteAsync(request1, CancellationToken.None);
        await router.RouteAsync(request2, CancellationToken.None);

        // Assert
        Assert.Equal(2, spyScopeFactory.CreateScopeCount);
        Assert.Equal(2, spyScopeFactory.DisposeCount);
    }

    private sealed class SpyScopeFactory : IServiceScopeFactory, IServiceScope
    {
        private readonly IServiceScopeFactory _inner;
        private IServiceScope? _currentScope;

        public int CreateScopeCount { get; private set; }
        public int DisposeCount { get; private set; }

        public IServiceProvider ServiceProvider => _currentScope?.ServiceProvider
            ?? throw new InvalidOperationException("No active scope.");

        public SpyScopeFactory(IServiceScopeFactory inner)
        {
            _inner = inner;
        }

        public IServiceScope CreateScope()
        {
            CreateScopeCount++;
            _currentScope = _inner.CreateScope();
            return this;
        }

        public void Dispose()
        {
            DisposeCount++;
            _currentScope?.Dispose();
            _currentScope = null;
        }
    }
}
