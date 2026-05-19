namespace POS.Shared.Domain.Entities.Central;

public class SyncIngestAck
{
    public Guid Id { get; set; }

    public int TenantId { get; set; }

    public int LocationId { get; set; }

    public int TerminalId { get; set; }

    public long ChunkSequence { get; set; }

    public string ChunkIdempotencyKey { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public int EventCount { get; set; }

    public DateOnly? FirstBusinessDate { get; set; }

    public DateOnly? LastBusinessDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AckPayloadJson { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset ReceivedOn { get; set; }

    public DateTimeOffset ExpiresOn { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
}
