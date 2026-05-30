using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Desktop.Bridge;
using POS.Desktop.Services.Catalog;
using POS.Desktop.Services.Session;
using POS.Desktop.Services.Auth;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Shifts;
using POS.Desktop.Services.Orders;
using POS.Desktop.Services.Payments;
using POS.Desktop.Services.CashControl;
using POS.Desktop.Services.Sync;
using POS.Desktop.Data;
using POS.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

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
            sp.GetRequiredService<IProvisionedTerminalContext>(),
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

        // Task 4.4.4: Catalog read handlers
        Register("catalog.listCategories", sp => (req, ct) => HandleCatalogListCategoriesAsync(
            sp.GetRequiredService<ICatalogService>(),
            req,
            ct));
        Register("catalog.listItems", sp => (req, ct) => HandleCatalogListItemsAsync(
            sp.GetRequiredService<ICatalogService>(),
            req,
            ct));
        Register("catalog.searchItems", sp => (req, ct) => HandleCatalogSearchItemsAsync(
            sp.GetRequiredService<ICatalogService>(),
            req,
            ct));
        Register("catalog.lookupByIdentifier", sp => (req, ct) => HandleCatalogLookupByIdentifierAsync(
            sp.GetRequiredService<ICatalogService>(),
            req,
            ct));

        // Task 5.2.3: Shift open bridge handler.
        Register("shift.open", sp => (req, ct) => HandleShiftOpenAsync(
            sp.GetRequiredService<IShiftService>(),
            req,
            ct));

        // Task 5.2.7: Get current shift status bridge handler.
        Register("shift.getCurrent", sp => (req, ct) => HandleGetCurrentShiftAsync(
            sp.GetRequiredService<IShiftService>(),
            req,
            ct));

        // Task 5.2.8: Get shift-open policy (limits + checklist) from config.
        Register("shift.getOpenPolicy", sp => (req, ct) => HandleGetShiftOpenPolicyAsync(
            sp.GetRequiredService<IShiftService>(),
            req,
            ct));

        // Task 5.3.8: Cart/Order endpoints
        Register("order.getCart", sp => (req, ct) => HandleGetCartAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.addItem", sp => (req, ct) => HandleAddItemAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.updateLineQuantity", sp => (req, ct) => HandleUpdateLineQuantityAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.removeItem", sp => (req, ct) => HandleRemoveItemAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.clearCart", sp => (req, ct) => HandleClearCartAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.applyDiscount", sp => (req, ct) => HandleApplyDiscountAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));
        Register("order.removeDiscount", sp => (req, ct) => HandleRemoveDiscountAsync(
            sp.GetRequiredService<IOrderService>(),
            req,
            ct));

        // Task 5.4.9: Payment bridge handlers
        Register("payment.getTenderMethods", sp => (req, ct) => HandlePaymentGetTenderMethodsAsync(
            sp.GetRequiredService<PosLocalDbContext>(), req, ct));
        Register("payment.complete", sp => (req, ct) => HandlePaymentCompleteAsync(
            sp.GetRequiredService<IPaymentService>(), req, ct));

        // Task 5.5.7: Cash control handlers
        Register("cash.getSummary", sp => (req, ct) => HandleCashGetSummaryAsync(
            sp.GetRequiredService<ICashControlService>(), req, ct));
        Register("cash.recordMovement", sp => (req, ct) => HandleCashRecordMovementAsync(
            sp.GetRequiredService<ICashControlService>(), req, ct));
        Register("cash.getLedger", sp => (req, ct) => HandleCashGetLedgerAsync(
            sp.GetRequiredService<PosLocalDbContext>(),
            sp.GetRequiredService<IProvisionedTerminalContext>(),
            req,
            ct));
        Register("cash.getReasonCodes", sp => (req, ct) => HandleCashGetReasonCodesAsync(
            sp.GetRequiredService<PosLocalDbContext>(), req, ct));

        Register("sync.getStatus", sp => (req, ct) => HandleGetSyncStatusAsync(
            sp.GetRequiredService<ISyncStatusService>(), req, ct));
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
    /// Handles the auth.validatePin message, validating credentials against the database
    /// and initializing a session upon success.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleAuthValidatePinAsync(
        IAuthService authService,
        ISessionService sessionService,
        IProvisionedTerminalContext provisioningContext,
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
            if (string.IsNullOrWhiteSpace(result.Operator.SessionId))
            {
                _logger.LogError("Authentication succeeded but no LocalTerminalSession ID was resolved.");
                return BridgeResponseEnvelope.Failure(
                    type: request.Type,
                    requestId: request.RequestId,
                    code: "SESSION_NOT_CREATED",
                    message: "A terminal session could not be established."
                );
            }

            string resolvedTerminalId = provisioningContext.IsProvisioned
                ? provisioningContext.CurrentTerminalId.ToString()
                : "POS-01";

            var session = new OperatorSession(
                OperatorId: result.Operator.OperatorId,
                DisplayName: result.Operator.DisplayName,
                Role: result.Operator.Role,
                LoginTime: DateTimeOffset.UtcNow,
                TerminalId: resolvedTerminalId,
                SessionId: result.Operator.SessionId
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

    // ----------------------------------------------------------------
    // Task 4.4.4 — Catalog bridge handlers
    // ----------------------------------------------------------------

    /// <summary>
    /// Returns all catalog categories ordered by SortOrder.
    /// Payload: {} (no parameters required).
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleCatalogListCategoriesAsync(
        ICatalogService catalogService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        var categories = await catalogService.ListCategoriesAsync(cancellationToken);
        return BridgeResponseEnvelope.Success(
            request.Type,
            request.RequestId,
            new CatalogListCategoriesResponse { Categories = categories });
    }

    /// <summary>
    /// Returns catalog items, optionally filtered by categoryId and/or searchText.
    /// Payload: { categoryId?: number, searchText?: string, limit?: number }.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleCatalogListItemsAsync(
        ICatalogService catalogService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = ParseCatalogItemQuery(request);
            var items = await catalogService.ListItemsAsync(query, cancellationToken);
            return BridgeResponseEnvelope.Success(
                request.Type,
                request.RequestId,
                new CatalogListItemsResponse { Items = items });
        }
        catch (JsonException)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
    }

    /// <summary>
    /// Searches catalog items by name, code, SKU, or barcode.
    /// Payload: { searchText?: string, limit?: number }.
    /// Empty searchText returns all items up to limit.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleCatalogSearchItemsAsync(
        ICatalogService catalogService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            string searchText = string.Empty;
            int limit = 50;

            if (request.Payload.HasValue)
            {
                var payloadJson = JsonSerializer.Serialize(request.Payload.Value, BridgeJsonSerializerOptions.Default);
                using var doc = JsonDocument.Parse(payloadJson);

                if (doc.RootElement.TryGetProperty("searchText", out var searchProp))
                    searchText = searchProp.GetString() ?? string.Empty;

                if (doc.RootElement.TryGetProperty("limit", out var limitProp) &&
                    limitProp.TryGetInt32(out var parsedLimit))
                    limit = parsedLimit;
            }

            var items = await catalogService.SearchItemsAsync(searchText, limit, cancellationToken);
            return BridgeResponseEnvelope.Success(
                request.Type,
                request.RequestId,
                new CatalogListItemsResponse { Items = items });
        }
        catch (JsonException)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
    }

    /// <summary>
    /// Looks up a single item by barcode or other identifier value.
    /// Payload: { identifierValue: string }.
    /// Response: { found: bool, item: CatalogItemDto | null }.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleCatalogLookupByIdentifierAsync(
        ICatalogService catalogService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing.");
        }

        try
        {
            var payloadJson = JsonSerializer.Serialize(request.Payload.Value, BridgeJsonSerializerOptions.Default);
            using var doc = JsonDocument.Parse(payloadJson);

            if (!doc.RootElement.TryGetProperty("identifierValue", out var identProp))
            {
                return BridgeResponseEnvelope.Failure(
                    request.Type, request.RequestId, "MALFORMED_REQUEST",
                    "Required property 'identifierValue' was missing.");
            }

            var identifierValue = identProp.GetString() ?? string.Empty;
            var item = await catalogService.FindByIdentifierAsync(identifierValue, cancellationToken);

            return BridgeResponseEnvelope.Success(
                request.Type,
                request.RequestId,
                new CatalogLookupResponse { Found = item is not null, Item = item });
        }
        catch (JsonException)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
    }

    /// <summary>
    /// Safely parses a <see cref="CatalogItemQuery"/> from the request payload.
    /// Missing or null payload returns a default query (no filters, limit 50).
    /// </summary>
    private static CatalogItemQuery ParseCatalogItemQuery(BridgeRequestEnvelope request)
    {
        if (!request.Payload.HasValue)
            return new CatalogItemQuery();

        var payloadJson = JsonSerializer.Serialize(request.Payload.Value, BridgeJsonSerializerOptions.Default);
        using var doc = JsonDocument.Parse(payloadJson);

        int? categoryId = null;
        if (doc.RootElement.TryGetProperty("categoryId", out var catProp) &&
            catProp.ValueKind == JsonValueKind.Number &&
            catProp.TryGetInt32(out var cat))
            categoryId = cat;

        string? searchText = null;
        if (doc.RootElement.TryGetProperty("searchText", out var searchProp))
            searchText = searchProp.GetString();

        int limit = 50;
        if (doc.RootElement.TryGetProperty("limit", out var limitProp) &&
            limitProp.TryGetInt32(out var lim))
            limit = lim;

        return new CatalogItemQuery { CategoryId = categoryId, SearchText = searchText, Limit = limit };
    }

    /// <summary>
    /// Handles the shift.open message, validating float and invoking the shift service.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleShiftOpenAsync(
        IShiftService shiftService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing.");
        }

        decimal openingFloat = 0;
        try
        {
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(request.Payload, BridgeJsonSerializerOptions.Default);
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);

            if (!doc.RootElement.TryGetProperty("openingFloat", out var floatProp))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Required parameter 'openingFloat' was missing.");
            }

            if (floatProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                if (!floatProp.TryGetDecimal(out openingFloat))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'openingFloat' must be a valid number.");
                }
            }
            else if (floatProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var strVal = floatProp.GetString();
                if (!decimal.TryParse(strVal, out openingFloat))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'openingFloat' must be a valid number.");
                }
            }
            else
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'openingFloat' must be numeric.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse shift.open payload.");
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Failed to parse request payload.");
        }

        var result = await shiftService.OpenShiftAsync(openingFloat, cancellationToken);

        if (result.IsSuccess && result.Shift != null)
        {
            return BridgeResponseEnvelope.Success(
                type: request.Type,
                requestId: request.RequestId,
                payload: new
                {
                    shiftId = result.Shift.Id.ToString(),
                    businessDate = result.Shift.BusinessDate.ToString("yyyy-MM-dd"),
                    openingFloat = result.Shift.OpeningCashAmount,
                    status = result.Shift.Status.ToString()
                }
            );
        }

        string errorCode = result.ErrorCode ?? "SHIFT_OPEN_FAILED";
        string errorMessage = result.ErrorMessage ?? "The shift could not be opened.";

        return BridgeResponseEnvelope.Failure(
            type: request.Type,
            requestId: request.RequestId,
            code: errorCode,
            message: errorMessage
        );
    }

    /// <summary>
    /// Handles the shift.getCurrent message, retrieving active shift details.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleGetCurrentShiftAsync(
        IShiftService shiftService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await shiftService.GetCurrentShiftAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(
                type: request.Type,
                requestId: request.RequestId,
                payload: new
                {
                    isOpen = result.IsOpen,
                    shiftId = result.ShiftId?.ToString(),
                    businessDate = result.BusinessDate?.ToString("yyyy-MM-dd"),
                    openingFloat = result.OpeningFloat,
                    status = result.Status
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current shift.");
            return BridgeResponseEnvelope.Failure(
                type: request.Type,
                requestId: request.RequestId,
                code: "SHIFT_QUERY_FAILED",
                message: "Failed to query current shift status."
            );
        }
    }

    /// <summary>
    /// Handles the shift.getOpenPolicy message.
    /// Returns configured cash-drawer limits and pre-shift checklist from appsettings.
    /// Does not require an active session or open shift.
    /// Never exposes internal exception details to the client.
    /// </summary>
    private async Task<BridgeResponseEnvelope> HandleGetShiftOpenPolicyAsync(
        IShiftService shiftService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var policy = await shiftService.GetOpenPolicyAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(
                type: request.Type,
                requestId: request.RequestId,
                payload: new
                {
                    cashDrawerLimit = policy.CashDrawerLimit,
                    autoSafeDropThreshold = policy.AutoSafeDropThreshold,
                    checklist = policy.Checklist
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve shift open policy.");
            return BridgeResponseEnvelope.Failure(
                type: request.Type,
                requestId: request.RequestId,
                code: "POLICY_FETCH_FAILED",
                message: "Failed to retrieve shift open policy."
            );
        }
    }

    private async Task<BridgeResponseEnvelope> HandleGetCartAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cartState = await orderService.GetCartStateAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type,
                request.RequestId,
                ex.ErrorCode,
                ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleAddItemAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        var payload = request.Payload.Value;
        if (!payload.TryGetProperty("variantId", out var variantIdProp) ||
            variantIdProp.ValueKind != JsonValueKind.Number ||
            !variantIdProp.TryGetInt32(out var variantId))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'variantId' is required and must be an integer number.");
        }

        int quantity = 1;
        if (payload.TryGetProperty("quantity", out var quantityProp) && quantityProp.ValueKind != JsonValueKind.Null)
        {
            if (quantityProp.ValueKind != JsonValueKind.Number || !quantityProp.TryGetInt32(out quantity))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'quantity' must be an integer number.");
            }
        }

        try
        {
            var cartState = await orderService.AddItemAsync(variantId, quantity, cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, ex.ErrorCode, ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleUpdateLineQuantityAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        var payload = request.Payload.Value;
        if (!payload.TryGetProperty("variantId", out var variantIdProp) ||
            variantIdProp.ValueKind != JsonValueKind.Number ||
            !variantIdProp.TryGetInt32(out var variantId))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'variantId' is required and must be an integer number.");
        }

        if (!payload.TryGetProperty("quantity", out var quantityProp) ||
            quantityProp.ValueKind != JsonValueKind.Number ||
            !quantityProp.TryGetInt32(out var quantity))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'quantity' is required and must be an integer number.");
        }

        try
        {
            var cartState = await orderService.UpdateLineQuantityAsync(variantId, quantity, cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, ex.ErrorCode, ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleRemoveItemAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        var payload = request.Payload.Value;
        if (!payload.TryGetProperty("variantId", out var variantIdProp) ||
            variantIdProp.ValueKind != JsonValueKind.Number ||
            !variantIdProp.TryGetInt32(out var variantId))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'variantId' is required and must be an integer number.");
        }

        try
        {
            var cartState = await orderService.RemoveItemAsync(variantId, cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, ex.ErrorCode, ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleClearCartAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cartState = await orderService.ClearCartAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type,
                request.RequestId,
                ex.ErrorCode,
                ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleApplyDiscountAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        var payload = request.Payload.Value;
        if (!payload.TryGetProperty("discountType", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'discountType' is required and must be a string.");
        }
        var discountType = typeProp.GetString() ?? string.Empty;

        if (!payload.TryGetProperty("discountValue", out var valProp) ||
            valProp.ValueKind != JsonValueKind.Number ||
            !valProp.TryGetDecimal(out var discountValue))
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'discountValue' is required and must be a number.");
        }

        try
        {
            var cartState = await orderService.ApplyDiscountAsync(discountType, discountValue, cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, ex.ErrorCode, ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandleRemoveDiscountAsync(
        IOrderService orderService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cartState = await orderService.RemoveDiscountAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, cartState);
        }
        catch (OrderValidationException ex)
        {
            return BridgeResponseEnvelope.Failure(
                request.Type,
                request.RequestId,
                ex.ErrorCode,
                ex.SafeMessage);
        }
    }

    private async Task<BridgeResponseEnvelope> HandlePaymentGetTenderMethodsAsync(
        PosLocalDbContext db,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var methods = await db.LocalTenderMethods
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .Select(t => new
                {
                    id = t.Id,
                    code = t.Code,
                    name = t.Name,
                    tenderType = t.TenderType,
                    allowsChange = t.AllowsChange,
                    requiresExternalReference = t.RequiresExternalReference,
                    sortOrder = t.SortOrder
                })
                .ToListAsync(cancellationToken);

            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new { methods });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query tender methods.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "QUERY_FAILED", "Failed to load payment methods.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandlePaymentCompleteAsync(
        IPaymentService paymentService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        try
        {
            var payload = request.Payload.Value;

            if (!payload.TryGetProperty("tenders", out var tendersProp) || tendersProp.ValueKind != JsonValueKind.Array)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'tenders' is required and must be an array.");
            }

            var tenders = new List<PaymentTenderRequest>();
            foreach (var t in tendersProp.EnumerateArray())
            {
                if (!t.TryGetProperty("tenderMethodId", out var tmIdProp) || !tmIdProp.TryGetInt32(out var tenderMethodId))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Each tender must have an integer 'tenderMethodId'.");
                }
                if (!t.TryGetProperty("amount", out var amtProp) || !amtProp.TryGetDecimal(out var amount))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Each tender must have a numeric 'amount'.");
                }
                string? extRef = null;
                if (t.TryGetProperty("externalPaymentReference", out var extRefProp))
                    extRef = extRefProp.GetString();
                tenders.Add(new PaymentTenderRequest(tenderMethodId, amount, extRef));
            }

            if (tenders.Count == 0)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "At least one tender is required.");
            }

            string? guestName = null;
            if (payload.TryGetProperty("guestName", out var guestNameProp))
                guestName = guestNameProp.GetString();

            string? guestPhone = null;
            if (payload.TryGetProperty("guestPhone", out var guestPhoneProp))
                guestPhone = guestPhoneProp.GetString();

            string? idempotencyKey = null;
            if (payload.TryGetProperty("idempotencyKey", out var idKeyProp))
                idempotencyKey = idKeyProp.GetString();

            var completionRequest = new PaymentCompletionRequest(tenders, guestName, guestPhone, idempotencyKey);
            var result = await paymentService.CompleteOrderAsync(completionRequest, cancellationToken);

            if (!result.Success)
            {
                return BridgeResponseEnvelope.Failure(
                    request.Type, request.RequestId,
                    result.ErrorCode ?? "PAYMENT_FAILED",
                    result.ErrorMessage ?? "Payment could not be completed.");
            }

            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
            {
                orderId = result.OrderId?.ToString(),
                receiptNumber = result.ReceiptNumber,
                changeAmount = result.ChangeAmount,
                receiptText = result.ReceiptText,
                printJobId = result.PrintJobId?.ToString(),
                outboxEventId = result.OutboxEventId?.ToString()
            });
        }
        catch (JsonException)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandleCashGetSummaryAsync(
        ICashControlService cashControlService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await cashControlService.GetDrawerSummaryAsync(cancellationToken);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
            {
                isOpen = summary.IsOpen,
                shiftId = summary.ShiftId?.ToString(),
                businessDate = summary.BusinessDate?.ToString("yyyy-MM-dd"),
                openingFloat = summary.OpeningFloat,
                cashSales = summary.CashSales,
                safeDrops = summary.SafeDrops,
                floatInjections = summary.FloatInjections,
                expectedDrawerBalance = summary.ExpectedDrawerBalance,
                transactionCount = summary.TransactionCount,
                lastMovementAt = summary.LastMovementAt,
                alertCode = summary.AlertCode,
                alertMessage = summary.AlertMessage,
                isSafeDropRecommended = summary.IsSafeDropRecommended,
                isOverLimit = summary.IsOverLimit,
                cashDrawerLimit = summary.CashDrawerLimit,
                safeDropThreshold = summary.SafeDropThreshold
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cash drawer summary.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "SUMMARY_FAILED", "Failed to load drawer summary.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandleCashRecordMovementAsync(
        ICashControlService cashControlService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        if (!request.Payload.HasValue || request.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was missing or invalid.");
        }

        try
        {
            var payload = request.Payload.Value;

            // Validate idempotencyKey
            if (!payload.TryGetProperty("idempotencyKey", out var idKeyProp) ||
                idKeyProp.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(idKeyProp.GetString()))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'idempotencyKey' is required.");
            }
            string idempotencyKey = idKeyProp.GetString()!;

            // Validate amount
            if (!payload.TryGetProperty("amount", out var amountProp) || amountProp.ValueKind == JsonValueKind.Null)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'amount' is required.");
            }
            decimal amount;
            if (amountProp.ValueKind == JsonValueKind.Number)
            {
                if (!amountProp.TryGetDecimal(out amount))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'amount' must be a valid number.");
                }
            }
            else if (amountProp.ValueKind == JsonValueKind.String)
            {
                if (!decimal.TryParse(amountProp.GetString(), out amount))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'amount' must be a valid number.");
                }
            }
            else
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'amount' must be a valid number.");
            }

            // Validate reasonCodeId
            if (!payload.TryGetProperty("reasonCodeId", out var reasonProp) || reasonProp.ValueKind == JsonValueKind.Null)
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'reasonCodeId' is required.");
            }
            int reasonCodeId;
            if (reasonProp.ValueKind == JsonValueKind.Number)
            {
                if (!reasonProp.TryGetInt32(out reasonCodeId))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'reasonCodeId' must be a valid integer.");
                }
            }
            else if (reasonProp.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(reasonProp.GetString(), out reasonCodeId))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'reasonCodeId' must be a valid integer.");
                }
            }
            else
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'reasonCodeId' must be a valid integer.");
            }

            // Parse movementType
            if (!payload.TryGetProperty("movementType", out var movTypeProp))
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Parameter 'movementType' is required.");
            }

            POS.Shared.Enums.CashDrawerMovementType movementType;
            if (movTypeProp.ValueKind == JsonValueKind.String)
            {
                var movTypeStr = movTypeProp.GetString();
                if (string.Equals(movTypeStr, "Drop", StringComparison.OrdinalIgnoreCase))
                {
                    movementType = POS.Shared.Enums.CashDrawerMovementType.Drop;
                }
                else if (string.Equals(movTypeStr, "Payout", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "Correction", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "Injection", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "OpeningFloat", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "SaleCashIn", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "RefundCashOut", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(movTypeStr, "NoSale", StringComparison.OrdinalIgnoreCase))
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "INVALID_MOVEMENT_TYPE", "Only Drop operations are allowed.");
                }
                else
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "INVALID_MOVEMENT_TYPE", "Unsupported movement type.");
                }
            }
            else if (movTypeProp.ValueKind == JsonValueKind.Number)
            {
                if (movTypeProp.TryGetInt32(out var numericVal))
                {
                    if (numericVal == 4)
                    {
                        movementType = POS.Shared.Enums.CashDrawerMovementType.Drop;
                    }
                    else
                    {
                        return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "INVALID_MOVEMENT_TYPE", "Unsupported movement type.");
                    }
                }
                else
                {
                    return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "INVALID_MOVEMENT_TYPE", "Unsupported movement type.");
                }
            }
            else
            {
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "INVALID_MOVEMENT_TYPE", "Unsupported movement type.");
            }

            // Optional comments & manager operator/pin details
            string? comment = null;
            if (payload.TryGetProperty("comment", out var commentProp) && commentProp.ValueKind == JsonValueKind.String)
            {
                comment = commentProp.GetString();
            }

            string? managerOperatorId = null;
            if (payload.TryGetProperty("managerOperatorId", out var manOpProp) && manOpProp.ValueKind == JsonValueKind.String)
            {
                managerOperatorId = manOpProp.GetString();
            }

            string? managerPin = null;
            if (payload.TryGetProperty("managerPin", out var manPinProp) && manPinProp.ValueKind == JsonValueKind.String)
            {
                managerPin = manPinProp.GetString();
            }

            var movementRequest = new CashControlMovementRequest(
                MovementType: movementType,
                Amount: amount,
                ReasonCodeId: reasonCodeId,
                Comment: comment,
                IdempotencyKey: idempotencyKey,
                ManagerOperatorId: managerOperatorId,
                ManagerPin: managerPin
            );

            var result = await cashControlService.RecordMovementAsync(movementRequest, cancellationToken);
            if (!result.Success)
            {
                string code = result.ErrorCode ?? "RECORD_MOVEMENT_FAILED";
                string message = result.ErrorMessage ?? "The movement could not be recorded.";
                return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, code, message);
            }

            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
            {
                success = true,
                movementId = result.MovementId?.ToString(),
                movementType = result.MovementType?.ToString(),
                amount = result.Amount,
                reasonCodeId = result.ReasonCodeId,
                shiftId = result.ShiftId?.ToString(),
                businessDate = result.BusinessDate?.ToString("yyyy-MM-dd"),
                terminalSequence = result.TerminalSequence,
                occurredOn = result.OccurredOn
            });
        }
        catch (JsonException)
        {
            return BridgeResponseEnvelope.Failure(request.Type, request.RequestId, "MALFORMED_REQUEST", "Payload was not valid JSON.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cash drawer movement.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "RECORD_MOVEMENT_FAILED", "Failed to record movement.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandleCashGetLedgerAsync(
        PosLocalDbContext db,
        IProvisionedTerminalContext provisioningContext,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!provisioningContext.IsProvisioned)
            {
                return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
                {
                    isOpen = false,
                    movements = Array.Empty<object>()
                });
            }

            var activeShift = await db.LocalShifts
                .FirstOrDefaultAsync(s => s.LocationId == provisioningContext.CurrentLocationId &&
                                          s.TerminalId == provisioningContext.CurrentTerminalId &&
                                          s.Status == POS.Shared.Enums.ShiftStatus.Open,
                                     cancellationToken);

            if (activeShift == null)
            {
                return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
                {
                    isOpen = false,
                    movements = Array.Empty<object>()
                });
            }

            var movementsQuery = from m in db.LocalCashDrawerMovements
                                 where m.ShiftId == activeShift.Id && m.IsActive
                                 join r in db.LocalReasonCodes on m.ReasonCodeId equals r.Id into reasonJoin
                                 from r in reasonJoin.DefaultIfEmpty()
                                 select new
                                 {
                                     Movement = m,
                                     ReasonCode = r
                                 };

            var movementsList = await movementsQuery
                .OrderByDescending(x => x.Movement.TerminalSequence)
                .Take(100)
                .ToListAsync(cancellationToken);

            var sortedMovements = movementsList
                .OrderByDescending(x => x.Movement.TerminalSequence)
                .ThenByDescending(x => x.Movement.OccurredOn)
                .Select(x => new
                {
                    movementId = x.Movement.Id.ToString(),
                    movementType = x.Movement.MovementType.ToString(),
                    amount = x.Movement.Amount,
                    reasonCodeId = x.Movement.ReasonCodeId,
                    reasonCode = x.ReasonCode?.Code,
                    reasonName = x.ReasonCode?.Name,
                    comment = x.Movement.Comment,
                    authorizedByEmployeeId = x.Movement.AuthorizedByEmployeeId,
                    businessDate = x.Movement.BusinessDate.ToString("yyyy-MM-dd"),
                    terminalSequence = x.Movement.TerminalSequence,
                    occurredOn = x.Movement.OccurredOn
                })
                .ToList();

            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
            {
                isOpen = true,
                shiftId = activeShift.Id.ToString(),
                businessDate = activeShift.BusinessDate.ToString("yyyy-MM-dd"),
                movements = sortedMovements
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cash control ledger.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "LEDGER_FAILED", "Failed to load ledger movements.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandleCashGetReasonCodesAsync(
        PosLocalDbContext db,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allReasonCodes = await db.LocalReasonCodes
                .ToListAsync(cancellationToken);

            var cashControlCodes = allReasonCodes
                .Where(r => string.Equals(r.ReasonCategory, "CashControl", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .ToList();

            bool usedFallback = false;
            var targetList = cashControlCodes;

            if (cashControlCodes.Count == 0)
            {
                usedFallback = true;
                targetList = allReasonCodes
                    .OrderBy(r => r.SortOrder)
                    .ThenBy(r => r.Name)
                    .ToList();
            }

            var reasonCodesPayload = targetList.Select(r => new
            {
                id = r.Id,
                code = r.Code,
                name = r.Name,
                reasonCategory = r.ReasonCategory,
                requiresManagerApproval = r.RequiresManagerApproval
            }).ToList();

            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, new
            {
                reasonCodes = reasonCodesPayload,
                usedFallback = usedFallback,
                reasonCategoryFilter = "CashControl"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cash control reason codes.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "REASON_CODES_FAILED", "Failed to load reason codes.");
        }
    }

    private async Task<BridgeResponseEnvelope> HandleGetSyncStatusAsync(
        ISyncStatusService syncStatusService,
        BridgeRequestEnvelope request,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await syncStatusService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            return BridgeResponseEnvelope.Success(request.Type, request.RequestId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync status.");
            return BridgeResponseEnvelope.Failure(
                request.Type, request.RequestId, "SYNC_STATUS_FAILED", "Failed to load sync status.");
        }
    }
}
