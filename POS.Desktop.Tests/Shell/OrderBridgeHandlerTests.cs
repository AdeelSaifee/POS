using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Orders;
using POS.Desktop.Shell;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public class OrderBridgeHandlerTests
{
    private (PosWebMessageRouter Router, StubOrderService Service) CreateRouterWithStub()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var stub = new StubOrderService();
        services.AddScoped<IOrderService>(_ => stub);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var router = new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
        return (router, stub);
    }

    [Fact]
    public void OrderEndpoints_AreAllRegistered()
    {
        var (router, _) = CreateRouterWithStub();
        Assert.True(router.CanHandle("order.getCart"));
        Assert.True(router.CanHandle("order.addItem"));
        Assert.True(router.CanHandle("order.updateLineQuantity"));
        Assert.True(router.CanHandle("order.removeItem"));
        Assert.True(router.CanHandle("order.clearCart"));
        Assert.True(router.CanHandle("order.applyDiscount"));
        Assert.True(router.CanHandle("order.removeDiscount"));
    }

    [Fact]
    public async Task GetCart_RoutesToService_AndReturnsState()
    {
        var (router, service) = CreateRouterWithStub();
        service.CartStateToReturn = new CartStateDto { SubtotalAmount = 100m, TotalAmount = 105m };

        var request = new BridgeRequestEnvelope { Type = "order.getCart", RequestId = "req-1", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.GetCartCalled);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(100m, doc.RootElement.GetProperty("subtotalAmount").GetDecimal());
        Assert.Equal(105m, doc.RootElement.GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public async Task AddItem_ValidatesVariantId_AndOptionalQuantity()
    {
        var (router, service) = CreateRouterWithStub();
        var payloadObj = new { variantId = 42, quantity = 5 };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.addItem",
            RequestId = "req-2",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.AddItemCalled);
        Assert.Equal(42, service.LastVariantId);
        Assert.Equal(5, service.LastQuantity);
    }

    [Fact]
    public async Task AddItem_WorksWithDefaultQuantity()
    {
        var (router, service) = CreateRouterWithStub();
        var payloadObj = new { variantId = 42 };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.addItem",
            RequestId = "req-3",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.AddItemCalled);
        Assert.Equal(42, service.LastVariantId);
        Assert.Equal(1, service.LastQuantity);
    }

    [Fact]
    public async Task UpdateLineQuantity_ValidatesVariantId_AndQuantity()
    {
        var (router, service) = CreateRouterWithStub();
        var payloadObj = new { variantId = 10, quantity = 3 };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.updateLineQuantity",
            RequestId = "req-4",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.UpdateQtyCalled);
        Assert.Equal(10, service.LastVariantId);
        Assert.Equal(3, service.LastQuantity);
    }

    [Fact]
    public async Task RemoveItem_ValidatesVariantId()
    {
        var (router, service) = CreateRouterWithStub();
        var payloadObj = new { variantId = 7 };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.removeItem",
            RequestId = "req-5",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.RemoveItemCalled);
        Assert.Equal(7, service.LastVariantId);
    }

    [Fact]
    public async Task ClearCart_RoutesToService()
    {
        var (router, service) = CreateRouterWithStub();

        var request = new BridgeRequestEnvelope { Type = "order.clearCart", RequestId = "req-6", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.ClearCartCalled);
    }

    [Fact]
    public async Task ApplyDiscount_ValidatesDiscountType_AndDiscountValue()
    {
        var (router, service) = CreateRouterWithStub();
        var payloadObj = new { discountType = "pct", discountValue = 10m };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.applyDiscount",
            RequestId = "req-7",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.ApplyDiscountCalled);
        Assert.Equal("pct", service.LastDiscountType);
        Assert.Equal(10m, service.LastDiscountValue);
    }

    [Fact]
    public async Task RemoveDiscount_RoutesToService()
    {
        var (router, service) = CreateRouterWithStub();

        var request = new BridgeRequestEnvelope { Type = "order.removeDiscount", RequestId = "req-8", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.RemoveDiscountCalled);
    }

    [Theory]
    [InlineData("order.addItem", "{\"variantId\":\"42\"}")] // variantId is string instead of number
    [InlineData("order.addItem", "{\"variantId\":42.5}")] // variantId is fractional
    [InlineData("order.addItem", "{\"quantity\":5}")] // missing variantId
    [InlineData("order.updateLineQuantity", "{\"variantId\":10}")] // missing quantity
    [InlineData("order.updateLineQuantity", "{\"variantId\":10,\"quantity\":\"3\"}")] // quantity is string
    [InlineData("order.removeItem", "{}")] // missing variantId
    [InlineData("order.applyDiscount", "{\"discountType\":10,\"discountValue\":10}")] // type is number
    [InlineData("order.applyDiscount", "{\"discountType\":\"pct\",\"discountValue\":\"10\"}")] // value is string
    public async Task StrictPayloadParsing_ReturnsMalformedRequest_ForInvalidTypes(string type, string payloadJson)
    {
        var (router, _) = CreateRouterWithStub();
        var payloadElement = JsonDocument.Parse(payloadJson).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = type,
            RequestId = "req-err",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task OrderValidationException_MapsSafeErrorCodeAndMessage()
    {
        var (router, service) = CreateRouterWithStub();
        service.ExceptionToThrow = new OrderValidationException("Invalid quantity specified.", "INVALID_QUANTITY");

        var payloadObj = new { variantId = 42, quantity = -5 };
        var payloadElement = JsonDocument.Parse(JsonSerializer.Serialize(payloadObj)).RootElement;

        var request = new BridgeRequestEnvelope
        {
            Type = "order.addItem",
            RequestId = "req-val",
            Version = "v1",
            Payload = payloadElement
        };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_QUANTITY", response.Error.Code);
        Assert.Equal("Invalid quantity specified.", response.Error.Message);
    }

    [Fact]
    public async Task GetCart_OrderValidationException_MapsSafeErrorCodeAndMessage()
    {
        var (router, service) = CreateRouterWithStub();
        service.ExceptionToThrow = new OrderValidationException("Invalid cart state.", "INVALID_CART");

        var request = new BridgeRequestEnvelope { Type = "order.getCart", RequestId = "req-val-get", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_CART", response.Error.Code);
        Assert.Equal("Invalid cart state.", response.Error.Message);
    }

    [Fact]
    public async Task ClearCart_OrderValidationException_MapsSafeErrorCodeAndMessage()
    {
        var (router, service) = CreateRouterWithStub();
        service.ExceptionToThrow = new OrderValidationException("Cannot clear cart.", "CANNOT_CLEAR");

        var request = new BridgeRequestEnvelope { Type = "order.clearCart", RequestId = "req-val-clear", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("CANNOT_CLEAR", response.Error.Code);
        Assert.Equal("Cannot clear cart.", response.Error.Message);
    }

    [Fact]
    public async Task RemoveDiscount_OrderValidationException_MapsSafeErrorCodeAndMessage()
    {
        var (router, service) = CreateRouterWithStub();
        service.ExceptionToThrow = new OrderValidationException("Cannot remove discount.", "CANNOT_REMOVE_DISCOUNT");

        var request = new BridgeRequestEnvelope { Type = "order.removeDiscount", RequestId = "req-val-remove-discount", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("CANNOT_REMOVE_DISCOUNT", response.Error.Code);
        Assert.Equal("Cannot remove discount.", response.Error.Message);
    }


    private class StubOrderService : IOrderService
    {
        public bool GetCartCalled { get; set; }
        public bool AddItemCalled { get; set; }
        public int LastVariantId { get; set; }
        public int LastQuantity { get; set; }
        public bool UpdateQtyCalled { get; set; }
        public bool RemoveItemCalled { get; set; }
        public bool ClearCartCalled { get; set; }
        public bool ApplyDiscountCalled { get; set; }
        public string? LastDiscountType { get; set; }
        public decimal LastDiscountValue { get; set; }
        public bool RemoveDiscountCalled { get; set; }

        public OrderValidationException? ExceptionToThrow { get; set; }
        public CartStateDto CartStateToReturn { get; set; } = new();

        public Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default)
        {
            GetCartCalled = true;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> AddItemAsync(int variantId, int quantity = 1, CancellationToken cancellationToken = default)
        {
            AddItemCalled = true;
            LastVariantId = variantId;
            LastQuantity = quantity;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> UpdateLineQuantityAsync(int variantId, int quantity, CancellationToken cancellationToken = default)
        {
            UpdateQtyCalled = true;
            LastVariantId = variantId;
            LastQuantity = quantity;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> RemoveItemAsync(int variantId, CancellationToken cancellationToken = default)
        {
            RemoveItemCalled = true;
            LastVariantId = variantId;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> ClearCartAsync(CancellationToken cancellationToken = default)
        {
            ClearCartCalled = true;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> ApplyDiscountAsync(string discountType, decimal discountValue, CancellationToken cancellationToken = default)
        {
            ApplyDiscountCalled = true;
            LastDiscountType = discountType;
            LastDiscountValue = discountValue;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }

        public Task<CartStateDto> RemoveDiscountAsync(CancellationToken cancellationToken = default)
        {
            RemoveDiscountCalled = true;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(CartStateToReturn);
        }
    }
}
