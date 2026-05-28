using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Bridge;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Payments;
using POS.Desktop.Shell;
using POS.Desktop.Tests.TestSupport;
using Xunit;

namespace POS.Desktop.Tests.Shell;

public class PaymentBridgeHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private const int TenantId = 42;

    public void Dispose() => _dbHarness.Dispose();

    private (PosWebMessageRouter Router, StubPaymentService Service, PosLocalDbContext Db) CreateRouterWithStub()
    {
        var db = _dbHarness.CreateProvisionedDbContext(TenantId);
        var stub = new StubPaymentService();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IPaymentService>(_ => stub);
        services.AddScoped(_ => db);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var router = new PosWebMessageRouter(scopeFactory, NullLogger<PosWebMessageRouter>.Instance);
        return (router, stub, db);
    }

    [Fact]
    public void PaymentEndpoints_AreRegistered()
    {
        var (router, _, _) = CreateRouterWithStub();
        Assert.True(router.CanHandle("payment.getTenderMethods"));
        Assert.True(router.CanHandle("payment.complete"));
    }

    // ── payment.getTenderMethods ──────────────────────────────────────────────

    [Fact]
    public async Task GetTenderMethods_ReturnsEmpty_WhenNoMethodsSeeded()
    {
        var (router, _, _) = CreateRouterWithStub();
        var request = new BridgeRequestEnvelope { Type = "payment.getTenderMethods", RequestId = "req-1", Version = "v1" };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("methods").GetArrayLength());
    }

    [Fact]
    public async Task GetTenderMethods_ReturnsSeededMethods_WithExpectedFields()
    {
        var (router, _, db) = CreateRouterWithStub();
        db.LocalTenderMethods.AddRange(
            new LocalTenderMethod { Id = 1, TenantId = TenantId, Code = "CASH", Name = "Cash", TenderType = "Cash", AllowsChange = true, RequiresExternalReference = false, SortOrder = 10 },
            new LocalTenderMethod { Id = 2, TenantId = TenantId, Code = "CARD", Name = "Card", TenderType = "Card", AllowsChange = false, RequiresExternalReference = true, SortOrder = 20 }
        );
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "payment.getTenderMethods", RequestId = "req-2", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var methods = doc.RootElement.GetProperty("methods");
        Assert.Equal(2, methods.GetArrayLength());

        var first = methods[0];
        Assert.Equal(1, first.GetProperty("id").GetInt32());
        Assert.Equal("CASH", first.GetProperty("code").GetString());
        Assert.Equal("Cash", first.GetProperty("name").GetString());
        Assert.Equal("Cash", first.GetProperty("tenderType").GetString());
        Assert.True(first.GetProperty("allowsChange").GetBoolean());
        Assert.False(first.GetProperty("requiresExternalReference").GetBoolean());
        Assert.Equal(10, first.GetProperty("sortOrder").GetInt32());
    }

    [Fact]
    public async Task GetTenderMethods_ReturnsSortedBySortOrderThenName()
    {
        var (router, _, db) = CreateRouterWithStub();
        db.LocalTenderMethods.AddRange(
            new LocalTenderMethod { Id = 3, TenantId = TenantId, Code = "WALLET", Name = "Wallet", TenderType = "Wallet", SortOrder = 30 },
            new LocalTenderMethod { Id = 1, TenantId = TenantId, Code = "CASH",   Name = "Cash",   TenderType = "Cash",   SortOrder = 10 },
            new LocalTenderMethod { Id = 2, TenantId = TenantId, Code = "CARD",   Name = "Card",   TenderType = "Card",   SortOrder = 20 }
        );
        await db.SaveChangesAsync();

        var request = new BridgeRequestEnvelope { Type = "payment.getTenderMethods", RequestId = "req-3", Version = "v1" };
        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        var methods = doc.RootElement.GetProperty("methods");
        Assert.Equal("CASH",   methods[0].GetProperty("code").GetString());
        Assert.Equal("CARD",   methods[1].GetProperty("code").GetString());
        Assert.Equal("WALLET", methods[2].GetProperty("code").GetString());
    }

    // ── payment.complete — validation ─────────────────────────────────────────

    [Fact]
    public async Task Complete_MissingPayload_ReturnsMalformedRequest()
    {
        var (router, _, _) = CreateRouterWithStub();
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-4", Version = "v1" };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.NotNull(response.Error);
        Assert.Equal("MALFORMED_REQUEST", response.Error.Code);
    }

    [Fact]
    public async Task Complete_MissingTendersProperty_ReturnsMalformedRequest()
    {
        var (router, _, _) = CreateRouterWithStub();
        var payloadElement = JsonDocument.Parse("""{"idempotencyKey":"key-1"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-5", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
    }

    [Fact]
    public async Task Complete_EmptyTendersArray_ReturnsMalformedRequest()
    {
        var (router, _, _) = CreateRouterWithStub();
        var payloadElement = JsonDocument.Parse("""{"tenders":[]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-6", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
    }

    [Fact]
    public async Task Complete_TenderMissingAmount_ReturnsMalformedRequest()
    {
        var (router, _, _) = CreateRouterWithStub();
        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1}]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-7", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
    }

    [Fact]
    public async Task Complete_TenderMissingTenderMethodId_ReturnsMalformedRequest()
    {
        var (router, _, _) = CreateRouterWithStub();
        var payloadElement = JsonDocument.Parse("""{"tenders":[{"amount":100}]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-13", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("MALFORMED_REQUEST", response.Error!.Code);
    }

    // ── payment.complete — success path ───────────────────────────────────────

    [Fact]
    public async Task Complete_ValidPayload_CallsServiceAndReturnsSuccess()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(
            Success: true,
            OrderId: Guid.NewGuid(),
            ReceiptNumber: "RCP-001",
            ChangeAmount: 50m,
            ReceiptText: "Thank you!"
        );

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":1050.00}],"idempotencyKey":"idem-1"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-8", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.True(response.Ok);
        Assert.True(service.CompleteOrderCalled);

        var json = JsonSerializer.Serialize(response.Payload, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("RCP-001", doc.RootElement.GetProperty("receiptNumber").GetString());
        Assert.Equal(50m, doc.RootElement.GetProperty("changeAmount").GetDecimal());
        Assert.Equal("Thank you!", doc.RootElement.GetProperty("receiptText").GetString());
    }

    [Fact]
    public async Task Complete_MapsIdempotencyKeyToService()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":500}],"idempotencyKey":"stable-key-xyz"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-9", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Equal("stable-key-xyz", service.LastRequest!.IdempotencyKey);
    }

    [Fact]
    public async Task Complete_MapsExternalPaymentReference()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":2,"amount":500,"externalPaymentReference":"TXN-ABC"}]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-10", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Single(service.LastRequest!.Tenders);
        Assert.Equal("TXN-ABC", service.LastRequest.Tenders[0].ExternalPaymentReference);
    }

    [Fact]
    public async Task Complete_ServiceReturnsFailure_PropagatesErrorCode()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(
            Success: false,
            ErrorCode: "EMPTY_CART",
            ErrorMessage: "Cart is empty."
        );

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":100}]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-11", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("EMPTY_CART", response.Error!.Code);
        Assert.Equal("Cart is empty.", response.Error.Message);
    }

    [Fact]
    public async Task Complete_WalletTender_MapsGuestPhone()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":3,"amount":500,"externalPaymentReference":"TXN-WALLET-ABC123"}],"guestPhone":"+923001234567","idempotencyKey":"idem-w1"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-w1", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Equal("+923001234567", service.LastRequest!.GuestPhone);
        Assert.Equal("TXN-WALLET-ABC123", service.LastRequest.Tenders[0].ExternalPaymentReference);
    }

    [Fact]
    public async Task Complete_CardTender_MapsExternalPaymentReference_Stable()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":2,"amount":1000,"externalPaymentReference":"TXN-CARD-ABCD1234EFGH"}],"idempotencyKey":"idem-c1"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-c1", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Equal("TXN-CARD-ABCD1234EFGH", service.LastRequest!.Tenders[0].ExternalPaymentReference);
    }

    [Fact]
    public async Task Complete_ServiceThrows_ReturnsHandlerError_WithoutLeakingDetails()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ExceptionToThrow = new InvalidOperationException("Internal DB failure.");

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":100}]}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-12", Version = "v1", Payload = payloadElement };

        var response = await router.RouteAsync(request, CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Equal("HANDLER_ERROR", response.Error!.Code);
        Assert.DoesNotContain("Internal DB failure", response.Error.Message);
    }

    [Fact]
    public async Task Complete_GuestName_IsMappedToService()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":500}],"guestName":"John Smith"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-14", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Equal("John Smith", service.LastRequest!.GuestName);
    }

    [Fact]
    public async Task Complete_MultipleTenders_AllMappedToService()
    {
        var (router, service, _) = CreateRouterWithStub();
        service.ResultToReturn = new PaymentCompletionResult(Success: true);

        var payloadElement = JsonDocument.Parse("""{"tenders":[{"tenderMethodId":1,"amount":300},{"tenderMethodId":2,"amount":700,"externalPaymentReference":"TXN-CARD-ABCDEF123456"}],"idempotencyKey":"idem-split-1"}""").RootElement;
        var request = new BridgeRequestEnvelope { Type = "payment.complete", RequestId = "req-15", Version = "v1", Payload = payloadElement };

        await router.RouteAsync(request, CancellationToken.None);

        Assert.Equal(2, service.LastRequest!.Tenders.Count);
        Assert.Equal(1, service.LastRequest.Tenders[0].TenderMethodId);
        Assert.Equal(300m, service.LastRequest.Tenders[0].Amount);
        Assert.Null(service.LastRequest.Tenders[0].ExternalPaymentReference);
        Assert.Equal(2, service.LastRequest.Tenders[1].TenderMethodId);
        Assert.Equal(700m, service.LastRequest.Tenders[1].Amount);
        Assert.Equal("TXN-CARD-ABCDEF123456", service.LastRequest.Tenders[1].ExternalPaymentReference);
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private class StubPaymentService : IPaymentService
    {
        public bool CompleteOrderCalled { get; private set; }
        public PaymentCompletionRequest? LastRequest { get; private set; }
        public PaymentCompletionResult ResultToReturn { get; set; } = new(Success: true);
        public Exception? ExceptionToThrow { get; set; }

        public Task<PaymentCompletionResult> CompleteOrderAsync(
            PaymentCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            CompleteOrderCalled = true;
            LastRequest = request;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(ResultToReturn);
        }
    }
}
