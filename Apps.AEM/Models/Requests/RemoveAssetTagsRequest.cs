using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class RemoveAssetTagsRequest
{
    [Display("Tags to remove")]
    public List<string> TagsToRemove { get; set; }
}
