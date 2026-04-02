using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class OnContentFragmentTagAddedResponse(IEnumerable<ContentFragmentTagAddedItemResponse> items)
{
    [Display("Content fragments")]
    public List<ContentFragmentTagAddedItemResponse> Items { get; set; } = items.ToList();

    [Display("Total count")]
    public int TotalCount { get; set; } = items.Count();
}
