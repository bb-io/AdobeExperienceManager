using Newtonsoft.Json;

namespace Apps.AEM.Models.Entities;

public class AssetPropertiesEntity
{
    [JsonProperty("cq:tags")]
    public List<string> CqTags { get; set; }
}
