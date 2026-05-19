using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class ZReport : AppendOnlyEntity
{
    public int LocationId { get; set; }

    public int? TerminalId { get; set; }

    public Guid? ShiftId { get; set; }

    public int GeneratedByEmployeeId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public string ReportNumber { get; set; } = string.Empty;

    public ZReportType ReportType { get; set; }

    public ZReportStatus Status { get; set; }

    public decimal GrossSalesAmount { get; set; }

    public decimal NetSalesAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal RefundAmount { get; set; }

    public decimal CashExpectedAmount { get; set; }

    public decimal? CashCountedAmount { get; set; }

    public decimal? CashVarianceAmount { get; set; }

    public string ReportPayloadJson { get; set; } = string.Empty;

    public DateTimeOffset GeneratedOn { get; set; }

    public DateTimeOffset? SyncedOn { get; set; }
}
