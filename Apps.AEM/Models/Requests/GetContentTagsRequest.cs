
using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class GetContentTagsRequest
{
    [Display("Content path", Description = "Path to the content item to retrieve tags for, starts with `/content/`. Support both sites and assets.")]
    public string ContentId { get; set; } = string.Empty;
}
