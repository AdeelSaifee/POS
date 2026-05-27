using POS.Shared.Enums;

namespace POS.Desktop.Data.LocalEntities;

public class LocalUnitOfMeasure : LocalCatalogEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public MeasurementType MeasurementType { get; set; }

    public int DecimalPlaces { get; set; }

    public bool AllowsFractionalQuantity { get; set; }
}
