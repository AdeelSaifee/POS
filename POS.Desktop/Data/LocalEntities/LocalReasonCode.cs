namespace POS.Desktop.Data.LocalEntities;

public class LocalReasonCode : LocalCatalogEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ReasonCategory { get; set; } = string.Empty;

    public bool RequiresManagerApproval { get; set; }

    public int SortOrder { get; set; }
}
