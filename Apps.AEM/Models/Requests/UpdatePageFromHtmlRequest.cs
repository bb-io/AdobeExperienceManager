using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Requests;

public class UpdatePageFromHtmlRequest
{
    [Display("Target page path")]
    public string TargetPagePath { get; set; } = string.Empty;
    
    public FileReference File { get; set; } = null!;
}
