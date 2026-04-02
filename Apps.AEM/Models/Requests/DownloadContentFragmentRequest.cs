using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class DownloadContentFragmentRequest
{
    [Display("Content path", Description = "The content fragment path to download. Must start with /content/dam.")]
    [DataSource(typeof(ContentFragmentDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Check out", Description = "When true, the content fragment will be checked out before download. Disabled by default.")]
    public bool? CheckOut { get; set; }

    [Display("Excluded fields", Description = "Optional field names to omit from the generated HTML body. History and preview URL are excluded by default.")]
    public IEnumerable<string>? ExcludedFields { get; set; }

    [Display("Include references", Description = "When true, referenced content fragments are appended to the same HTML file. Disabled by default.")]
    public bool? IncludeReferences { get; set; }

    [Display("Content fragment models to exclude", Description = "Optional referenced content fragment model names to exclude from export at any nesting level.")]
    public IEnumerable<string>? ExcludedReferenceModels { get; set; }

    [Display("Reference fields to exclude", Description = "Optional reference field names to skip when traversing referenced content fragments at any nesting level.")]
    public IEnumerable<string>? ExcludedReferenceFields { get; set; }

    [Display("Max level of nesting included", Description = "Maximum nesting level of referenced content fragments to include. Root is level 0 and the first reference is level 1. Defaults to 10.")]
    public int? MaxReferenceNestingLevel { get; set; }
}
