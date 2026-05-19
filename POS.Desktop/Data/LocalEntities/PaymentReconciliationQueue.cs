using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class PaymentReconciliationQueue : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid OrderId { get; set; }

    public Guid PaymentId { get; set; }

    public int TenderMethodId { get; set; }

    public string? ExternalPaymentReference { get; set; }

    public string? PaymentToken { get; set; }

    public PaymentReconciliationStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptOn { get; set; }

    public DateTimeOffset? LastAttemptOn { get; set; }

    public string? LastResultCode { get; set; }

    public string? LastResultMessage { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset? RetainUntil { get; set; }
}
