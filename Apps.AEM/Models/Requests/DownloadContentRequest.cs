using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Requests;

public class DownloadContentRequest : IDownloadContentInput
{
    [Display("Page path", Description = "Page path to be used in the request.")]
    [DataSource(typeof(PageDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Include reference content")]
    public bool? IncludeReferenceContent { get; set; }

    [Display("File format", Description = "Format of the file to be downloaded, defaults to an interoperable HTML.")]
    [DataSource(typeof(DownloadFileFormatHandler))]
    public string? FileFormat { get; set; } = "text/html";
}