using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class PrintQueue : LocalOperationalEntity
{
    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? ZReportId { get; set; }

    public string PrintJobType { get; set; } = string.Empty;

    public string? ReceiptNumber { get; set; }

    public int? ReceiptTemplateId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public string? RenderedContent { get; set; }

    public PrintQueueStatus Status { get; set; }

    public int Priority { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptOn { get; set; }

    public DateTimeOffset? PrintedOn { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset? RetainUntil { get; set; }
}
