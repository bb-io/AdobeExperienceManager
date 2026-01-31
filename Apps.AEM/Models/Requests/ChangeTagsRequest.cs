using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class ChangeTagsRequest
{
    [Display("Content path")]
    public string ContentPath { get; set; } = String.Empty;

    [Display("Tags to add")]
    public IEnumerable<string>? AddTags { get; set; }

    [Display("Tags to remove")]
    public IEnumerable<string>? RemoveTags { get; set; }
}
