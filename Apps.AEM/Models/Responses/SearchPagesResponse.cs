using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class SearchPagesResponse(List<PageResponse> pages)
{
    [Display("Content")]
    public List<PageResponse> Pages { get; set; } = pages;

    [Display("Total count")]
    public double TotalCount { get; set; } = pages.Count;
}
