using System;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

/// <summary>
/// Represents a locally persisted payment transaction row, aligned with central Payment.
/// </summary>
public class LocalPayment
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public Guid OrderId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public int TenderMethodId { get; set; }

    public Guid? OriginalPaymentId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public long TerminalSequence { get; set; }

    public PaymentType PaymentType { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public decimal? AuthorizedAmount { get; set; }

    public decimal? CapturedAmount { get; set; }

    public string? PaymentToken { get; set; }

    public string? ExternalPaymentReference { get; set; }

    public string? AuthorizationCode { get; set; }

    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public bool RequiresReconciliation { get; set; }

    public DateTimeOffset? ReconciledOn { get; set; }

    public DateTimeOffset ProcessedOn { get; set; }

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
