using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Shell;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public class PosWebMessageRouterTests
{
    private PosWebMessageRouter CreateRouter(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configureServices?.Invoke(services);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
    }

    [Fact]
    public void Router_ExposesBuiltInTransportEcho()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        var canHandle = router.CanHandle("transport.echo");

        // Assert
        Assert.True(canHandle);
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
    public void Router_GetRegisteredTypes_ReturnsRegisteredTypes()
    {
        // Arrange
        var router = CreateRouter();

        // Act
        var types = router.GetRegisteredTypes();

        // Assert
        Assert.Contains("transport.echo", types);
        Assert.Single(types); // Assuming only transport.echo is registered by default
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
    public async Task RouteAsync_ThrowsKeyNotFoundException_ForUnknownType_Provisional()
    {
        // Arrange
        var router = CreateRouter();
        var request = new BridgeRequestEnvelope { Type = "unknown.action", RequestId = "req-3", Version = "v1" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => router.RouteAsync(request, CancellationToken.None));
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
}
