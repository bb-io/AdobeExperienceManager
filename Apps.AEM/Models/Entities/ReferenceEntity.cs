using Newtonsoft.Json;

namespace Apps.AEM.Models.Entities;
public record class ReferenceEntity
{
    [JsonProperty("referencePath")]
    public string ReferencePath { get; set; }

    [JsonIgnore]
    public string Content { get; set; }

    [JsonProperty("propertyName")]
    public string? PropertyName { get; set; }

    [JsonProperty("propertyPath")]
    public string? PropertyPath { get; set; }

    public ReferenceEntity(string referencePath, string content, string? propertyName = null, string? propertyPath = null)
    {
        ReferencePath = referencePath;
        Content = content;
        PropertyName = propertyName;
        PropertyPath = propertyPath;
    }
}