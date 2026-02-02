using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class OnTagAddedContentResponse(IEnumerable<string> contentList)
{
    [Display("Content paths")]
    public IEnumerable<string> ContentIds { get; set; } = contentList;

    [Display("Total count")]
    public int TotalCount { get; set; } = contentList.Count();
}
