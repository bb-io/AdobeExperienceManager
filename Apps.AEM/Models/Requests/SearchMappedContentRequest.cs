using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class SearchMappedContentRequest
{
    [Display("Content paths", Description = "List of content paths to search for mapped references, eg. /content/dam/guides/en/example.ditamap")]
    public IEnumerable<string> ContentIds { get; set; } = [];

    [Display("Include map itself", Description = "Whether to include the map/guide itself into the results. Included by default")]
    public bool? IncludeMaps { get; set; } = true;

    [Display("Follow references recursively", Description = "Whether to follow references recursively to find all mapped content, to collect all references from referenced maps. Enabled by default.")]
    public bool? SearchRecursively { get; set; } = true;

    [Display("Exclude references with specified tags", Description = "If a referenced content has any of the specified tags, it will be excluded from the results.")]
    public IEnumerable<string>? ExcludedTags { get; set; } = [];
}
