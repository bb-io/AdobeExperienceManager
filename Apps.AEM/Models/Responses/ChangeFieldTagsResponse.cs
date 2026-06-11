using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class ChangeFieldTagsResponse
{
    [Display("Tags after update")]
    public IEnumerable<string> Tags { get; set; } = [];
}
