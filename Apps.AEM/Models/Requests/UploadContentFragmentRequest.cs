using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Requests;

public class UploadContentFragmentRequest
{
    [Display("Content", Description = "A translated content fragment HTML or XLIFF file.")]
    public FileReference Content { get; set; } = new();

    [Display("Variation title", Description = "The variation title to create or update on the target content fragment.")]
    public string VariationTitle { get; set; } = string.Empty;

    [Display("Variation description", Description = "Description used only when the target variation needs to be created.")]
    public string? VariationDescription { get; set; }

    [Display("Check in", Description = "When true, the content fragment will be checked in after upload. If it is not checked out, the check-in request succeeds without changes.")]
    public bool? CheckIn { get; set; }
}
