namespace POS.Desktop.Data.LocalEntities;

public class LocalItemIdentifier : LocalCatalogEntity
{
    public int ItemId { get; set; }

    public int? ItemVariantId { get; set; }

    public string IdentifierType { get; set; } = string.Empty;

    public string IdentifierValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}
