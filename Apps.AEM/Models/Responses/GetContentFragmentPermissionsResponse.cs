using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetContentFragmentPermissionsResponse
{
    [Display("Permissions")]
    public IEnumerable<string> Permissions { get; set; } = [];
}
