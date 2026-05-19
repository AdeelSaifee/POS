using POS.Shared.Domain.Base;
using POS.Shared.Enums;

namespace POS.Shared.Domain.Entities.Central;

public class UnitOfMeasure : TenantScopedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public MeasurementType MeasurementType { get; set; }

    public int DecimalPlaces { get; set; }

    public int? BaseUnitId { get; set; }

    public decimal? ConversionFactorToBase { get; set; }

    public bool AllowsFractionalQuantity { get; set; }
}
