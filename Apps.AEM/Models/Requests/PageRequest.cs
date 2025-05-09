using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class PageRequest
{
    [Display("Page path", Description = "Page path to be used in the request."), DataSource(typeof(PageDataHandler))]
    public string PagePath { get; set; } = string.Empty;
}
