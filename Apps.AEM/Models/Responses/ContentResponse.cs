using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Newtonsoft.Json;

namespace Apps.AEM.Models.Responses;

public class ContentResponse : IDownloadContentInput
{
    [Display("Content path")]
    [JsonProperty("path")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Display("Created at")]
    [JsonProperty("created")]
    public DateTime Created { get; set; }

    [Display("Modified at")]
    [JsonProperty("modified")]
    public DateTime Modified { get; set; }
}
