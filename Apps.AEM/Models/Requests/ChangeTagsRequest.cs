using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class ChangeTagsRequest
{
    [Display("Content path")]
    public string ContentPath { get; set; } = String.Empty;

    [Display("Tags to add")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? AddTags { get; set; }

    [Display("Tags to remove")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? RemoveTags { get; set; }
}
