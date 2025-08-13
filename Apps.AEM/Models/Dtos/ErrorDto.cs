using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;
public class ErrorDto
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("error")]
    public string Error { get; set; } = string.Empty;
    
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonProperty("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}
