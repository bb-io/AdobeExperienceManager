namespace Apps.AEM.Models.Responses;

public class SearchAssetsResponse
{
    public IEnumerable<AssetItem> Assets { get; set; } = [];
    public int AssetsFound { get; set; }
}

public class AssetItem
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}