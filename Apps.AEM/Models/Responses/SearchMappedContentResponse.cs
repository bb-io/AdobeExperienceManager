using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class SearchMappedContentResponse
{
    [Display("Content paths")]
    public IEnumerable<string> ContentIds { get; set; } = [];

    [Display("Errors")]
    public IEnumerable<string> Errors { get; set; } = [];
}
