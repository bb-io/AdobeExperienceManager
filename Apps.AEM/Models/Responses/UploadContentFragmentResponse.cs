using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class UploadContentFragmentResponse
{
    [Display("Path", Description = "The content fragment path where the variation was updated.")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Variation name", Description = "The resolved content fragment variation name used for the update.")]
    public string VariationName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [Display("Target file", Description = "HTML of the uploaded variation for Blacklake integration.")]
    public FileReference? TargetFile { get; set; }
}
