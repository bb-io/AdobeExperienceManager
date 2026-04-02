using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;

public class ContentFragmentTagListDto
{
    [JsonProperty("items")]
    public List<ContentFragmentTagDto> Items { get; set; } = [];
}

public class ContentFragmentTagDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}
