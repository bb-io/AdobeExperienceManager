using Newtonsoft.Json.Linq;

namespace Apps.AEM.Models.Entities;

public class ContentFragmentHtmlEntity(string sourcePath, JArray fields, bool referenceContent)
{
    public string SourcePath { get; set; } = sourcePath;

    public JArray Fields { get; set; } = fields;

    public bool ReferenceContent { get; set; } = referenceContent;
}
