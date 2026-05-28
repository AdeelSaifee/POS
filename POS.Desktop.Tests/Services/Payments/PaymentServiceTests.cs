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
            Tenders: new List<PaymentTenderRequest> { new(1, 105m) }
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
            Tenders: new List<PaymentTenderRequest> { new(1, 100m) } // 100 paid for 90 due
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
            Tenders: new List<PaymentTenderRequest> { new(2, 150m, "TXN-CARD-99") }
        );

        // Act
        var result = await paymentService.CompleteOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        var savedPayment = await db.LocalPayments.FirstOrDefaultAsync(p => p.OrderId == result.OrderId);
        Assert.NotNull(savedPayment);
        Assert.Equal(2, savedPayment.TenderMethodId);
        Assert.Equal("TXN-CARD-99", savedPayment.ExternalPaymentReference);
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
            Tenders: new List<PaymentTenderRequest> { new(1, 50m), new(2, 150m) }
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
            Tenders: new List<PaymentTenderRequest> { new(1, 99m) } // underpaid by 1 PKR
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

        var request = new PaymentCompletionRequest(Tenders: new List<PaymentTenderRequest> { new(1, 50m) });

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
        var request = new PaymentCompletionRequest(Tenders: new List<PaymentTenderRequest> { new(1, 50m) });

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
        var request = new PaymentCompletionRequest(Tenders: new List<PaymentTenderRequest> { new(1, 50m) });

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
        var request = new PaymentCompletionRequest(Tenders: new List<PaymentTenderRequest> { new(1, 50m) });

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
            Tenders: new List<PaymentTenderRequest> { new(999, 50m) } // Unknown tender ID 999
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
            Tenders: new List<PaymentTenderRequest> { new(2, 105m, "TXN-CARD") } // Card (AllowsChange = false) overpaid by 5 PKR
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
            GuestPhone: "555-1234"
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
            GuestPhone: "555-1234"
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
        var request = new PaymentCompletionRequest(Tenders: new List<PaymentTenderRequest> { new(1, 50m) });

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
            Tenders: new List<PaymentTenderRequest> { new(1, 200m) }
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

    private class StubOrderService : IOrderService
    {
        public CartStateDto CartState { get; set; } = new();
        public bool ClearCartCalled { get; set; }

        public Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CartState);
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
