using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class UpdateAssetTagsRequest
{
    [Display("Tags")]
    public List<string> Tags { get; set; }
}
