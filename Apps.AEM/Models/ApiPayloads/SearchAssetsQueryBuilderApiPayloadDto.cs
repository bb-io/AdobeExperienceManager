using Newtonsoft.Json;

namespace Apps.AEM.Models.ApiPayloads;

public class QueryBuilderDto
{
    [JsonProperty("hits")]
    public List<QueryBuilderHitDto> Hits { get; set; } = [];

    [JsonProperty("more")]
    public bool More { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }

    [JsonProperty("results")]
    public int Results { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("total")]
    public int Total { get; set; }
}

public class QueryBuilderHitDto
{
    [JsonProperty("excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [JsonProperty("lastModified")]
    public DateTime LastModified { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}
