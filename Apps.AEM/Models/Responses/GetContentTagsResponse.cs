using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetContentTagsResponse
{
    [Display("Tags")]
    public IEnumerable<string> Tags { get; set; } = [];
}
