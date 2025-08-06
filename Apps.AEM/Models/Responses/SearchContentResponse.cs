using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class SearchContentResponse(IEnumerable<ContentResponse> contentList)
{
    [Display("Content")]
    public IEnumerable<ContentResponse> Content { get; set; } = contentList;

    [Display("Total count")]
    public int TotalCount { get; set; } = contentList.Count();
}
