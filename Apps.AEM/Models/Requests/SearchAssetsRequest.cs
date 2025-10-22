using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class SearchAssetsRequest
{
    [Display("Root path", Description = "The path under which assets are searched (must start with /content/dam).")]
    public string RootPath { get; set; } = string.Empty;

    [Display("Node name", Description = "A patter of the name to search for. For example: *.dita")]
    public string? NodeName { get; set; } = string.Empty;
}
