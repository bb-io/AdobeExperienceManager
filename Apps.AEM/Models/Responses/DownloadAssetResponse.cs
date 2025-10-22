using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class DownloadAssetResponse
{
    [Display("Asset file")]
    public FileReference File { get; set; } = new();
}
