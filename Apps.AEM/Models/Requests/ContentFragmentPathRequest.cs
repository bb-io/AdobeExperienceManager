using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.AEM.Models.Requests;

public class ContentFragmentPathRequest
{
    [Display("Content path", Description = "The content fragment path. Must start with /content/dam.")]
    [FileDataSource(typeof(AssetPickerDataSourceHandler))]
    public string ContentId { get; set; } = string.Empty;
}
