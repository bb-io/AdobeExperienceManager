namespace Apps.AEM.Models.Dtos;

using Newtonsoft.Json;

public class AccessTokenDto
{
    [JsonProperty("token_type")]
    public string JsonPropertyTokenType { get; set; } = string.Empty;
    
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
}
