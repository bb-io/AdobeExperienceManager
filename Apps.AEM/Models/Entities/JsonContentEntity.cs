using Newtonsoft.Json.Linq;

namespace Apps.AEM.Models.Entities;
public class JsonContentEntity(string sourcePath, JObject targetContent, List<ReferenceEntity> references, bool referenceContent)
{
    public string SourcePath { get; set; } = sourcePath;

    public JObject TargetContent { get; set; } = targetContent;

    public List<ReferenceEntity> References { get; set; } = references;

    public bool ReferenceContent { get; set; } = referenceContent;
}
