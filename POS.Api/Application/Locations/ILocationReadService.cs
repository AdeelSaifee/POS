using POS.Api.Contracts.Locations;

namespace POS.Api.Application.Locations;

public interface ILocationReadService : IApplicationService
{
    Task<IReadOnlyList<LocationListItemDto>> GetLocationsAsync(CancellationToken cancellationToken);
}
