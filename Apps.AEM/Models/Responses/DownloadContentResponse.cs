using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.AEM.Models.Responses;

public class DownloadContentResponse(FileReference fileReference) : IDownloadContentOutput
{
    public FileReference Content { get; set; } = fileReference;
}
