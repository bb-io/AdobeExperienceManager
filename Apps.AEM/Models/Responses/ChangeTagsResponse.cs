using Apps.AEM.Models.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class ChangeTagsResponse
{
    [Display("Tags at the content piece after update")]
    public IEnumerable<string> Tags { get; set; } = [];
}
