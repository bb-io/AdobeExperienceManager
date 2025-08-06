using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class UploadContentResponse
{
    [Display("Path", Description = "The path where the content was uploaded.")]
    public string ContentId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
