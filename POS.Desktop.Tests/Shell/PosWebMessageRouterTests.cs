using System.Threading;
using System.Threading.Tasks;
using POS.Desktop.Bridge;
using POS.Desktop.Shell;

namespace POS.Desktop.Tests.Shell;

public class PosWebMessageRouterTests
{
    [Fact]
    public void Router_ExposesBuiltInTransportEcho()
    {
        // Arrange
        var router = new PosWebMessageRouter();

        // Act
        var canHandle = router.CanHandle("transport.echo");

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void Router_IdentifiesKnownType()
    {
        // Arrange
        var router = new PosWebMessageRouter();

        // Act
        var found = router.TryGetHandler("transport.echo", out var handler);

        // Assert
        Assert.True(found);
        Assert.NotNull(handler);
    }

    [Fact]
    public void Router_RejectsUnknownType()
    {
        // Arrange
        var router = new PosWebMessageRouter();

        // Act
        var canHandle = router.CanHandle("unknown.type");
        var found = router.TryGetHandler("unknown.type", out var handler);

        // Assert
        Assert.False(canHandle);
        Assert.False(found);
        Assert.Null(handler);
    }

    [Fact]
    public void Router_RejectsNullOrEmptyType()
    {
        // Arrange
        var router = new PosWebMessageRouter();

        // Act & Assert
        Assert.False(router.CanHandle(null!));
        Assert.False(router.CanHandle(string.Empty));
        Assert.False(router.CanHandle("   "));

        Assert.False(router.TryGetHandler(null!, out _));
        Assert.False(router.TryGetHandler(string.Empty, out _));
        Assert.False(router.TryGetHandler("   ", out _));
    }

    [Fact]
    public void Router_GetRegisteredTypes_ReturnsRegisteredTypes()
    {
        // Arrange
        var router = new PosWebMessageRouter();

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
        var router = new PosWebMessageRouter();
        Assert.True(router.TryGetHandler("transport.echo", out var handler));
        
        var request = new BridgeRequestEnvelope
        {
            Version = "v1",
            Type = "transport.echo",
            RequestId = "req-1",
            Payload = null
        };

        // Act
        var response = await handler(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Equal("transport.echo", response.Type);
        Assert.Equal("req-1", response.RequestId);
        Assert.NotNull(response.Payload);
    }
}
