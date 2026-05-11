using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.AEM.Models.Dtos;

public class GetPathByTagQueryBuilderResponseDto
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("results")]
    public int Results { get; set; }

    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("more")]
    public bool More { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("hits")]
    public List<QueryBuilderPathHitResponseDto> Hits { get; set; } = [];
}

public class QueryBuilderPathHitResponseDto
{
    [JsonProperty("jcr:path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("jcr:uuid")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("jcr:content/jcr:title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("jcr:content/metadata/dc:title")]
    public string MetadataTitle { get; set; } = string.Empty;

    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
}
