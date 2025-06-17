using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;

public class ReferenceDto
{
    [JsonProperty("propertyName")]
    public string? PropertyName { get; set; }

    [JsonProperty("propertyPath")]
    public string? PropertyPath { get; set; }

    [JsonProperty("referencePath")]
    public string ReferencePath { get; set; } = string.Empty;
}