using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class DownloadAssetMetadataResponse
{
    [Display("Metadata JSON file")]
    public FileReference File { get; set; } = new();
}
