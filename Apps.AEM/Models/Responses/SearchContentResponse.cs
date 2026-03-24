using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Responses;

public class SearchContentResponse(IEnumerable<ContentResponse> contentList) : IMultiDownloadableContentOutput<ContentResponse>
{
    [Display("Content")]
    public List<ContentResponse> Items { get; set; } = contentList.ToList();

    [Display("Total count")]
    public int TotalCount { get; set; } = contentList.Count();
}
