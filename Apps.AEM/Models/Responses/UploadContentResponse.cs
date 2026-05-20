using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class UploadContentResponse : UploadReferenceContentResponse
{
    [Display("Target file", Description = "HTML representation of the translated content for Blacklake. Present only for the root (non-reference) content item.")]
    public FileReference? TargetFile { get; set; }
}
