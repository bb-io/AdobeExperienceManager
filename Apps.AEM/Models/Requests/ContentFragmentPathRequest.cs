using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class ContentFragmentPathRequest
{
    [Display("Content path", Description = "The content fragment path. Must start with /content/dam.")]
    [DataSource(typeof(ContentFragmentDataHandler))]
    public string ContentId { get; set; } = string.Empty;
}
