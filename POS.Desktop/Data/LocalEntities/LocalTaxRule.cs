using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalTaxRule : LocalCatalogEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public TaxCalculationMode CalculationMode { get; set; }

    public long RuleVersion { get; set; }
}
