using POS.Api.Contracts.UnitsOfMeasure;

namespace POS.Api.Application.UnitsOfMeasure;

public interface IUnitOfMeasureReadService : IApplicationService
{
    Task<IReadOnlyList<UnitOfMeasureListItemDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken);
}
