using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class ContentFragmentCheckoutStateResponse
{
    [Display("Path", Description = "The content fragment path whose checkout state was updated.")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Checked out", Description = "Whether the content fragment is checked out after the action completes.")]
    public bool CheckedOut { get; set; }

    public string Message { get; set; } = string.Empty;
}
