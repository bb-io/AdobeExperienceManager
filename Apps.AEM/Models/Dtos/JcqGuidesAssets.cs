using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;

internal class JcqGuidesAssets
{
    [JsonProperty("fmDependents")]
    public IEnumerable<string>? AllReferences { get; set; } = [];

    [JsonProperty("fmditaMaprefs")]
    public IEnumerable<string>? MapsReferenced { get; set; } = [];

    [JsonProperty("fmditaTopicrefs")]
    public IEnumerable<string>? TopicsReferenced { get; set; } = [];
}
