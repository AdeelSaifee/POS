using Microsoft.EntityFrameworkCore;
using POS.Api.Contracts.UnitsOfMeasure;
using POS.Api.Data;

namespace POS.Api.Application.UnitsOfMeasure;

public sealed class UnitOfMeasureReadService : IUnitOfMeasureReadService
{
    private readonly PosCentralDbContext _dbContext;

    public UnitOfMeasureReadService(PosCentralDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UnitOfMeasureListItemDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new UnitOfMeasureListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.MeasurementType,
                x.BaseUnitId,
                x.ConversionFactorToBase))
            .ToListAsync(cancellationToken);
    }
}
