using Newtonsoft.Json;

namespace Apps.AEM.Events.Models;

public class QueryBuilderPathResponseDto
{
    public List<HitDto> Hits { get; set; }
}

public class HitDto
{
    [JsonProperty("jcr:path")]
    public string Path { get; set; }
}
