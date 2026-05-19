namespace POS.Shared.Contracts;

public interface IProvisionedTerminalContext
{
    int CurrentTenantId { get; }

    int CurrentLocationId { get; }

    int CurrentTerminalId { get; }

    bool IsProvisioned { get; }
}
