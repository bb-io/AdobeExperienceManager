using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class SearchPagesResponse(IEnumerable<PageResponse> pages)
{
    [Display("Content")]
    public IEnumerable<PageResponse> Pages { get; set; } = pages;

    [Display("Total count")]
    public int TotalCount { get; set; } = pages.Count();
}
