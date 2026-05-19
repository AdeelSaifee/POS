using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class CashAccount : TenantScopedEntity
{
    public int? LocationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public CashAccountType AccountType { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumberMasked { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;
}
