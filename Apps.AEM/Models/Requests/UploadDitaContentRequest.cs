using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.AEM.Models.Requests;

public class UploadDitaContentRequest
{
    [Display("Content", Description = "File to upload, can be interoperable XLIFF, as well as translated dita or ditamap file.")]
    public FileReference Content { get; set; } = new();

    [Display("Source file path", Description = "Path to the original file, eg. /content/dam/guides/en/example.dita")]
    public string SourceFilePath { get; set; } = string.Empty;

    [Display("Target file path", Description = "Path where translated file should be placed, eg. /content/dam/guides/fr/example.dita")]
    public string TargetFilePath { get; set; } = string.Empty;
}
