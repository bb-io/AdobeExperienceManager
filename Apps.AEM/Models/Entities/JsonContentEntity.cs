using Newtonsoft.Json.Linq;

namespace Apps.AEM.Models.Entities;

public class JsonContentEntity
{
    public string? SourcePath { get; set; }
    public JObject TargetContent { get; set; }
    public List<ReferenceEntity> References { get; set; }
    public bool ReferenceContent { get; set; }

    public JsonContentEntity(string? sourcePath, JObject targetContent, List<ReferenceEntity> references, bool referenceContent)
    {
        SourcePath = sourcePath;
        TargetContent = targetContent;
        References = references;
        ReferenceContent = referenceContent;
    }
}
