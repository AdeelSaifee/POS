using POS.Shared.Enums;

namespace POS.Api.Contracts.UnitsOfMeasure;

public sealed record UnitOfMeasureListItemDto(
    int Id,
    string Code,
    string Name,
    MeasurementType MeasurementType,
    int? BaseUnitId,
    decimal? ConversionFactorToBase);
