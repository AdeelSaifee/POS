using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class Payment : AppendOnlyEntity
{
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

    public string CurrencyCode { get; set; } = string.Empty;

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
}
