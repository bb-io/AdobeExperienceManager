using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Events.Models;

public class OnPropertyUpdatedRequest
{
    [Display("Root path")]
    public string RootPath { get; set; }

    [Display("Property name")]
    public string PropertyName { get; set; }

    [Display("Property value")]
    public string PropertyValue { get; set; }
}
