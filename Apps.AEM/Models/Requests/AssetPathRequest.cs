using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class AssetPathRequest
{
    [Display("Path", Description = "The path of the asset to be downloaded (must start with /content/dam).")]
    public string Path { get; set; } = string.Empty;
}
