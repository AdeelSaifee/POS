using Microsoft.EntityFrameworkCore;
using POS.Api.Contracts.Locations;
using POS.Api.Data;

namespace POS.Api.Application.Locations;

public sealed class LocationReadService : ILocationReadService
{
    private readonly PosCentralDbContext _dbContext;

    public LocationReadService(PosCentralDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LocationListItemDto>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LocationListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.LocationType))
            .ToListAsync(cancellationToken);
    }
}
