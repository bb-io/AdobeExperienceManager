using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Responses;

public class FileResponse(FileReference fileReference)
{
    public FileReference File { get; set; } = fileReference;
}
