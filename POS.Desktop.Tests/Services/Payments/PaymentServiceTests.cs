using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Data;
using POS.Desktop.Data.LocalEntities;
using POS.Desktop.Services.Orders;
using POS.Desktop.Services.Payments;
using POS.Desktop.Services.Receipts;
using POS.Desktop.Services.Provisioning;
using POS.Desktop.Services.Session;
using POS.Desktop.Tests.TestSupport;
using POS.Shared.Contracts;
using POS.Shared.Enums;
using Xunit;

namespace POS.Desktop.Tests.Services.Payments;

public class PaymentServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _dbHarness = new();
    private readonly int _tenantId = 1;
    private readonly int _locationId = 101;
    private readonly int _terminalId = 999;
    private readonly string _operatorId = "EMP123";

    public void Dispose()
    {
        _dbHarness.Dispose();
    }

    private PosLocalDbContext CreateDbContext(IProvisionedTerminalContext? context = null)
    {
        return _dbHarness.CreateDbContext(context);
    }

    private async Task SeedBaseDataAsync(PosLocalDbContext db)
    {
        // 1. Seed tender methods
        db.LocalTenderMethods.AddRange(
            new LocalTenderMethod { Id = 1, TenantId = _tenantId, Code = "CASH", Name = "Cash", TenderType = "Cash", AllowsChange = true, RequiresExternalReference = false },
            new LocalTenderMethod { Id = 2, TenantId = _tenantId, Code = "CARD", Name = "Card", TenderType = "Card", AllowsChange = false, RequiresExternalReference = true }
        );

        // 2. Seed employee
        db.LocalEmployees.Add(new LocalEmployee
        {
            Id = 10,
            TenantId = _tenantId,
            EmployeeNumber = _operatorId,
            DisplayName = "John Cashier",
            Status = EmployeeStatus.Active
        });

        // 3. Seed active terminal session
        db.LocalTerminalSessions.Add(new LocalTerminalSession
        {
            Id = 1,
            TenantId = _tenantId,
            LocationId = _locationId,
            TerminalId = _terminalId,
            EmployeeId = 10,
            EmployeeNumber = _operatorId,
            DisplayName = "John Cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        // 4. Seed active shift
        db.LocalShifts.Add(new LocalShift
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            LocationId = _locationId,
            TerminalId = _terminalId,
            OpenedByEmployeeId = 10,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TerminalSequence = 1,
            Status = ShiftStatus.Open,
            OpeningCashAmount = 100m,
            OpenedOn = DateTimeOffset.UtcNow,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CompleteOrderAsync_ExactCashPayment_SucceedsAndPersists()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                DiscountAmount = 0m,
                TaxAmount = 5m,
                TotalAmount = 105m,
                Lines = new List<CartLineDto>
                {
                    new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, TaxAmount = 5m, NetAmount = 105m, Unit = "PCS" }
                }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 105m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);
        Assert.NotNull(result.ReceiptNumber);
        Assert.Equal(0m, result.ChangeAmount);

        // Verify order saved in SQLite database
        var savedOrder = await db.LocalOrders.FirstOrDefaultAsync(o => o.Id == result.OrderId);
        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Completed, savedOrder.Status);
        Assert.Equal(PaymentStatus.Paid, savedOrder.PaymentStatus);
        Assert.Equal(105m, savedOrder.TotalAmount);
        Assert.Equal(105m, savedOrder.PaidAmount);
        Assert.Equal(0m, savedOrder.ChangeAmount);

        var savedLine = await db.LocalOrderLines.FirstOrDefaultAsync(l => l.OrderId == result.OrderId);
        Assert.NotNull(savedLine);
        Assert.Equal(101, savedLine.ItemVariantId);
        Assert.Equal(105m, savedLine.NetAmount);

        var savedPayment = await db.LocalPayments.FirstOrDefaultAsync(p => p.OrderId == result.OrderId);
        Assert.NotNull(savedPayment);
        Assert.Equal(1, savedPayment.TenderMethodId);
        Assert.Equal(105m, savedPayment.Amount);
        Assert.False(savedPayment.RequiresReconciliation);

        var hasRecon = await db.PaymentReconciliationQueue.AnyAsync(r => r.PaymentId == savedPayment.Id);
        Assert.False(hasRecon);

        // Draft cart must be cleared
        Assert.True(stubOrderService.ClearCartCalled);
    }

    [Fact]
    public async Task CompleteOrderAsync_CashOverpayment_ReturnsCorrectChange()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 90m,
                DiscountAmount = 0m,
                TaxAmount = 0m,
                TotalAmount = 90m,
                Lines = new List<CartLineDto>
                {
                    new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 90m, GrossAmount = 90m, TaxAmount = 0m, NetAmount = 90m, Unit = "PCS" }
                }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) }, // 100 paid for 90 due
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10m, result.ChangeAmount);

        var savedOrder = await db.LocalOrders.FirstOrDefaultAsync(o => o.Id == result.OrderId);
        Assert.NotNull(savedOrder);
        Assert.Equal(100m, savedOrder.PaidAmount);
        Assert.Equal(10m, savedOrder.ChangeAmount);
    }

    [Fact]
    public async Task CompleteOrderAsync_CardExactPayment_Succeeds()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 150m,
                TotalAmount = 150m,
                Lines = new List<CartLineDto> { new() { ItemId = 2, VariantId = 102, Name = "Item B", Quantity = 1, UnitPrice = 150m, GrossAmount = 150m, NetAmount = 150m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(2, 150m, "TXN-CARD-99") },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        var savedPayment = await db.LocalPayments.FirstOrDefaultAsync(p => p.OrderId == result.OrderId);
        Assert.NotNull(savedPayment);
        Assert.Equal(2, savedPayment.TenderMethodId);
        Assert.Equal("TXN-CARD-99", savedPayment.ExternalPaymentReference);
        Assert.True(savedPayment.RequiresReconciliation);

        var reconRow = await db.PaymentReconciliationQueue.FirstOrDefaultAsync(r => r.PaymentId == savedPayment.Id);
        Assert.NotNull(reconRow);
        Assert.Null(reconRow.PaymentToken); // Assert PaymentToken is null per security rules
        Assert.Equal(PaymentReconciliationStatus.Pending, reconRow.Status);
        Assert.Equal("TXN-CARD-99", reconRow.ExternalPaymentReference);
        Assert.Equal($"reconciliation:payment:{savedPayment.Id}", reconRow.IdempotencyKey);
    }

    [Fact]
    public async Task CompleteOrderAsync_SplitTender_Succeeds()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 200m,
                TotalAmount = 200m,
                Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 2, UnitPrice = 100m, GrossAmount = 200m, NetAmount = 200m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        // Act: split pay 50 Cash + 150 Card
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m), new(2, 150m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        var savedPayments = await db.LocalPayments.Where(p => p.OrderId == result.OrderId).ToListAsync();
        Assert.Equal(2, savedPayments.Count);
        Assert.Contains(savedPayments, p => p.TenderMethodId == 1 && p.Amount == 50m);
        Assert.Contains(savedPayments, p => p.TenderMethodId == 2 && p.Amount == 150m);
    }

    [Fact]
    public async Task CompleteOrderAsync_UnderpaidTender_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                TotalAmount = 100m,
                Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 99m) }, // underpaid by 1 PKR
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UNDERPAID", result.ErrorCode);
        Assert.False(stubOrderService.ClearCartCalled);
    }

    [Fact]
    public async Task CompleteOrderAsync_EmptyCart_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService { CartState = new CartStateDto() }; // empty
        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EMPTY_CART", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_UnprovisionedTerminal_GetsRejected()
    {
        // Arrange
        var provContext = new ProvisionedTerminalContext(); // Unprovisioned
        using var db = CreateDbContext(provContext);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto { TotalAmount = 50m, Lines = new List<CartLineDto> { new() { VariantId = 1 } } }
        };
        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UNPROVISIONED_TERMINAL", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_NoActiveSession_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto { TotalAmount = 50m, Lines = new List<CartLineDto> { new() { VariantId = 1 } } }
        };
        var stubSessionService = new StubSessionService(); // No session

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_ACTIVE_SESSION", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_NoOpenShift_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);

        // Seed base data except open shift
        db.LocalTenderMethods.Add(new LocalTenderMethod { Id = 1, TenantId = _tenantId, Code = "CASH", Name = "Cash", TenderType = "Cash", AllowsChange = true });
        db.LocalEmployees.Add(new LocalEmployee { Id = 10, TenantId = _tenantId, EmployeeNumber = _operatorId, DisplayName = "John", Status = EmployeeStatus.Active });
        db.LocalTerminalSessions.Add(new LocalTerminalSession
        {
            Id = 1,
            TenantId = _tenantId,
            LocationId = _locationId,
            TerminalId = _terminalId,
            EmployeeId = 10,
            EmployeeNumber = _operatorId,
            DisplayName = "John Cashier",
            Role = "Cashier",
            Status = TerminalSessionStatus.Open,
            LoggedInOn = DateTimeOffset.UtcNow,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await db.SaveChangesAsync();

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto { TotalAmount = 50m, Lines = new List<CartLineDto> { new() { VariantId = 1 } } }
        };
        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_OPEN_SHIFT", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_InvalidTenderMethod_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto { TotalAmount = 50m, Lines = new List<CartLineDto> { new() { VariantId = 1 } } }
        };
        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(999, 50m) }, // Unknown tender ID 999
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_TENDER_METHOD", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_OverpaymentWithoutCashAllowsChangeTender_GetsRejected()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                TotalAmount = 100m,
                Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(2, 105m, "TXN-CARD") }, // Card (AllowsChange = false) overpaid by 5 PKR
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("OVERPAYMENT_REJECTED", result.ErrorCode);
    }

    [Fact]
    public async Task CompleteOrderAsync_Success_CreatesSyncOutboxAndPrintQueueRows()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                DiscountAmount = 10m,
                TaxAmount = 5m,
                TotalAmount = 95m,
                Lines = new List<CartLineDto>
                {
                    new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, DiscountAmount = 10m, TaxAmount = 5m, NetAmount = 95m, Unit = "PCS" }
                }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) }, // 100 paid for 95 due (5 change)
            GuestName: "Alice Guest",
            GuestPhone: "555-1234",
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.PrintJobId);
        Assert.NotNull(result.OutboxEventId);
        Assert.NotNull(result.ReceiptText);

        // Verify SyncOutbox row
        var outboxRow = await db.SyncOutbox.FirstOrDefaultAsync(x => x.Id == result.OutboxEventId);
        Assert.NotNull(outboxRow);
        Assert.Equal("OrderCompleted", outboxRow.EventType);
        Assert.Equal(result.OrderId, outboxRow.EventId);
        Assert.Equal(SyncOutboxStatus.Pending, outboxRow.Status);
        Assert.Equal(0, outboxRow.AttemptCount);
        Assert.Equal($"order-completed:{result.OrderId}", outboxRow.IdempotencyKey);
        Assert.Equal(provRecord.TenantId, outboxRow.TenantId);
        Assert.Equal(provRecord.LocationId, outboxRow.LocationId);
        Assert.Equal(provRecord.TerminalId, outboxRow.TerminalId);
        Assert.True(outboxRow.IsActive);
        Assert.NotEmpty(outboxRow.PayloadJson);
        Assert.NotEmpty(outboxRow.PayloadHash);

        // Verify PrintQueue row
        var printRow = await db.PrintQueue.FirstOrDefaultAsync(x => x.Id == result.PrintJobId);
        Assert.NotNull(printRow);
        Assert.Equal("Receipt", printRow.PrintJobType);
        Assert.Equal(result.OrderId, printRow.OrderId);
        Assert.Equal(result.ReceiptNumber, printRow.ReceiptNumber);
        Assert.Equal(PrintQueueStatus.Pending, printRow.Status);
        Assert.Equal(1, printRow.Priority);
        Assert.Equal(0, printRow.AttemptCount);
        Assert.Equal($"receipt-print:{result.OrderId}", printRow.IdempotencyKey);
        Assert.Equal(provRecord.TenantId, printRow.TenantId);
        Assert.Equal(provRecord.LocationId, printRow.LocationId);
        Assert.Equal(provRecord.TerminalId, printRow.TerminalId);
        Assert.True(printRow.IsActive);
        Assert.NotEmpty(printRow.PayloadJson);
        Assert.Equal(result.ReceiptText, printRow.RenderedContent);
    }

    [Fact]
    public async Task CompleteOrderAsync_ReceiptRenderedContent_ContainsCorrectFormatAndData()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                DiscountAmount = 10m,
                TaxAmount = 5m,
                TotalAmount = 95m,
                Lines = new List<CartLineDto>
                {
                    new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, DiscountAmount = 10m, TaxAmount = 5m, NetAmount = 95m, Unit = "PCS" }
                }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) }, // 100 paid for 95 due (5 change)
            GuestName: "Alice Guest",
            GuestPhone: "555-1234",
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        var receipt = result.ReceiptText;
        Assert.NotNull(receipt);
        Assert.NotNull(result.ReceiptNumber);

        // Check required fields in plain text receipt content
        Assert.Contains(result.ReceiptNumber, receipt);
        Assert.Contains("TOTAL:", receipt);
        Assert.Contains("95.00", receipt);
        Assert.Contains("Subtotal:", receipt);
        Assert.Contains("100.00", receipt);
        Assert.Contains("Discount:", receipt);
        Assert.Contains("10.00", receipt);
        Assert.Contains("Tax:", receipt);
        Assert.Contains("5.00", receipt);
        Assert.Contains("Cash:", receipt);
        Assert.Contains("Change Due:", receipt);
        Assert.Contains("Terminal ID: 999", receipt);
        Assert.Contains("Cashier:     John Cashier", receipt);
        Assert.Contains("Guest:       Alice Guest", receipt);
    }

    [Fact]
    public async Task CompleteOrderAsync_Failure_DoesNotCreateSyncOutboxOrPrintQueueRows()
    {
        // Arrange
        var provContext = new ProvisionedTerminalContext(); // Unprovisioned
        using var db = CreateDbContext(provContext);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto { TotalAmount = 50m, Lines = new List<CartLineDto> { new() { VariantId = 1 } } }
        };
        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 50m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UNPROVISIONED_TERMINAL", result.ErrorCode);

        // Verify no sync outbox or print queue records exist
        var outboxCount = await db.SyncOutbox.CountAsync();
        var printCount = await db.PrintQueue.CountAsync();
        Assert.Equal(0, outboxCount);
        Assert.Equal(0, printCount);
    }

    [Fact]
    public async Task CompleteOrderAsync_TransactionFailure_RollsBackAllEntitiesAtomically()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 200m,
                DiscountAmount = 0m,
                TaxAmount = 0m,
                TotalAmount = 200m,
                Lines = new List<CartLineDto>
                {
                    // Quantity is -1 (negative) to trigger database check constraint violation CK_LocalOrderLine_Quantity
                    new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = -1, UnitPrice = -200m, GrossAmount = 200m, TaxAmount = 0m, NetAmount = 200m, Unit = "PCS" }
                }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 200m) },
            IdempotencyKey: Guid.NewGuid().ToString("N")
        );

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await paymentService.CompleteOrderAsync(request);
        });

        // Verify database is empty of orders, order lines, payments, sync outbox, and print queue due to transaction rollback
        var ordersCount = await db.LocalOrders.CountAsync();
        var linesCount = await db.LocalOrderLines.CountAsync();
        var paymentsCount = await db.LocalPayments.CountAsync();
        var outboxCount = await db.SyncOutbox.CountAsync();
        var printCount = await db.PrintQueue.CountAsync();

        Assert.Equal(0, ordersCount);
        Assert.Equal(0, linesCount);
        Assert.Equal(0, paymentsCount);
        Assert.Equal(0, outboxCount);
        Assert.Equal(0, printCount);

        // Active cart state must NOT have been cleared because save failed
        Assert.False(stubOrderService.ClearCartCalled);
    }

    [Fact]
    public async Task CompleteOrderAsync_MissingIdempotencyKey_RejectsWithIdempotencyKeyRequired()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                TotalAmount = 100m,
                Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) },
            IdempotencyKey: null // Missing
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", result.ErrorCode);
        Assert.Contains("key is required", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteOrderAsync_SuccessfulCompletion_StoresFingerprintInMetadataJson()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var stubOrderService = new StubOrderService
        {
            CartState = new CartStateDto
            {
                SubtotalAmount = 100m,
                TotalAmount = 100m,
                Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) },
            IdempotencyKey: idempotencyKey
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        var savedOrder = await db.LocalOrders.FirstAsync(o => o.Id == result.OrderId);
        Assert.NotNull(savedOrder.MetadataJson);
        Assert.Contains("PayloadFingerprint", savedOrder.MetadataJson);
    }

    [Fact]
    public async Task CompleteOrderAsync_RepeatedKeyAfterCartCleared_BypassesFingerprintAndReturnsOriginalResult()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var cartState = new CartStateDto
        {
            SubtotalAmount = 100m,
            TotalAmount = 100m,
            Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
        };
        var stubOrderService = new StubOrderService { CartState = cartState };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var idempotencyKey = "TEST-REPEAT-KEY";
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) },
            IdempotencyKey: idempotencyKey
        );

        // First Completion
        var firstResult = await paymentService.CompleteOrderAsync(request);
        Assert.True(firstResult.Success);

        // Clear cart to simulate post-success empty-cart state
        stubOrderService.CartState = new CartStateDto(); // Empty cart

        // Act - Retry completion with same key and now empty cart
        var secondResult = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(secondResult.Success);
        Assert.Equal(firstResult.OrderId, secondResult.OrderId);
        Assert.Equal(firstResult.ReceiptNumber, secondResult.ReceiptNumber);
        Assert.Equal(firstResult.ChangeAmount, secondResult.ChangeAmount);
        Assert.Equal(firstResult.ReceiptText, secondResult.ReceiptText);
        Assert.Equal(firstResult.PrintJobId, secondResult.PrintJobId);
        Assert.Equal(firstResult.OutboxEventId, secondResult.OutboxEventId);

        // Verify no duplicate entities created
        var ordersCount = await db.LocalOrders.CountAsync(o => o.IdempotencyKey == idempotencyKey);
        var paymentsCount = await db.LocalPayments.CountAsync(p => p.OrderId == firstResult.OrderId);
        var outboxCount = await db.SyncOutbox.CountAsync(s => s.EventId == firstResult.OrderId);
        var printCount = await db.PrintQueue.CountAsync(p => p.OrderId == firstResult.OrderId);

        Assert.Equal(1, ordersCount);
        Assert.Equal(1, paymentsCount);
        Assert.Equal(1, outboxCount);
        Assert.Equal(1, printCount);
    }

    [Fact]
    public async Task CompleteOrderAsync_RepeatedKeyWithChangedPayload_RejectsWithConflict()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var cartState = new CartStateDto
        {
            SubtotalAmount = 100m,
            TotalAmount = 100m,
            Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
        };
        var stubOrderService = new StubOrderService { CartState = cartState };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        var idempotencyKey = "TEST-CONFLICT-KEY";
        var request1 = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) },
            IdempotencyKey: idempotencyKey
        );

        // First Completion
        var firstResult = await paymentService.CompleteOrderAsync(request1);
        Assert.True(firstResult.Success);

        // Second request has the same key but different cart lines
        var differentCartState = new CartStateDto
        {
            SubtotalAmount = 200m,
            TotalAmount = 200m,
            Lines = new List<CartLineDto> { new() { ItemId = 2, VariantId = 102, Name = "Item B", Quantity = 1, UnitPrice = 200m, GrossAmount = 200m, NetAmount = 200m } }
        };
        stubOrderService.CartState = differentCartState;

        var request2 = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 200m) },
            IdempotencyKey: idempotencyKey
        );

        // Act
        var secondResult = await paymentService.CompleteOrderAsync(request2);

        // Assert
        Assert.False(secondResult.Success);
        Assert.Equal("IDEMPOTENCY_CONFLICT", secondResult.ErrorCode);
        Assert.Contains("already exists with a different payload", secondResult.ErrorMessage);
    }

    [Fact]
    public async Task CompleteOrderAsync_UniqueConstraintRaceConflict_SafelyHandlesRollbackAndReturnsExistingResult()
    {
        // Arrange
        var provRecord = new ProvisioningRecord(_tenantId, _locationId, _terminalId);
        var provContext = new ProvisionedTerminalContext(provRecord);
        using var db = CreateDbContext(provContext);
        await SeedBaseDataAsync(db);

        var activeShift = await db.LocalShifts.FirstAsync(s => s.Status == ShiftStatus.Open);

        var cartState = new CartStateDto
        {
            SubtotalAmount = 100m,
            TotalAmount = 100m,
            Lines = new List<CartLineDto> { new() { ItemId = 1, VariantId = 101, Name = "Item A", Quantity = 1, UnitPrice = 100m, GrossAmount = 100m, NetAmount = 100m } }
        };

        var idempotencyKey = "TEST-RACE-KEY";
        var request = new PaymentCompletionRequest(
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) },
            IdempotencyKey: idempotencyKey
        );

        Guid printQueueId = Guid.NewGuid();
        Guid syncOutboxId = Guid.NewGuid();
        Guid concurrentOrderId = Guid.NewGuid();
        string receiptNumber = "20260528-999-00099";

        // We simulate the concurrent insert race by inserting the duplicate order inside OnGetCartState
        // which runs right after the early lookup and before the main db transaction starts.
        var stubOrderService = new StubOrderService
        {
            CartState = cartState,
            OnGetCartState = async () =>
            {
                using var db2 = CreateDbContext(provContext);

                var concurrentOrder = new LocalOrder
                {
                    Id = concurrentOrderId,
                    TenantId = _tenantId,
                    LocationId = _locationId,
                    TerminalId = _terminalId,
                    ShiftId = activeShift.Id,
                    EmployeeId = 10,
                    BusinessDate = activeShift.BusinessDate,
                    TerminalSequence = 99,
                    ReceiptNumber = receiptNumber,
                    OrderType = OrderType.Sale,
                    Status = OrderStatus.Completed,
                    PaymentStatus = PaymentStatus.Paid,
                    FulfillmentStatus = FulfillmentStatus.Completed,
                    SubtotalAmount = 100m,
                    TotalAmount = 100m,
                    IdempotencyKey = idempotencyKey,
                    MetadataJson = $"{{\"PayloadFingerprint\":\"{BuildFingerprint(cartState, request, activeShift.Id, activeShift.BusinessDate)}\"}}"
                };
                db2.LocalOrders.Add(concurrentOrder);

                var printQueue = new PrintQueue
                {
                    Id = printQueueId,
                    TenantId = _tenantId,
                    LocationId = _locationId,
                    TerminalId = _terminalId,
                    OrderId = concurrentOrder.Id,
                    PrintJobType = "Receipt",
                    ReceiptNumber = concurrentOrder.ReceiptNumber,
                    RenderedContent = "Concurrently Rendered Receipt Content",
                    Status = PrintQueueStatus.Pending
                };
                db2.PrintQueue.Add(printQueue);

                var syncOutbox = new SyncOutbox
                {
                    Id = syncOutboxId,
                    TenantId = _tenantId,
                    LocationId = _locationId,
                    TerminalId = _terminalId,
                    BusinessDate = concurrentOrder.BusinessDate,
                    EventType = "OrderCompleted",
                    EventId = concurrentOrder.Id,
                    Status = SyncOutboxStatus.Pending
                };
                db2.SyncOutbox.Add(syncOutbox);

                await db2.SaveChangesAsync();
            }
        };

        var stubSessionService = new StubSessionService
        {
            CurrentSession = new OperatorSession(_operatorId, "John Cashier", "Cashier", DateTimeOffset.UtcNow, _terminalId.ToString(), "STUB-SESS")
        };

        var paymentService = new PaymentService(db, stubOrderService, stubSessionService, provContext, new ReceiptRenderer(), NullLogger<PaymentService>.Instance);

        // Act - Call CompleteOrderAsync which does not find it in early lookup,
        // but throws UNIQUE constraint violation on SaveChangesAsync, rolls back, and reloads!
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(concurrentOrderId, result.OrderId);
        Assert.Equal(receiptNumber, result.ReceiptNumber);
        Assert.Equal("Concurrently Rendered Receipt Content", result.ReceiptText);
        Assert.Equal(printQueueId, result.PrintJobId);
        Assert.Equal(syncOutboxId, result.OutboxEventId);
    }

    private string BuildFingerprint(CartStateDto cartState, PaymentCompletionRequest request, Guid shiftId, DateOnly businessDate)
    {
        var details = new
        {
            TenantId = _tenantId,
            LocationId = _locationId,
            TerminalId = _terminalId,
            ShiftId = shiftId,
            BusinessDate = businessDate,
            Lines = cartState.Lines.Select(l => new
            {
                ItemId = l.ItemId,
                VariantId = l.VariantId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                GrossAmount = l.GrossAmount,
                DiscountAmount = l.DiscountAmount,
                TaxAmount = l.TaxAmount,
                NetAmount = l.NetAmount
            }).OrderBy(l => l.ItemId).ThenBy(l => l.VariantId).ToList(),
            Totals = new
            {
                cartState.SubtotalAmount,
                cartState.DiscountAmount,
                cartState.TaxAmount,
                cartState.TotalAmount
            },
            Tenders = request.Tenders.Select(t => new
            {
                t.TenderMethodId,
                Amount = MoneyRounder.Round(t.Amount),
                ExternalPaymentReference = t.ExternalPaymentReference ?? string.Empty
            }).OrderBy(t => t.TenderMethodId).ThenBy(t => t.Amount).ThenBy(t => t.ExternalPaymentReference).ToList(),
            Guest = new
            {
                GuestName = request.GuestName?.Trim() ?? string.Empty,
                GuestPhone = request.GuestPhone?.Trim() ?? string.Empty
            }
        };
        return ComputeSha256Hash(System.Text.Json.JsonSerializer.Serialize(details));
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }

    private class StubOrderService : IOrderService
    {
        public CartStateDto CartState { get; set; } = new();
        public bool ClearCartCalled { get; set; }
        public Func<Task>? OnGetCartState { get; set; }

        public async Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default)
        {
            if (OnGetCartState != null)
            {
                await OnGetCartState();
            }
            return CartState;
        }

        public Task<CartStateDto> ClearCartAsync(CancellationToken cancellationToken = default)
        {
            ClearCartCalled = true;
            return Task.FromResult(new CartStateDto());
        }

        public Task<CartStateDto> AddItemAsync(int variantId, int quantity = 1, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CartStateDto> UpdateLineQuantityAsync(int variantId, int quantity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CartStateDto> RemoveItemAsync(int variantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CartStateDto> ApplyDiscountAsync(string discountType, decimal discountValue, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CartStateDto> RemoveDiscountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class StubSessionService : ISessionService
    {
        public OperatorSession? CurrentSession { get; set; }
        public bool IsActive => CurrentSession != null;

        public void StartSession(OperatorSession session) { CurrentSession = session; }
        public void ClearSession() { CurrentSession = null; }
    }
}
