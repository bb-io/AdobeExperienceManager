using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class DownloadContentRequest
{
    [Display("Include reference content")]
    public bool? IncludeReferenceContnent { get; set; }
}