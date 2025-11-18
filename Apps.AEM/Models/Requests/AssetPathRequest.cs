using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.AEM.Models.Requests;

public class AssetPathRequest
{
    [Display("Path", Description = "The path of the asset to be downloaded (must start with /content/dam).")]
    [FileDataSource(typeof(AssetPickerDataSourceHandler))]
    public string Path { get; set; } = string.Empty;
}
