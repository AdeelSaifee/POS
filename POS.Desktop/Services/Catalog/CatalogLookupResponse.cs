namespace POS.Desktop.Services.Catalog;

/// <summary>Bridge response payload for catalog.lookupByIdentifier.</summary>
public sealed record CatalogLookupResponse
{
    /// <summary>True when a matching item was found for the given identifier.</summary>
    public bool Found { get; init; }

    /// <summary>The matched item, or null when Found is false.</summary>
    public CatalogItemDto? Item { get; init; }
}
