using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetIdsFromContentResponse
{
    [Display("Root content")]
    public string RootContentId { get; set; } = string.Empty;

    [Display("Referenced content")]
    public List<string> ReferencedContentIds { get; set; } = [];
}
