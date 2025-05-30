using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Requests;

public class UpdatePageFromHtmlRequest
{
    [Display("Target page path"), DataSource(typeof(PageDataHandler))]
    public string TargetPagePath { get; set; } = string.Empty;
    
    public FileReference File { get; set; } = null!;
}
