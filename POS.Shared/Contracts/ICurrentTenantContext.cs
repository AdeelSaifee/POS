namespace POS.Shared.Contracts;

public interface ICurrentTenantContext
{
    int CurrentTenantId { get; }

    bool HasTenant { get; }

    bool IsSystemScope { get; }
}
