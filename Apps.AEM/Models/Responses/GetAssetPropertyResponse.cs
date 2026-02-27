using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetAssetPropertyResponse
{
    [Display("Property name")]
    public string PropertyName { get; set; } = string.Empty;

    [Display("Property value")]
    public string PropertyValue { get; set; } = string.Empty;
}
