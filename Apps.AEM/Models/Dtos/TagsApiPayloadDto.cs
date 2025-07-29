using Apps.AEM.Utils.Converters.Tags;
using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;

[JsonConverter(typeof(TagsApiPayloadDtoConverter))]
public class TagsApiPayloadDto
{
    [JsonProperty("hidden")]
    public bool Hidden { get; set; } = false;

    [JsonProperty("jcr:createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonProperty("jcr:mixinTypes")]
    public List<string> MixinTypes { get; set; } = [];

    [JsonProperty("sling:target")]
    public string Target { get; set; } = string.Empty;

    [JsonProperty("sling:resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonProperty("jcr:title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("jcr:primaryType")]
    public string PrimaryType { get; set; } = string.Empty;

    [JsonProperty("languages")]
    public List<string> Languages { get; set; } = [];

    [JsonProperty("jcr:created")]
    [JsonConverter(typeof(TagDateTimeConverter))]
    public DateTime Created { get; set; }

    // Dynamic children: any key with "jcr:primaryType": "cq:Tag" will be deserialized as TagNodeDto
    public IEnumerable<TagNodeDto> Tags { get; set; } = [];
}

[JsonConverter(typeof(TagNodeDtoConverter))]
public class TagNodeDto
{
    public string TagId { get; set; } = string.Empty;

    [JsonProperty("jcr:title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("jcr:description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("jcr:primaryType")]
    public string PrimaryType { get; set; } = string.Empty;

    [JsonProperty("sling:resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonProperty("jcr:createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonProperty("jcr:created")]
    [JsonConverter(typeof(TagDateTimeConverter))]
    public DateTime Created { get; set; }

    // Dynamic children: any key with "jcr:primaryType": "cq:Tag" will be deserialized as TagNodeDto
    public IEnumerable<TagNodeDto> Tags { get; set; } = [];
}
