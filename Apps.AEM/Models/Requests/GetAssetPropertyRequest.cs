using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class GetAssetPropertyRequest
{
    [Display("Property name", Description = "Property name to be updated. For example, to update the title and description, provide 'dc:title' or 'dc:description'.")]
    public string PropertyName { get; set; } = string.Empty;
}
