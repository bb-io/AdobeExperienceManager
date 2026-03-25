using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.AEM.Models.Requests;

public class DownloadContentFragmentRequest
{
    [Display("Content path", Description = "The content fragment path to download. Must start with /content/dam.")]
    [FileDataSource(typeof(AssetPickerDataSourceHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Check out", Description = "When true, the content fragment will be checked out before download. Disabled by default.")]
    public bool? CheckOut { get; set; }

    [Display("Excluded fields", Description = "Optional field names to omit from the generated HTML body. History and preview URL are excluded by default.")]
    public IEnumerable<string>? ExcludedFields { get; set; }
}
