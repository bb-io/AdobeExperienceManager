using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class DownloadContentFragmentResponse(FileReference fileReference)
{
    public FileReference Content { get; set; } = fileReference;
}
