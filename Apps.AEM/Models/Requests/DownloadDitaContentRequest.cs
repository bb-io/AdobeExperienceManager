using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.AEM.Models.Requests;

public class DownloadDitaContentRequest
{
    [Display("Content path", Description = "Content path to be used in the request.")]
    [FileDataSource(typeof(ContentPickerDataSourceHandler))]
    public string ContentId { get; set; } = string.Empty;
}
