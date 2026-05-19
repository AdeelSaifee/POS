namespace POS.Shared.Domain.Base;

public abstract class TenantRootEntity
{
    public int Id { get; set; }

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }
}
