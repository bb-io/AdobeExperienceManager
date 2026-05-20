using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class UploadContentResponse
{
    [Display("Path", Description = "The path where the content was uploaded.")]
    public string ContentId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [Display("Target file", Description = "HTML representation of the translated content for Blacklake. Present only for the root (non-reference) content item.")]
    public FileReference? TargetFile { get; set; }
}
