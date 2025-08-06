using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Requests;

public class UploadContentRequest : IUploadContentInput
{
    [Display("Content", Description = "File to upload, can be interoperable HTML, XLIFF or an original JSON.")]
    public FileReference Content { get; set; } = new();

    [Display("Source language", Description = "Replace source language in path with target language, eg. '/en-us/'.")]
    public string SourceLocale { get; set; } = string.Empty;

    [Display("Target language", Description = "Target language to put instead of source language in path, eg. '/de-de/'.")]
    public string Locale { get; set; } = string.Empty;

    [Display("Overwrite main content path", Description = "Specify source content path to be modified with source to target language replacement (this input will overwrite main or 'root' path from content file). Useful for testing as target content could be created in a different location. Can be only used with 'Skip references' input set to 'true'.")]
    [DataSource(typeof(PageDataHandler))]
    public string? ContentId { get; set; } = string.Empty;

    [Display("Skip references", Description = "When set to true, references won't be updated.")]
    public bool? SkipUpdatingReferences { get; set; }

    [Display("Ignore reference content errors", Description = "When set to true, errors updating reference content will be ignored.")]
    public bool? IgnoreReferenceContentErrors { get; set; }
}
