namespace POS.Desktop.Data.LocalEntities;

public class LocalTenderMethod : LocalCatalogEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TenderType { get; set; } = string.Empty;

    public bool AllowsChange { get; set; }

    public bool RequiresExternalReference { get; set; }

    public int SortOrder { get; set; }
}
