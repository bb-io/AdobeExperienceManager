using Newtonsoft.Json;

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
}
