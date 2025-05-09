using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class PageResponse
{
    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Path")]
    public string Path { get; set; } = string.Empty;

    [Display("Created at")]
    public DateTime Created { get; set; }

    [Display("Modified at")]
    public DateTime Modified { get; set; }
}