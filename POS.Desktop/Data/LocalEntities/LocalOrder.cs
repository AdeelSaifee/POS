using System;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents a locally persisted order/sale, aligned with central Order.
/// </summary>
public class LocalOrder
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public int EmployeeId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? OriginalOrderId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public OrderType OrderType { get; set; }

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public FulfillmentStatus FulfillmentStatus { get; set; }

    public long CatalogVersion { get; set; }

    public int? PriceListId { get; set; }

    public long RuleVersion { get; set; }

    public int? ReceiptTemplateId { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public string? GuestName { get; set; }

    public string? GuestPhone { get; set; }

    public DateTimeOffset? CompletedOn { get; set; }

    public DateTimeOffset? VoidedOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }

    public string? MetadataJson { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
}
