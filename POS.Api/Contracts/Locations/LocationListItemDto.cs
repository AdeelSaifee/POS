using POS.Shared.Enums;

namespace POS.Api.Contracts.Locations;

public sealed record LocationListItemDto(
    int Id,
    string LocationCode,
    string Name,
    LocationType LocationType);
