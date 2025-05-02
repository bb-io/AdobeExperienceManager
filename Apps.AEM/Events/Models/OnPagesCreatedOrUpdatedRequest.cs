using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Events.Models;

public class OnPagesCreatedOrUpdatedRequest
{
    [Display("Root path")]
    public string? RootPath { get; set; }

    [Display("Root path includes")]
    public IEnumerable<string>? RootPathIncludes { get; set; }
}
