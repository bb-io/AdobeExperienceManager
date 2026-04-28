using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class ContentFragmentTagAddedItemResponse
{
    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Content path")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Fragment ID")]
    public string FragmentId { get; set; } = string.Empty;

    [Display("Added tags")]
    public IEnumerable<string> AddedTags { get; set; } = [];
}
