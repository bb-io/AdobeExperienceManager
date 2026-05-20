using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class UploadSitesResponse
{
    [Display("Reference content")]
    public IEnumerable<UploadReferenceContentResponse> ReferenceContent { get; set; }
    
    [Display("Root content")]
    public UploadContentResponse RootContent { get; set; }
    
    public UploadSitesResponse(UploadContentResponse rootContent, IEnumerable<UploadContentResponse> referenceContent)
    {
        RootContent = rootContent;
        ReferenceContent = referenceContent;
    }
}