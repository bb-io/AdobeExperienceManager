using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Responses;

public class DownloadContentResponse(FileReference fileReference, IEnumerable<string>? downloadIssues = null) : IDownloadContentOutput
{
    public FileReference Content { get; set; } = fileReference;

    [Display("Skipped due to errors")]
    public IEnumerable<string>? DownloadIssues { get; set; } = downloadIssues;
}
