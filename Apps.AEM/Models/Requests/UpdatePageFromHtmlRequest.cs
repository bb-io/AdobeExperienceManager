using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Requests;

public class UpdatePageFromHtmlRequest
{
    [Display("Target page path"), DataSource(typeof(PageDataHandler))]
    public string TargetPagePath { get; set; } = string.Empty;

    [Display("Source language")]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    public string? TargetLanguage { get; set; }

    [Display("Ignore reference content errors", Description = "When set to true, errors updating reference content will be ignored")]
    public bool? IgnoreReferenceContentErrors { get; set; }
    
    public FileReference File { get; set; } = null!;
}
