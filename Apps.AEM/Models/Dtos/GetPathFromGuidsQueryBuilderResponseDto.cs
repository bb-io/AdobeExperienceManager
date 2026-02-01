using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;

public class GetPathFromGuidsQueryBuilderResponseDto
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
    public List<QueryBuilderHitResponseDto> Hits { get; set; } = [];
}

public class QueryBuilderHitResponseDto
{
    [JsonProperty("jcr:path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("jcr:content")]
    public JcrContentDto Content { get; set; } = new();
}

public class JcrContentDto
{
    [JsonProperty("fmUuid")]
    public string FmUuid { get; set; } = string.Empty;

    [JsonProperty("metadata")]
    public JcrMetadataDto Metadata { get; set; } = new();
}

public class JcrMetadataDto
{
    [JsonProperty("cq:tags")]
    public List<string> Tags { get; set; } = [];
}