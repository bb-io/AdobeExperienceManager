using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetIdsFromContentResponse
{
    [Display("Root content ID")]
    public string RootContentId { get; set; } = string.Empty;

    [Display("Referenced content IDs")]
    public List<string> ReferencedContentIds { get; set; } = [];
}
