using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Responses;

public class ContentResponse : IDownloadContentInput
{
    [Display("Content path")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Created at")]
    public DateTime Created { get; set; }

    [Display("Modified at")]
    public DateTime Modified { get; set; }
}