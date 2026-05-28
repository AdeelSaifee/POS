using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Orders;
using POS.Desktop.Services.Receipts;
using POS.Desktop.Services.Session;
using POS.Shared.Contracts;
using POS.Shared.Enums;

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Implements payment processing, split-tender rules, and atomic SQLite persistence.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly PosLocalDbContext _db;
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;
    private readonly IProvisionedTerminalContext _provisionedTerminalContext;
    private readonly IReceiptRenderer _receiptRenderer;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PosLocalDbContext db,
        IOrderService orderService,
        ISessionService sessionService,
        IProvisionedTerminalContext provisionedTerminalContext,
        IReceiptRenderer receiptRenderer,
        ILogger<PaymentService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _provisionedTerminalContext = provisionedTerminalContext ?? throw new ArgumentNullException(nameof(provisionedTerminalContext));
        _receiptRenderer = receiptRenderer ?? throw new ArgumentNullException(nameof(receiptRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PaymentCompletionResult> CompleteOrderAsync(
        PaymentCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 1. Confirm terminal is provisioned
        if (!_provisionedTerminalContext.IsProvisioned)
        {
            _logger.LogWarning("Payment failed: Terminal is not provisioned.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "UNPROVISIONED_TERMINAL",
                ErrorMessage: "The terminal has not been provisioned.");
        }

        int currentTenantId = _provisionedTerminalContext.CurrentTenantId;
        int currentLocationId = _provisionedTerminalContext.CurrentLocationId;
        int currentTerminalId = _provisionedTerminalContext.CurrentTerminalId;

        // 2. Confirm active operator session exists
        if (!_sessionService.IsActive || _sessionService.CurrentSession == null)
        {
            _logger.LogWarning("Payment failed: No active operator session.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "NO_ACTIVE_SESSION",
                ErrorMessage: "An active operator session is required to complete payment.");
        }

        var currentSession = _sessionService.CurrentSession;

        // 3. Confirm LocalTerminalSession exists and is open
        var terminalSession = await _db.LocalTerminalSessions
            .FirstOrDefaultAsync(s => s.TerminalId == currentTerminalId && s.Status == TerminalSessionStatus.Open, cancellationToken);

        if (terminalSession == null)
        {
            _logger.LogWarning("Payment failed: No open terminal session found in database.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "NO_ACTIVE_SESSION",
                ErrorMessage: "The terminal session is not open.");
        }

        // Retrieve EmployeeId
        var employee = await _db.LocalEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeNumber == currentSession.OperatorId, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Payment failed: Active operator '{OperatorId}' not found in local employee database.", currentSession.OperatorId);
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "EMPLOYEE_NOT_FOUND",
                ErrorMessage: "Operator not found in database.");
        }

        // 4. Confirm active open LocalShift exists
        var activeShift = await _db.LocalShifts
            .FirstOrDefaultAsync(s => s.LocationId == currentLocationId && s.TerminalId == currentTerminalId && s.Status == ShiftStatus.Open, cancellationToken);

        if (activeShift == null)
        {
            _logger.LogWarning("Payment failed: No active open shift exists on location {LocationId} and terminal {TerminalId}.", currentLocationId, currentTerminalId);
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "NO_OPEN_SHIFT",
                ErrorMessage: "No active shift is open on this terminal.");
        }

        // 5. Confirm cart is not empty and total is greater than zero
        var cartState = await _orderService.GetCartStateAsync(cancellationToken);
        if (cartState == null || cartState.Lines.Count == 0)
        {
            _logger.LogWarning("Payment failed: Active cart is empty.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "EMPTY_CART",
                ErrorMessage: "Cannot checkout an empty cart.");
        }

        if (cartState.TotalAmount <= 0)
        {
            _logger.LogWarning("Payment failed: Active cart total is zero or negative. Total: {TotalAmount}", cartState.TotalAmount);
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "INVALID_CART_TOTAL",
                ErrorMessage: "Cart total must be greater than zero.");
        }

        // 6. Validate tender list and amounts
        if (request.Tenders == null || request.Tenders.Count == 0)
        {
            _logger.LogWarning("Payment failed: No tenders supplied in payment completion request.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "NO_TENDERS",
                ErrorMessage: "At least one tender is required.");
        }

        foreach (var tender in request.Tenders)
        {
            if (tender.Amount <= 0)
            {
                _logger.LogWarning("Payment failed: Negative or zero tender amount specified. Amount: {Amount}", tender.Amount);
                return new PaymentCompletionResult(
                    Success: false,
                    ErrorCode: "INVALID_TENDER_AMOUNT",
                    ErrorMessage: "Tender amount must be greater than zero.");
            }
        }

        // 7. Verify tender methods exist
        var tenderMethodIds = request.Tenders.Select(t => t.TenderMethodId).Distinct().ToList();
        var dbTenderMethods = await _db.LocalTenderMethods
            .Where(m => tenderMethodIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        if (dbTenderMethods.Count != tenderMethodIds.Count)
        {
            _logger.LogWarning("Payment failed: One or more tender method IDs in the request were invalid.");
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "INVALID_TENDER_METHOD",
                ErrorMessage: "One or more payment/tender methods are invalid.");
        }

        // 8. Confirm total paid covers total due
        decimal totalTendered = MoneyRounder.Round(request.Tenders.Sum(t => t.Amount));
        decimal totalDue = cartState.TotalAmount;

        if (totalTendered < totalDue)
        {
            _logger.LogWarning("Payment failed: Insufficient payment. Tendered: {Tendered}, Due: {Due}", totalTendered, totalDue);
            return new PaymentCompletionResult(
                Success: false,
                ErrorCode: "UNDERPAID",
                ErrorMessage: "The total payment amount does not cover the balance due.");
        }

        // 9. Compute change amount and enforce non-cash overpayment rejection
        decimal changeAmount = 0m;
        if (totalTendered > totalDue)
        {
            var hasCashTender = request.Tenders.Any(t =>
            {
                var method = dbTenderMethods.First(m => m.Id == t.TenderMethodId);
                return method.AllowsChange;
            });

            if (!hasCashTender)
            {
                _logger.LogWarning("Payment failed: Overpayment rejected because no tender method allows change.");
                return new PaymentCompletionResult(
                    Success: false,
                    ErrorCode: "OVERPAYMENT_REJECTED",
                    ErrorMessage: "Overpayment is not allowed for non-cash payment methods.");
            }

            changeAmount = MoneyRounder.Round(totalTendered - totalDue);
        }

        // 10. Generate next order sequence and receipt number
        long nextOrderSequence = 1;
        var lastOrder = await _db.LocalOrders
            .AsNoTracking()
            .Where(o => o.LocationId == currentLocationId && o.TerminalId == currentTerminalId)
            .OrderByDescending(o => o.TerminalSequence)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastOrder != null)
        {
            nextOrderSequence = lastOrder.TerminalSequence + 1;
        }

        var businessDate = activeShift.BusinessDate;
        var receiptNumber = $"{businessDate:yyyyMMdd}-{currentTerminalId}-{nextOrderSequence:D5}";

        // 11. Enforce idempotency key check if provided
        var safeIdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? Guid.NewGuid().ToString("N")
            : request.IdempotencyKey;

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _db.LocalOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.TenantId == currentTenantId && o.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existingOrder != null)
            {
                _logger.LogInformation("Payment completed: Returning existing order from idempotency key '{IdempotencyKey}'", request.IdempotencyKey);
                return new PaymentCompletionResult(
                    Success: true,
                    OrderId: existingOrder.Id,
                    ReceiptNumber: existingOrder.ReceiptNumber,
                    ChangeAmount: existingOrder.ChangeAmount);
            }
        }

        // 12. Create LocalOrder, LocalOrderLines, and LocalPayments
        var correlationId = Guid.NewGuid().ToString("N");
        var orderId = Guid.NewGuid();

        var localOrder = new LocalOrder
        {
            Id = orderId,
            TenantId = currentTenantId,
            LocationId = currentLocationId,
            TerminalId = currentTerminalId,
            ShiftId = activeShift.Id,
            EmployeeId = employee.Id,
            CustomerId = null,
            OriginalOrderId = null,
            BusinessDate = businessDate,
            TerminalSequence = nextOrderSequence,
            ReceiptNumber = receiptNumber,
            OrderType = OrderType.Sale,
            Status = OrderStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            FulfillmentStatus = FulfillmentStatus.Completed,
            CatalogVersion = 1,
            PriceListId = null,
            RuleVersion = 1,
            ReceiptTemplateId = null,
            SubtotalAmount = cartState.SubtotalAmount,
            DiscountAmount = cartState.DiscountAmount,
            TaxAmount = cartState.TaxAmount,
            TotalAmount = cartState.TotalAmount,
            PaidAmount = totalTendered,
            ChangeAmount = changeAmount,
            CurrencyCode = "PKR",
            GuestName = request.GuestName,
            GuestPhone = request.GuestPhone,
            CompletedOn = DateTimeOffset.UtcNow,
            VoidedOn = null,
            SyncedOn = null,
            MetadataJson = null,
            IdempotencyKey = safeIdempotencyKey,
            CorrelationId = correlationId,
            IsActive = true,
            CreatedBy = currentSession.DisplayName,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedBy = null,
            UpdatedOn = null
        };

        int lineNumber = 1;
        var orderLines = new List<LocalOrderLine>();
        foreach (var line in cartState.Lines)
        {
            var localLine = new LocalOrderLine
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                OrderId = orderId,
                LocationId = currentLocationId,
                TerminalId = currentTerminalId,
                ItemId = line.ItemId,
                ItemVariantId = line.VariantId,
                OriginalOrderLineId = null,
                ReasonCodeId = null,
                AuthorizedByEmployeeId = null,
                LineNumber = lineNumber++,
                LineType = "Product",
                Status = "Active",
                SKU = null,
                Barcode = null,
                ItemName = line.Name,
                VariantName = null,
                UnitOfMeasureCode = line.Unit ?? "PCS",
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                GrossAmount = line.GrossAmount,
                DiscountAmount = line.DiscountAmount,
                TaxAmount = line.TaxAmount,
                NetAmount = line.NetAmount,
                TaxRuleId = line.TaxRuleId,
                TaxRate = line.TaxRate,
                PriceListId = null,
                CatalogVersion = 1,
                MetadataJson = null,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                CorrelationId = correlationId,
                IsActive = true,
                CreatedBy = currentSession.DisplayName,
                CreatedOn = localOrder.CreatedOn,
                UpdatedBy = null,
                UpdatedOn = null
            };
            orderLines.Add(localLine);
        }

        var payments = new List<LocalPayment>();
        foreach (var tender in request.Tenders)
        {
            var method = dbTenderMethods.First(m => m.Id == tender.TenderMethodId);

            var localPayment = new LocalPayment
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                OrderId = orderId,
                LocationId = currentLocationId,
                TerminalId = currentTerminalId,
                ShiftId = activeShift.Id,
                TenderMethodId = tender.TenderMethodId,
                OriginalPaymentId = null,
                BusinessDate = businessDate,
                TerminalSequence = nextOrderSequence,
                PaymentType = PaymentType.Sale,
                Status = PaymentStatus.Paid,
                Amount = tender.Amount,
                CurrencyCode = "PKR",
                AuthorizedAmount = tender.Amount,
                CapturedAmount = tender.Amount,
                PaymentToken = null,
                ExternalPaymentReference = tender.ExternalPaymentReference,
                AuthorizationCode = null,
                CardBrand = null,
                CardLast4 = null,
                FailureCode = null,
                FailureMessage = null,
                RequiresReconciliation = false,
                ReconciledOn = null,
                ProcessedOn = DateTimeOffset.UtcNow,
                SyncedOn = null,
                MetadataJson = null,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                CorrelationId = correlationId,
                IsActive = true,
                CreatedBy = currentSession.DisplayName,
                CreatedOn = localOrder.CreatedOn,
                UpdatedBy = null,
                UpdatedOn = null
            };
            payments.Add(localPayment);
        }

        // 13. Execute atomic SQLite transaction commit
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        string receiptText;
        SyncOutbox syncOutbox;
        PrintQueue printQueue;

        try
        {
            // Render receipt text using persisted order data mapping
            var tenderMethodNames = dbTenderMethods.ToDictionary(m => m.Id, m => m.Name);
            receiptText = _receiptRenderer.RenderReceipt(localOrder, orderLines, payments, tenderMethodNames);

            // Serialize standard structured SyncOutbox payload
            var payload = new
            {
                Order = new
                {
                    localOrder.Id,
                    localOrder.TenantId,
                    localOrder.LocationId,
                    localOrder.TerminalId,
                    localOrder.ShiftId,
                    localOrder.EmployeeId,
                    localOrder.CustomerId,
                    localOrder.OriginalOrderId,
                    localOrder.BusinessDate,
                    localOrder.TerminalSequence,
                    localOrder.ReceiptNumber,
                    localOrder.OrderType,
                    localOrder.Status,
                    localOrder.PaymentStatus,
                    localOrder.FulfillmentStatus,
                    localOrder.CatalogVersion,
                    localOrder.PriceListId,
                    localOrder.RuleVersion,
                    localOrder.ReceiptTemplateId,
                    localOrder.SubtotalAmount,
                    localOrder.DiscountAmount,
                    localOrder.TaxAmount,
                    localOrder.TotalAmount,
                    localOrder.PaidAmount,
                    localOrder.ChangeAmount,
                    localOrder.CurrencyCode,
                    localOrder.GuestName,
                    localOrder.GuestPhone,
                    localOrder.CompletedOn,
                    localOrder.IdempotencyKey,
                    localOrder.CorrelationId
                },
                Lines = orderLines.Select(line => new
                {
                    line.Id,
                    line.TenantId,
                    line.OrderId,
                    line.LocationId,
                    line.TerminalId,
                    line.ItemId,
                    line.ItemVariantId,
                    line.OriginalOrderLineId,
                    line.ReasonCodeId,
                    line.AuthorizedByEmployeeId,
                    line.LineNumber,
                    line.LineType,
                    line.Status,
                    line.SKU,
                    line.Barcode,
                    line.ItemName,
                    line.VariantName,
                    line.UnitOfMeasureCode,
                    line.Quantity,
                    line.UnitPrice,
                    line.GrossAmount,
                    line.DiscountAmount,
                    line.TaxAmount,
                    line.NetAmount,
                    line.TaxRuleId,
                    line.TaxRate,
                    line.PriceListId,
                    line.CatalogVersion,
                    line.IdempotencyKey,
                    line.CorrelationId
                }).ToList(),
                Payments = payments.Select(p => new
                {
                    p.Id,
                    p.TenantId,
                    p.OrderId,
                    p.LocationId,
                    p.TerminalId,
                    p.ShiftId,
                    p.TenderMethodId,
                    p.OriginalPaymentId,
                    p.BusinessDate,
                    p.TerminalSequence,
                    p.PaymentType,
                    p.Status,
                    p.Amount,
                    p.CurrencyCode,
                    p.AuthorizedAmount,
                    p.CapturedAmount,
                    p.PaymentToken,
                    p.ExternalPaymentReference,
                    p.AuthorizationCode,
                    p.CardBrand,
                    p.CardLast4,
                    p.ProcessedOn,
                    p.IdempotencyKey,
                    p.CorrelationId
                }).ToList()
            };
            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadHash = ComputeSha256Hash(payloadJson);

            syncOutbox = new SyncOutbox
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                LocationId = currentLocationId,
                TerminalId = currentTerminalId,
                BusinessDate = businessDate,
                TerminalSequence = nextOrderSequence,
                EventType = "OrderCompleted",
                EventId = orderId,
                PayloadJson = payloadJson,
                PayloadHash = payloadHash,
                IdempotencyKey = $"order-completed:{orderId}",
                CorrelationId = correlationId,
                Status = SyncOutboxStatus.Pending,
                AttemptCount = 0,
                IsActive = true,
                CreatedBy = currentSession.DisplayName,
                CreatedOn = localOrder.CreatedOn
            };

            // Serialize print metadata payload
            var printPayloadJson = JsonSerializer.Serialize(new
            {
                orderId = orderId,
                receiptNumber = receiptNumber,
                totalAmount = localOrder.TotalAmount,
                paymentCount = payments.Count,
                createdOn = localOrder.CreatedOn
            });

            printQueue = new PrintQueue
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                LocationId = currentLocationId,
                TerminalId = currentTerminalId,
                OrderId = orderId,
                PrintJobType = "Receipt",
                ReceiptNumber = receiptNumber,
                ReceiptTemplateId = null,
                PayloadJson = printPayloadJson,
                RenderedContent = receiptText,
                Status = PrintQueueStatus.Pending,
                Priority = 1,
                AttemptCount = 0,
                IdempotencyKey = $"receipt-print:{orderId}",
                CorrelationId = correlationId,
                IsActive = true,
                CreatedBy = currentSession.DisplayName,
                CreatedOn = localOrder.CreatedOn
            };

            _db.LocalOrders.Add(localOrder);
            _db.LocalOrderLines.AddRange(orderLines);
            _db.LocalPayments.AddRange(payments);
            _db.SyncOutbox.Add(syncOutbox);
            _db.PrintQueue.Add(printQueue);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Order, outbox event and print job saved atomically inside SQLite transaction. Receipt: '{ReceiptNumber}', OrderId: {OrderId}", receiptNumber, orderId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction failed and rolled back while completing order.");
            throw;
        }

        // 14. Clear active C# draft cart only after successful save
        try
        {
            await _orderService.ClearCartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear active draft cart after successful order commit.");
        }

        return new PaymentCompletionResult(
            Success: true,
            OrderId: orderId,
            ReceiptNumber: receiptNumber,
            ChangeAmount: changeAmount,
            ReceiptText: receiptText,
            PrintJobId: printQueue.Id,
            OutboxEventId: syncOutbox.Id);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}
