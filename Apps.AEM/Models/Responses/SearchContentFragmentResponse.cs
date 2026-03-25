using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Responses;

public class SearchContentFragmentResponse(IEnumerable<ContentFragmentItemResponse> contentList) : IMultiDownloadableContentOutput<ContentFragmentItemResponse>
{
    [Display("Content fragments")]
    public List<ContentFragmentItemResponse> Items { get; set; } = contentList.ToList();

    [Display("Total count")]
    public int TotalCount { get; set; } = contentList.Count();
}
